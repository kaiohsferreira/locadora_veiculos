namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>Entrada da baixa de cobranca (PATCH /api/pagamentos/{id}/baixa): so a forma, pelo nome exato.</summary>
    public class BaixarPagamentoVO
    {
        /// <summary>Forma pelo nome exato (depois de Trim): PIX, CARTAO, BOLETO, DINHEIRO ou TRANSFERENCIA.</summary>
        public string? Forma { get; set; }

        /// <summary>
        /// Confirmacao explicita (padrao falso) para dar baixa por outra forma que nao PIX quando a cobranca tem PIX gerado
        /// (regra P6 de 20/11/2026). Sem ela, a baixa responde 409 e a cobranca nao recebe duas vezes.
        /// </summary>
        public bool ConfirmarBaixaComPixGerado { get; set; } = false;
    }
}
