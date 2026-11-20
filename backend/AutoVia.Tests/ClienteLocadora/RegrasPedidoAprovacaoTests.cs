using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;
using Xunit;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>Regras puras do teto de pedidos de aprovacao (item 6 de 20/11): limite atingido e janela de 24 horas, sem banco e sem servico.</summary>
    public class RegrasPedidoAprovacaoTests
    {
        private const int LimiteCinco = 5;
        private const int LimiteUm = 1;
        private const int PedidosZero = 0;
        private const int PedidosQuatro = 4;
        private const int PedidosCinco = 5;
        private const int PedidosSeis = 6;
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 20, 10, 0, 0);

        [Theory]
        [InlineData(PedidosZero, false)]
        [InlineData(PedidosQuatro, false)]
        [InlineData(PedidosCinco, true)]
        [InlineData(PedidosSeis, true)]
        public void AtingiuLimite_ComLimiteCinco_SoAtingeDoQuintoPedidoEmDiante(int locadorasPedidas, bool atingiu)
        {
            bool resultado = RegrasPedidoAprovacao.AtingiuLimite(locadorasPedidas, LimiteCinco);

            Assert.Equal(atingiu, resultado);
        }

        [Fact]
        public void AtingiuLimite_LimiteUmComUmPedido_EhVerdadeiro()
        {
            bool resultado = RegrasPedidoAprovacao.AtingiuLimite(1, LimiteUm);

            Assert.True(resultado);
        }

        [Fact]
        public void JanelaDesde_EhAgoraMenosAsHorasDaJanelaExatas()
        {
            DateTime desde = RegrasPedidoAprovacao.JanelaDesde(AgoraFixo);

            Assert.Equal(AgoraFixo.AddHours(-ConstantsValues.JanelaPedidosAprovacaoHoras), desde);
            Assert.Equal(AgoraFixo.AddHours(-24), desde);
        }
    }
}
