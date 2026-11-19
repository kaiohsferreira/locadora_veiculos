using AutoVia.Communication.ViewObjects.Utils;

namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Filtros da listagem de cobrancas da locadora (GET /api/pagamentos): status exato, intervalo de vencimento e paginacao.
    /// So a locadora do contexto; inclui a caucao.
    /// </summary>
    public class FiltroPagamentoVO : FiltroPaginacaoVO
    {
        /// <summary>Status exato: PENDENTE, PAGO, ATRASADO ou CANCELADO. Opcional.</summary>
        public string? Status { get; set; }

        /// <summary>Inicio do intervalo de vencimento (AAAA-MM-DD), inclusive. Opcional.</summary>
        public DateOnly? VencimentoDe { get; set; }

        /// <summary>Fim do intervalo de vencimento (AAAA-MM-DD), inclusive. Opcional; nao pode ser menor que VencimentoDe.</summary>
        public DateOnly? VencimentoAte { get; set; }
    }
}
