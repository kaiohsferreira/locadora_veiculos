using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using System;

namespace AutoVia.Tests.Reservas
{
    /// <summary>
    /// Regra pura do conflito de periodo da criacao de reserva (RegrasReserva.SobrepoePeriodo, usada no ReservaRepository
    /// dentro da transacao). Prova, sem banco, a sobreposicao: quem se sobrepoe conflita; quem apenas encosta (a devolucao
    /// de uma reserva no mesmo dia da retirada pedida, ou o contrario) nao conflita. O erro ErroReservaConflitoPeriodo
    /// so e lancado dentro da transacao, entao aqui se prova o predicado que decide o conflito.
    /// </summary>
    public class RegrasSobrepoePeriodoTests
    {
        private const int AnoDoTeste = 2026;
        private const int MesDoTeste = 12;

        /// <summary>Reserva ja existente (retirada e devolucao previstas, em dias de dezembro) confrontada com um periodo pedido.</summary>
        [Theory]
        [InlineData(10, 12, 11, 13, true)]
        [InlineData(10, 12, 10, 12, true)]
        [InlineData(10, 15, 11, 12, true)]
        [InlineData(11, 12, 10, 13, true)]
        [InlineData(10, 12, 10, 11, true)]
        [InlineData(10, 12, 12, 14, false)]
        [InlineData(12, 14, 10, 12, false)]
        [InlineData(10, 12, 5, 9, false)]
        [InlineData(10, 12, 13, 15, false)]
        public void SobrepoePeriodo_DecideSePeriodoExistenteConflitaComOPedido(int diaRetiradaReserva, int diaDevolucaoReserva, int diaRetiradaPedida, int diaDevolucaoPedida, bool conflita)
        {
            Reserva reservaExistente = new Reserva
            {
                DataRetiradaPrevista = Data(diaRetiradaReserva),
                DataDevolucaoPrevista = Data(diaDevolucaoReserva)
            };
            Func<Reserva, bool> predicado = RegrasReserva.SobrepoePeriodo(Data(diaRetiradaPedida), Data(diaDevolucaoPedida)).Compile();

            bool resultado = predicado(reservaExistente);

            Assert.Equal(conflita, resultado);
        }

        private static DateTime Data(int dia)
        {
            DateTime data = new DateTime(AnoDoTeste, MesDoTeste, dia);

            return data;
        }
    }
}
