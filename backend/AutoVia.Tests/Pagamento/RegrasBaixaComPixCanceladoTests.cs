using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Tabela verdade das regras puras da decisao 2 (21/11/2026): quando a baixa manual cancela o PIX antes e quando um PIX
    /// valido e nao cancelado bloqueia a baixa (reconferencia dentro da trava).
    /// </summary>
    public class RegrasBaixaComPixCanceladoTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 19, 10, 0, 0);

        [Theory]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, "tx", 15, "DINHEIRO", true, true)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, "tx", 15, "DINHEIRO", false, false)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, "tx", 15, "PIX", true, false)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, "tx", -1, "DINHEIRO", true, false)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, null, 15, "DINHEIRO", true, false)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PAGO, "tx", 15, "DINHEIRO", true, false)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.CANCELADO, "tx", 15, "DINHEIRO", true, false)]
        [InlineData(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, "tx", 15, "DINHEIRO", true, false)]
        [InlineData(TipoPagamento.COBRANCA_EXTRA, StatusPagamento.ATRASADO, "tx", 15, "TRANSFERENCIA", true, true)]
        public void PrecisaCancelarPixAntesDaBaixa_TabelaVerdade(TipoPagamento tipo, StatusPagamento status, string? idTransacao, int? minutosAteExpirar, string forma, bool confirmar, bool esperado)
        {
            DateTime? pixExpiraEm = minutosAteExpirar.HasValue ? Agora.AddMinutes(minutosAteExpirar.Value) : null;

            Assert.Equal(esperado, RegrasPagamento.PrecisaCancelarPixAntesDaBaixa(tipo, status, idTransacao, pixExpiraEm, forma, confirmar, Agora));
        }

        [Theory]
        [InlineData("tx", 15, "DINHEIRO", null, true)]
        [InlineData("tx", 15, "DINHEIRO", "tx", false)]
        [InlineData("tx-nova", 15, "DINHEIRO", "tx", true)]
        [InlineData("tx", -1, "DINHEIRO", null, false)]
        [InlineData(null, 15, "DINHEIRO", null, false)]
        [InlineData("tx", 15, "PIX", null, false)]
        [InlineData("tx", 15, "pix", null, false)]
        public void PixValidoSemCancelamento_TabelaVerdade(string? idTransacao, int? minutosAteExpirar, string forma, string? idTransacaoCancelado, bool esperado)
        {
            DateTime? pixExpiraEm = minutosAteExpirar.HasValue ? Agora.AddMinutes(minutosAteExpirar.Value) : null;

            Assert.Equal(esperado, RegrasPagamento.PixValidoSemCancelamento(idTransacao, pixExpiraEm, forma, idTransacaoCancelado, Agora));
        }
    }
}
