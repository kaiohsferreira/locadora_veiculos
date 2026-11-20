using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
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
                List<Locacao> locacoes = await ConsultaLocacoesDoDia(inicioDoDia, inicioDoProximoDia)
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

        /// <summary>
        /// Consulta nomeada: locacoes nao desativadas em aberto (RegrasStatusVeiculo.StatusLocacoesEmAberto, EM_ANDAMENTO e ATRASADA)
        /// com retirada ou devolucao prevista em [inicioDoDia, inicioDoProximoDia). Sem Include: quem chama acrescenta.
        /// </summary>
        public IQueryable<Locacao> ConsultaLocacoesDoDia(DateTime inicioDoDia, DateTime inicioDoProximoDia)
        {
            try
            {
                StatusLocacao[] statusAbertos = RegrasStatusVeiculo.StatusLocacoesEmAberto();

                IQueryable<Locacao> consulta = _db.Locacoes
                    .AsNoTracking()
                    .Where(locacao => locacao.DisabledAt == null
                        && statusAbertos.Contains(locacao.Status)
                        && ((locacao.DataRetirada >= inicioDoDia && locacao.DataRetirada < inicioDoProximoDia)
                            || (locacao.DataDevolucaoPrevista >= inicioDoDia && locacao.DataDevolucaoPrevista < inicioDoProximoDia)));

                return consulta;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPainelOperacional, GetType().ToString());
                throw;
            }
        }
    }
}
