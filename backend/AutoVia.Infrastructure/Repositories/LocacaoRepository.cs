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
    /// <summary>Persistencia das locacoes com transacao e bloqueio das linhas envolvidas.</summary>
    public class LocacaoRepository : ILocacaoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public LocacaoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        /// <summary>Busca reserva ativa com veiculo para validacao das regras de inicio.</summary>
        public async Task<Reserva?> FindReservaByIdAsync(int idReserva)
        {
            try
            {
                Reserva? reserva = await _db.Reservas
                    .AsNoTracking()
                    .Include(x => x.Veiculo)
                    .FirstOrDefaultAsync(x => x.IdReserva == idReserva && x.DisabledAt == null);

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
                Funcionario? funcionario = await _db.Funcionarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdFuncionario == idFuncionario && x.Ativo && x.DisabledAt == null);

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
                bool existe = await _db.Locacoes
                    .AsNoTracking()
                    .AnyAsync(x => x.IdReserva == idReserva && x.DisabledAt == null);

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Cria a locacao e muda reserva/veiculo na mesma transacao. FOR UPDATE serializa
        /// requisicoes para a mesma reserva e para reservas concorrentes do mesmo veiculo.
        /// </summary>
        public async Task<Locacao> StartFromReservaAsync(int idReserva, int idFuncionario)
        {
            try
            {
                await using IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();

                bool funcionarioBloqueado = await LockActiveRowAsync(
                    "SELECT id_funcionario FROM funcionario WHERE id_funcionario = @id AND ativo = TRUE AND disabled_at IS NULL FOR UPDATE",
                    idFuncionario);

                if (!funcionarioBloqueado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado);
                }

                bool reservaBloqueada = await LockActiveRowAsync(
                    "SELECT id_reserva FROM reserva WHERE id_reserva = @id AND disabled_at IS NULL FOR UPDATE",
                    idReserva);

                if (!reservaBloqueada)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                Reserva? reserva = await _db.Reservas
                    .FirstOrDefaultAsync(x => x.IdReserva == idReserva && x.DisabledAt == null);

                if (reserva == null)
                {
                    throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                }

                if (reserva.Status != StatusReserva.ATIVA && reserva.Status != StatusReserva.CONFIRMADA)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaEstadoInvalido);
                }

                if (await _db.Locacoes.AnyAsync(x => x.IdReserva == idReserva && x.DisabledAt == null))
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada);
                }

                bool veiculoBloqueado = await LockActiveRowAsync(
                    "SELECT id_veiculo FROM veiculo WHERE id_veiculo = @id AND disabled_at IS NULL FOR UPDATE",
                    reserva.IdVeiculo);

                if (!veiculoBloqueado)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                Veiculo? veiculo = await _db.Veiculos
                    .FirstOrDefaultAsync(x => x.IdVeiculo == reserva.IdVeiculo && x.DisabledAt == null);

                if (veiculo == null || veiculo.Status != StatusVeiculo.RESERVADO)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoVeiculoIndisponivel);
                }

                bool funcionarioAtivo = await _db.Funcionarios
                    .AsNoTracking()
                    .AnyAsync(x => x.IdFuncionario == idFuncionario && x.Ativo && x.DisabledAt == null);

                if (!funcionarioAtivo)
                {
                    throw new BusinessException(ConstantsMessage.ErroLocacaoFuncionarioNaoEncontrado);
                }

                DateTime agora = DateTime.Now;
                Locacao locacao = new Locacao
                {
                    IdReserva = reserva.IdReserva,
                    IdCliente = reserva.IdCliente,
                    IdVeiculo = veiculo.IdVeiculo,
                    IdFuncionario = idFuncionario,
                    DataRetirada = agora,
                    DataDevolucaoPrevista = reserva.DataDevolucaoPrevista,
                    KmSaida = veiculo.KmAtual,
                    ValorDiaria = veiculo.ValorDiaria,
                    ValorCaucao = 0m,
                    Status = StatusLocacao.EM_ANDAMENTO,
                    CreatedAt = agora,
                    UpdatedAt = agora,
                    DisabledAt = null
                };

                reserva.Status = StatusReserva.CONCLUIDA;
                reserva.UpdatedAt = agora;
                veiculo.Status = StatusVeiculo.LOCADO;
                veiculo.UpdatedAt = agora;

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
                Locacao? locacao = await _db.Locacoes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdLocacao == idLocacao && x.DisabledAt == null);

                return locacao;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

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
