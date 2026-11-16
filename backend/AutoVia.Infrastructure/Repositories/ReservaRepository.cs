using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<Veiculo?> FindVeiculoParaReservaAsync(int idVeiculo)
        {
            try
            {
                Veiculo? veiculo = await _db.Veiculos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdVeiculo == idVeiculo && x.DisabledAt == null);
                return veiculo;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
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

        public async Task<Reserva?> FindByIdAsync(int idReserva, int idCliente)
        {
            try
            {
                Reserva? reserva = await _db.Reservas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdReserva == idReserva && x.IdCliente == idCliente && x.DisabledAt == null);
                return reserva;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        public async Task<int> CountAsync(int idCliente, StatusReserva? status)
        {
            try
            {
                IQueryable<Reserva> consulta = AplicarFiltrosCliente(idCliente, status);
                int total = await consulta.CountAsync();
                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        public async Task<List<Reserva>> FindAllAsync(int idCliente, StatusReserva? status, int deslocamento, int quantidade)
        {
            try
            {
                List<Reserva> reservas = await AplicarFiltrosCliente(idCliente, status)
                    .OrderByDescending(x => x.DataRetiradaPrevista)
                    .ThenByDescending(x => x.IdReserva)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();
                return reservas;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        public async Task CancelAtomicallyAsync(int idReserva, int idCliente)
        {
            try
            {
                Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transacao = await _db.Database.BeginTransactionAsync();
                try
                {
                    Reserva? preliminar = await _db.Reservas.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.IdReserva == idReserva && x.IdCliente == idCliente && x.DisabledAt == null);

                    if (preliminar == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                    }

                    Veiculo? veiculo = await _db.Veiculos
                        .FromSqlRaw(SqlBloquearVeiculoAtivo, preliminar.IdVeiculo)
                        .FirstOrDefaultAsync();

                    Reserva? reserva = await _db.Reservas
                        .FirstOrDefaultAsync(x => x.IdReserva == idReserva && x.IdCliente == idCliente && x.DisabledAt == null);

                    if (reserva == null)
                    {
                        throw new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada);
                    }

                    if (reserva.Status == StatusReserva.CANCELADA)
                    {
                        await transacao.CommitAsync();
                        return;
                    }

                    if (reserva.Status == StatusReserva.CONCLUIDA)
                    {
                        throw new BusinessException(ConstantsMessage.ErroReservaConcluidaNaoCancelavel);
                    }

                    reserva.Status = StatusReserva.CANCELADA;
                    reserva.UpdatedAt = DateTime.Now;
                    await _db.SaveChangesAsync();

                    if (veiculo != null && (veiculo.Status == StatusVeiculo.DISPONIVEL || veiculo.Status == StatusVeiculo.RESERVADO))
                    {
                        bool outraReservaAtiva = await _db.Reservas.AsNoTracking().AnyAsync(x => x.IdVeiculo == reserva.IdVeiculo
                            && x.DisabledAt == null
                            && (x.Status == StatusReserva.ATIVA || x.Status == StatusReserva.CONFIRMADA));
                        StatusVeiculo statusCalculado = outraReservaAtiva ? StatusVeiculo.RESERVADO : StatusVeiculo.DISPONIVEL;
                        if (veiculo.Status != statusCalculado)
                        {
                            veiculo.Status = statusCalculado;
                            veiculo.UpdatedAt = DateTime.Now;
                            await _db.SaveChangesAsync();
                        }
                    }

                    await transacao.CommitAsync();
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
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
        }

        private IQueryable<Reserva> AplicarFiltrosCliente(int idCliente, StatusReserva? status)
        {
            try
            {
                IQueryable<Reserva> consulta = _db.Reservas.AsNoTracking()
                    .Where(x => x.DisabledAt == null && x.IdCliente == idCliente);
                if (status.HasValue)
                {
                    StatusReserva statusFiltro = status.Value;
                    consulta = consulta.Where(x => x.Status == statusFiltro);
                }
                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }
    }
}
