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
    public class PainelOperacionalRepository : IPainelOperacionalRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public PainelOperacionalRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<List<Reserva>> FindReservasDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia)
        {
            try
            {
                List<Reserva> reservas = await _db.Reservas
                    .AsNoTracking()
                    .Where(reserva => reserva.DisabledAt == null
                        && (reserva.Status == StatusReserva.ATIVA || reserva.Status == StatusReserva.CONFIRMADA)
                        && ((reserva.DataRetiradaPrevista >= inicioDoDia && reserva.DataRetiradaPrevista < inicioDoProximoDia)
                            || (reserva.DataDevolucaoPrevista >= inicioDoDia && reserva.DataDevolucaoPrevista < inicioDoProximoDia)))
                    .Include(reserva => reserva.Cliente)
                    .Include(reserva => reserva.Veiculo)
                    .ToListAsync();
                return reservas;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPainelOperacional, GetType().ToString());
                throw;
            }
        }

        public async Task<List<Locacao>> FindLocacoesDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia)
        {
            try
            {
                List<Locacao> locacoes = await _db.Locacoes
                    .AsNoTracking()
                    .Where(locacao => locacao.DisabledAt == null
                        && (locacao.Status == StatusLocacao.EM_ANDAMENTO || locacao.Status == StatusLocacao.ATRASADA)
                        && ((locacao.DataRetirada >= inicioDoDia && locacao.DataRetirada < inicioDoProximoDia)
                            || (locacao.DataDevolucaoPrevista >= inicioDoDia && locacao.DataDevolucaoPrevista < inicioDoProximoDia)))
                    .Include(locacao => locacao.Cliente)
                    .Include(locacao => locacao.Veiculo)
                    .ToListAsync();
                return locacoes;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPainelOperacional, GetType().ToString());
                throw;
            }
        }
    }
}
