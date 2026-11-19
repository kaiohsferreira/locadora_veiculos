using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Filtros das cobrancas do cliente autenticado (GET /api/pagamentos/meus): aba, periodo e paginacao. So as proprias
    /// cobrancas; o periodo so limita o inicio pelo vencimento, e as pendentes aparecem sempre.
    /// </summary>
    public class FiltroMeusPagamentosVO : FiltroPaginacaoVO
    {
        /// <summary>Aba: TODOS, PENDENTES ou PAGOS. Ausente vale TODOS.</summary>
        public string? Aba { get; set; }

        /// <summary>Periodo: DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO. Ausente vale DOZE_MESES.</summary>
        public string? Periodo { get; set; }
    }
}
