using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public class FakePainelOperacionalRepository : IPainelOperacionalRepository
    {
        public List<Reserva> Reservas { get; } = new List<Reserva>();
        public List<Locacao> Locacoes { get; } = new List<Locacao>();
        public bool LancarExcecao { get; set; }
        public DateTime? InicioRecebido { get; private set; }
        public DateTime? ProximoDiaRecebido { get; private set; }

        public Task<List<Reserva>> FindReservasDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia)
        {
            InicioRecebido = inicioDoDia;
            ProximoDiaRecebido = inicioDoProximoDia;
            if (LancarExcecao)
            {
                throw new InvalidOperationException();
            }

            List<Reserva> resultado = Reservas
                .Where(reserva => reserva.DisabledAt == null
                    && (reserva.Status == StatusReserva.ATIVA || reserva.Status == StatusReserva.CONFIRMADA)
                    && ((reserva.DataRetiradaPrevista >= inicioDoDia && reserva.DataRetiradaPrevista < inicioDoProximoDia)
                        || (reserva.DataDevolucaoPrevista >= inicioDoDia && reserva.DataDevolucaoPrevista < inicioDoProximoDia)))
                .ToList();
            return Task.FromResult(resultado);
        }

        public Task<List<Locacao>> FindLocacoesDoDiaAsync(DateTime inicioDoDia, DateTime inicioDoProximoDia)
        {
            InicioRecebido = inicioDoDia;
            ProximoDiaRecebido = inicioDoProximoDia;
            if (LancarExcecao)
            {
                throw new InvalidOperationException();
            }

            List<Locacao> resultado = Locacoes
                .Where(locacao => locacao.DisabledAt == null
                    && (locacao.Status == StatusLocacao.EM_ANDAMENTO || locacao.Status == StatusLocacao.ATRASADA)
                    && ((locacao.DataRetirada >= inicioDoDia && locacao.DataRetirada < inicioDoProximoDia)
                        || (locacao.DataDevolucaoPrevista >= inicioDoDia && locacao.DataDevolucaoPrevista < inicioDoProximoDia)))
                .ToList();
            return Task.FromResult(resultado);
        }
    }
}
