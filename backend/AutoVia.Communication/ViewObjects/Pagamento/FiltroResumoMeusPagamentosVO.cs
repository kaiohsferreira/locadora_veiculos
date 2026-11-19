namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Filtro do resumo das cobrancas do cliente autenticado (GET /api/pagamentos/meus/resumo): so o periodo, que afeta a
    /// contagem da aba PAGOS. Os demais totais nao mudam com o periodo.
    /// </summary>
    public class FiltroResumoMeusPagamentosVO
    {
        /// <summary>Periodo: DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO. Ausente vale DOZE_MESES.</summary>
        public string? Periodo { get; set; }
    }
}
