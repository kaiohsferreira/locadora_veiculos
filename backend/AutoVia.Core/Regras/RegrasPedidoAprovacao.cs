using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras do teto de pedidos de aprovacao do cliente (item 6, 20/11): a janela de 24 horas e o limite de locadoras
    /// distintas. Sem banco e sem servico. Usadas pelo ClienteLocadoraService (janela) e pelo ClienteLocadoraRepository (limite,
    /// dentro da transacao da trava do cliente), e testadas direto em RegrasPedidoAprovacaoTests.
    /// </summary>
    public static class RegrasPedidoAprovacao
    {
        /// <summary>Data de corte da janela: pedidos com data de solicitacao igual ou posterior contam (fronteira fechada).</summary>
        public static DateTime JanelaDesde(DateTime agora)
        {
            return agora.AddHours(-ConstantsValues.JanelaPedidosAprovacaoHoras);
        }

        /// <summary>Verdadeiro quando o numero de locadoras distintas com pedido chega ao limite: o pedido novo nao passa.</summary>
        public static bool AtingiuLimite(int locadorasPedidas, int limite)
        {
            return locadorasPedidas >= limite;
        }
    }
}
