using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private const string SqlBloquearVeiculoAtivo = "SELECT * FROM veiculo WHERE id_veiculo = {0} AND disabled_at IS NULL FOR UPDATE";
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public ReservaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<Reserva> InsertAtomicallyAsync(Reserva reserva)
        {
            try
            {
                Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();
                try
                {
                    Veiculo? veiculo = await _db.Veiculos
                        .FromSqlRaw(SqlBloquearVeiculoAtivo, reserva.IdVeiculo)
                        .FirstOrDefaultAsync();

                    if (veiculo == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaVeiculoNaoEncontrado);
                    }

                    if (veiculo.Status != StatusVeiculo.DISPONIVEL && veiculo.Status != StatusVeiculo.RESERVADO)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaVeiculoIndisponivel);
                    }

                    bool conflito = await _db.Reservas.AsNoTracking().AnyAsync(x => x.IdVeiculo == reserva.IdVeiculo
                        && x.DisabledAt == null
                        && (x.Status == StatusReserva.ATIVA || x.Status == StatusReserva.CONFIRMADA)
                        && x.DataRetiradaPrevista < reserva.DataDevolucaoPrevista
                        && x.DataDevolucaoPrevista > reserva.DataRetiradaPrevista);

                    if (conflito)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaConflitoPeriodo);
                    }

                    _db.Reservas.Add(reserva);
                    veiculo.Status = StatusVeiculo.RESERVADO;
                    veiculo.UpdatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();
                    await transacao.CommitAsync();
                    return reserva;
                }
                catch
                {
                    await transacao.RollbackAsync();
                    throw;
                }
                finally
                {
                    await transacao.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }
    }
}
