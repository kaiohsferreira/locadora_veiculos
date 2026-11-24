using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
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
    /// <summary>Persistencia das locacoes com transacao e bloqueio das linhas envolvidas.</summary>
    public class LocacaoRepository : ILocacaoRepository
    {
        /// <summary>Trava de linha do veiculo (ordem global de travas: veiculo, depois reserva). Sem filtro "Empresa": a reserva ja foi conferida antes.</summary>
        private const string SqlBloquearVeiculoAtivo = "SELECT id_veiculo FROM veiculo WHERE id_veiculo = @id AND disabled_at IS NULL FOR UPDATE";

        /// <summary>Trava de linha da reserva, depois da trava do veiculo. Sem filtro "Empresa": a reserva ja foi conferida antes.</summary>
        private const string SqlBloquearReservaAtiva = "SELECT id_reserva FROM reserva WHERE id_reserva = @id AND disabled_at IS NULL FOR UPDATE";

        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public LocacaoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Consulta apenas dados ativos da retirada, preservando os filtros de empresa.</summary>
        public IQueryable<Reserva> ConsultaReservaParaRetirada(int idReserva)
        {
            try
            {
                IQueryable<Reserva> consulta = ConsultaReservaAtiva(idReserva)
                    .AsNoTracking()
                    .Where(x => x.Cliente.DisabledAt == null && x.Veiculo.DisabledAt == null)
                    .Include(x => x.Veiculo)
                    .Include(x => x.Veiculo)
                        .ThenInclude(x => x.Categoria)
                    .Include(x => x.Veiculo)
                        .ThenInclude(x => x.Empresa)
                    .Include(x => x.Veiculo)
                        .ThenInclude(x => x.Fotos.Where(foto => foto.DisabledAt == null).OrderBy(foto => foto.Ordem).ThenBy(foto => foto.IdVeiculoFoto).Take(ConstantsValues.QuantidadeFotoCapaPorVeiculo))
                    .Include(x => x.Cliente)
                        .ThenInclude(x => x.Locadoras.Where(v => v.DisabledAt == null))
                    .Include(x => x.Locacoes.Where(l => l.DisabledAt == null));
                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca a reserva para retirada sem alterar seu estado.</summary>
        public async Task<Reserva?> FindReservaParaRetiradaAsync(int idReserva)
        {
            try
            {
                Reserva? reserva = await ConsultaReservaParaRetirada(idReserva).FirstOrDefaultAsync();
                return reserva;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Busca reserva ativa com veiculo e cliente para validacao das regras de inicio. O cliente e o pagador da caucao (e-mail e
        /// CPF lidos do banco, nunca do pedido); sem caucao exigida ele nao e usado.
        /// </summary>
        public async Task<Reserva?> FindReservaByIdAsync(int idReserva)
        {
            try
            {
                Reserva? reserva = await ConsultaReservaAtiva(idReserva)
                    .AsNoTracking()
                    .Include(x => x.Veiculo)
                    .Include(x => x.Cliente)
                    .FirstOrDefaultAsync();

                return reserva;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca funcionario autenticado, ativo e nao desativado logicamente.</summary>
        public async Task<Funcionario?> FindActiveFuncionarioByIdAsync(int idFuncionario)
        {
            try
            {
                Funcionario? funcionario = await ConsultaFuncionarioAtivo(idFuncionario)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return funcionario;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Verifica locacao nao desativada associada a reserva.</summary>
        public async Task<bool> ExistsActiveByReservaAsync(int idReserva)
        {
            try
            {
                bool existe = await ConsultaLocacoesAtivasDaReserva(idReserva)
                    .AsNoTracking()
                    .AnyAsync();

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cria a locacao e muda reserva e veiculo na mesma transacao. Ordem global de travas: veiculo, depois reserva
        /// (o funcionario nunca e travado). A reserva e lida com o filtro da locadora antes de qualquer SQL de trava; a
        /// releitura depois das travas reconfere tudo, inclusive o status e a locacao existente. Com caucao (ja pre-autorizada
        /// fora desta transacao), a locacao sai da regra pura com valor_caucao e a linha CAUCAO na navegacao Locacao.Pagamentos:
        /// o EF grava as duas no mesmo SaveChanges e no mesmo commit, ou nenhuma. Caucao nula = abertura do RF-17.
        /// </summary>
        public async Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario, DateTime dataRetirada, CaucaoAberturaRegistro? caucao)
        {
            try
            {
                Reserva? reservaLida = await ConsultaReservaAtiva(idReserva)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (reservaLida == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                int idVeiculo = reservaLida.IdVeiculo;

                await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                bool veiculoBloqueado = await LockActiveRowAsync(SqlBloquearVeiculoAtivo, idVeiculo);

                if (!veiculoBloqueado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                bool reservaBloqueada = await LockActiveRowAsync(SqlBloquearReservaAtiva, idReserva);

                if (!reservaBloqueada)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                Reserva? reserva = await ConsultaReservaAtiva(idReserva)
                    .FirstOrDefaultAsync();

                if (reserva == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                if (reserva.IdVeiculo != idVeiculo)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                if (!RegrasLocacao.ReservaPermiteAbrirLocacao(reserva.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido);
                }

                bool locacaoExistente = await ConsultaLocacoesAtivasDaReserva(idReserva)
                    .AnyAsync();

                if (locacaoExistente)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                Veiculo? veiculo = await ConsultaVeiculoAtivo(idVeiculo)
                    .FirstOrDefaultAsync();

                if (veiculo == null || !RegrasLocacao.VeiculoPermiteAbrirLocacao(veiculo.Status))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                bool funcionarioAtivo = await ConsultaFuncionarioAtivo(idFuncionario)
                    .AnyAsync();

                if (!funcionarioAtivo)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado);
                }

                DateTime momentoRegistro = DateTime.Now;
                Locacao locacao = RegrasLocacao.CriarLocacaoDeAbertura(reserva, veiculo, idFuncionario, dataRetirada, momentoRegistro, caucao);

                reserva.Status = RegrasLocacao.StatusReservaAposAbertura();
                reserva.UpdatedAt = momentoRegistro;
                veiculo.Status = RegrasLocacao.StatusVeiculoAposAbertura();
                veiculo.UpdatedAt = momentoRegistro;

                _db.Locacoes.Add(locacao);
                await _db.SaveChangesAsync();
                await transacao.CommitAsync();

                return locacao;
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Busca locacao ativa pelo identificador sem rastrear a entidade.</summary>
        public async Task<Locacao?> FindByIdAsync(int idLocacao)
        {
            try
            {
                Locacao? locacao = await ConsultaLocacaoAtiva(idLocacao)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                return locacao;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Releitura SEM trava da compensacao da caucao: existe locacao ativa da reserva com pagamento CAUCAO nao desativado de
        /// mesmo id_transacao? Com o filtro "Empresa" (o do pagamento vale pela locacao) e sem rastreamento.
        /// </summary>
        public async Task<bool> ExisteCaucaoGravadaAsync(int idReserva, string idTransacao)
        {
            try
            {
                bool existe = await ConsultaCaucaoGravadaDaReserva(idReserva, idTransacao)
                    .AsNoTracking()
                    .AnyAsync();

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Pagamentos CAUCAO nao desativados com o id_transacao informado, de locacao ativa da reserva. Com o filtro "Empresa" do
        /// pagamento (pela locacao): outra locadora nao devolve linha; sem contexto, nada volta.
        /// </summary>
        public IQueryable<Pagamento> ConsultaCaucaoGravadaDaReserva(int idReserva, string idTransacao)
        {
            try
            {
                IQueryable<Pagamento> consulta = _db.Pagamentos
                    .Where(x => x.Tipo == TipoPagamento.CAUCAO
                        && x.IdTransacao == idTransacao
                        && x.DisabledAt == null
                        && x.Locacao != null
                        && x.Locacao.IdReserva == idReserva
                        && x.Locacao.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Reserva nao desativada, pelo id. Com o filtro "Empresa" da locadora do contexto (outra locadora nao devolve linha).</summary>
        public IQueryable<Reserva> ConsultaReservaAtiva(int idReserva)
        {
            try
            {
                IQueryable<Reserva> consulta = _db.Reservas
                    .Where(x => x.IdReserva == idReserva && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Locacoes nao desativadas da reserva. Com o filtro "Empresa" pelo veiculo.</summary>
        public IQueryable<Locacao> ConsultaLocacoesAtivasDaReserva(int idReserva)
        {
            try
            {
                IQueryable<Locacao> consulta = _db.Locacoes
                    .Where(x => x.IdReserva == idReserva && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Funcionario ativo e nao desativado, pelo id. Com o filtro "Empresa" pelo id_empresa do funcionario.</summary>
        public IQueryable<Funcionario> ConsultaFuncionarioAtivo(int idFuncionario)
        {
            try
            {
                IQueryable<Funcionario> consulta = _db.Funcionarios
                    .Where(x => x.IdFuncionario == idFuncionario && x.Ativo && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Veiculo nao desativado, pelo id. Com o filtro "Empresa" pelo id_empresa do veiculo.</summary>
        public IQueryable<Veiculo> ConsultaVeiculoAtivo(int idVeiculo)
        {
            try
            {
                IQueryable<Veiculo> consulta = _db.Veiculos
                    .Where(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Locacao nao desativada, pelo id. Com o filtro "Empresa" pelo veiculo.</summary>
        public IQueryable<Locacao> ConsultaLocacaoAtiva(int idLocacao)
        {
            try
            {
                IQueryable<Locacao> consulta = _db.Locacoes
                    .Where(x => x.IdLocacao == idLocacao && x.DisabledAt == null);

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Executa a trava de linha (SELECT ... FOR UPDATE) na conexao do contexto, dentro da transacao aberta.
        /// E o unico ponto de SQL bruto do modulo; roda depois da leitura filtrada da reserva no metodo de abertura.
        /// </summary>
        private async Task<bool> LockActiveRowAsync(string comandoSql, int id)
        {
            try
            {
                DbConnection conexao = _db.Database.GetDbConnection();
                await using DbCommand comando = conexao.CreateCommand();
                comando.CommandText = comandoSql;
                comando.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();

                DbParameter parametroId = comando.CreateParameter();
                parametroId.ParameterName = "id";
                parametroId.Value = id;
                comando.Parameters.Add(parametroId);

                object? resultado = await comando.ExecuteScalarAsync();

                return resultado != null && resultado != DBNull.Value;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
