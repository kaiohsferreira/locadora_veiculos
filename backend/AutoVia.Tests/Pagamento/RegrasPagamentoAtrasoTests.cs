using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Regra pura da rotina de atraso (RF-34, contrato pagamento-atraso v1): DeveVirarAtrasado. So PENDENTE sem caucao com
    /// vencimento estritamente anterior a hoje vira ATRASADO. Testada direto, sem banco.
    /// </summary>
    public class RegrasPagamentoAtrasoTests
    {
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 19);

        [Fact]
        public void DeveVirarAtrasado_PendenteVencidoOntem_Verdadeiro()
        {
            Assert.True(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje.AddDays(-1), Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_PendenteVencendoHoje_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje, Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_PendenteFuturo_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje.AddDays(1), Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_JaAtrasado_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.ATRASADO, Hoje.AddDays(-1), Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_Pago_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.PAGO, Hoje.AddDays(-1), Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_Cancelado_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.DIARIA, StatusPagamento.CANCELADO, Hoje.AddDays(-1), Hoje));
        }

        [Fact]
        public void DeveVirarAtrasado_CaucaoPendenteVencida_Falso()
        {
            Assert.False(RegrasPagamento.DeveVirarAtrasado(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, Hoje.AddDays(-1), Hoje));
        }
    }
}
