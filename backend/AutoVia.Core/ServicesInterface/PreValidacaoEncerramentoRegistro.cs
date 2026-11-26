namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Resultado da pre-validacao do encerramento, sem gravar e sem provedor (cobrancas-extras v4/v6, passo 7 do POST de devolucao).
    /// </summary>
    public sealed class PreValidacaoEncerramentoRegistro
    {
        /// <summary>Total das extras (T): do snapshot quando a apuracao ja existe, ou o calculo sem gravar.</summary>
        public decimal Total { get; set; }

        /// <summary>Verdadeiro quando a apuracao ja foi gravada (imutavel).</summary>
        public bool ApuracaoExistente { get; set; }

        /// <summary>Pagamento COBRANCA_EXTRA lancado pela apuracao (snapshot); nulo em apuracao antiga ou sem cobranca.</summary>
        public int? IdPagamentoLancado { get; set; }

        /// <summary>Quantidade de COBRANCA_EXTRA PENDENTE ativas da locacao.</summary>
        public int QuantidadeCobrancasPendentes { get; set; }
    }
}
