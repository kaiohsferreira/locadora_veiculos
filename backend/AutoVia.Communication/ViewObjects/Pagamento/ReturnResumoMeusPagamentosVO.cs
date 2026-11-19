namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Resumo das cobrancas do cliente autenticado (GET /api/pagamentos/meus/resumo): totais do exercicio, pendencias e
    /// contagens das abas. Valores em reais com campo Str (R$ 0,00). Sem caucao nos totais e nas contagens.
    /// </summary>
    public class ReturnResumoMeusPagamentosVO
    {
        /// <summary>Ano do exercicio (ano corrente pelo relogio do servidor).</summary>
        public int AnoExercicio { get; set; }

        /// <summary>Total liquidado no ano corrente (pagamentos PAGO, sem caucao, com data de pagamento no ano).</summary>
        public decimal TotalLiquidadoAno { get; set; }

        /// <summary>Total liquidado no ano formatado em reais.</summary>
        public string TotalLiquidadoAnoStr { get; set; } = string.Empty;

        /// <summary>Quantidade de cobrancas quitadas no ano corrente.</summary>
        public int QuantidadeQuitadasAno { get; set; }

        /// <summary>Total pendente (PENDENTE ou ATRASADO, sem caucao), sem periodo.</summary>
        public decimal TotalPendente { get; set; }

        /// <summary>Total pendente formatado em reais.</summary>
        public string TotalPendenteStr { get; set; } = string.Empty;

        /// <summary>Quantidade de cobrancas pendentes, sem periodo.</summary>
        public int QuantidadePendentes { get; set; }

        /// <summary>Contagem da aba Pendentes (igual a QuantidadePendentes).</summary>
        public int QuantidadeAbaPendentes { get; set; }

        /// <summary>Contagem da aba Pagos: pagos sem caucao dentro do periodo pedido.</summary>
        public int QuantidadeAbaPagos { get; set; }
    }
}
