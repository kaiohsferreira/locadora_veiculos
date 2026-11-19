namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>Entrada da baixa de cobranca (PATCH /api/pagamentos/{id}/baixa): so a forma, pelo nome exato.</summary>
    public class BaixarPagamentoVO
    {
        /// <summary>Forma pelo nome exato (depois de Trim): PIX, CARTAO, BOLETO, DINHEIRO ou TRANSFERENCIA.</summary>
        public string? Forma { get; set; }
    }
}
