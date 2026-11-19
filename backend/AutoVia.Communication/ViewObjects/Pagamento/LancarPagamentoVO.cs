namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Entrada do lancamento de cobranca (POST /api/pagamentos). Estrutura plana: origem por id (locacao OU venda, nunca
    /// as duas), tipo pelo nome, valor e data de vencimento. Nao tem id da locadora: vem do contexto.
    /// </summary>
    public class LancarPagamentoVO
    {
        /// <summary>Id da locacao da cobranca. Informe este ou IdVenda, nunca os dois.</summary>
        public int? IdLocacao { get; set; }

        /// <summary>Id da venda da cobranca. Informe este ou IdLocacao, nunca os dois.</summary>
        public int? IdVenda { get; set; }

        /// <summary>Tipo pelo nome exato: DIARIA, COBRANCA_EXTRA ou VENDA (a rota nao aceita CAUCAO).</summary>
        public string? Tipo { get; set; }

        /// <summary>Valor em reais: maior que zero, ate 10 digitos inteiros e 2 casas.</summary>
        public decimal Valor { get; set; }

        /// <summary>Data de vencimento (AAAA-MM-DD). Obrigatoria; pode ser passada (lancamento retroativo) ou futura.</summary>
        public DateOnly? DataVencimento { get; set; }
    }
}
