using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    public class VistoriaRepository : IVistoriaRepository
    {
        private const string SqlBloquearLocacaoAtiva = "SELECT id_locacao FROM locacao WHERE id_locacao = @id AND disabled_at IS NULL FOR UPDATE";
        private const string ParametroId = "id";
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public VistoriaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Consulta de entrada ativa, sob o filtro Empresa e sem rastreamento.</summary>
        public IQueryable<Vistoria> ConsultaEntradaAtiva(int idLocacao)
        {
            try
            {
                return _db.Vistorias.AsNoTracking().Include(x => x.Funcionario).Where(x => x.IdLocacao == idLocacao &&
                    x.Tipo == TipoVistoria.ENTRADA && x.DisabledAt == null && x.Locacao.DisabledAt == null);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVistoria, GetType().ToString());
                throw;
            }
        }

        public async Task<Vistoria?> FindEntradaByLocacaoAsync(int idLocacao)
        {
            try
            {
                Vistoria? vistoria = await ConsultaEntradaAtiva(idLocacao).FirstOrDefaultAsync();
                return vistoria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarVistoria, GetType().ToString());
                throw;
            }
        }

        public async Task<Vistoria> InsertAsync(Vistoria vistoria)
        {
            try
            {
                vistoria.IdVistoria = 0;
                _db.Vistorias.Add(vistoria);
                await _db.SaveChangesAsync();
                return vistoria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Confere o escopo antes do SQL de trava, bloqueia a locacao na transacao e rele a locacao sob o filtro Empresa.
        /// O callback do servico valida a duplicidade e insere antes do commit, serializando entradas concorrentes.
        /// </summary>
        public async Task<T> ExecutarComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao)
        {
            try
            {
                bool existe = await _db.Locacoes.AsNoTracking().AnyAsync(x => x.IdLocacao == idLocacao && x.DisabledAt == null);
                if (!existe)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();
                bool bloqueada = await BloquearLocacaoAsync(idLocacao);
                bool continuaAtiva = await _db.Locacoes.AsNoTracking().AnyAsync(x => x.IdLocacao == idLocacao && x.DisabledAt == null);
                if (!bloqueada || !continuaAtiva)
                {
                    throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
                }

                T resultado = await operacao();
                await transacao.CommitAsync();
                return resultado;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }

        private async Task<bool> BloquearLocacaoAsync(int idLocacao)
        {
            try
            {
                DbConnection conexao = _db.Database.GetDbConnection();
                await using DbCommand comando = conexao.CreateCommand();
                comando.CommandText = SqlBloquearLocacaoAtiva;
                comando.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
                DbParameter parametro = comando.CreateParameter();
                parametro.ParameterName = ParametroId;
                parametro.Value = idLocacao;
                comando.Parameters.Add(parametro);
                object? resultado = await comando.ExecuteScalarAsync();
                return resultado != null && resultado != DBNull.Value;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }
    }
}
