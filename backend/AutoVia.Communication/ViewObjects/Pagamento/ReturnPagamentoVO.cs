namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Saida de uma cobranca para a locadora (lancamento, baixa e listagem). Nunca traz o id da transacao do provedor.
    /// Datas: campo bruto (AAAA-MM-DD, JSON) e campo Str (dd/MM/yyyy). Valor: decimal e ValorStr (R$ 0,00).
    /// </summary>
    public class ReturnPagamentoVO
    {
        /// <summary>Id do pagamento.</summary>
        public int IdPagamento { get; set; }

        /// <summary>Id da locacao da cobranca; nulo quando e de venda.</summary>
        public int? IdLocacao { get; set; }

        /// <summary>Id da venda da cobranca; nulo quando e de locacao.</summary>
        public int? IdVenda { get; set; }

        /// <summary>Tipo gravado: DIARIA, COBRANCA_EXTRA, CAUCAO ou VENDA.</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Valor em reais.</summary>
        public decimal Valor { get; set; }

        /// <summary>Valor formatado em reais (ex.: R$ 395,60).</summary>
        public string ValorStr { get; set; } = string.Empty;

        /// <summary>Data de vencimento (AAAA-MM-DD).</summary>
        public DateOnly DataVencimento { get; set; }

        /// <summary>Data de vencimento formatada (dd/MM/yyyy).</summary>
        public string DataVencimentoStr { get; set; } = string.Empty;

        /// <summary>Data da baixa (AAAA-MM-DD); nula enquanto nao foi paga.</summary>
        public DateOnly? DataPagamento { get; set; }

        /// <summary>Data da baixa formatada (dd/MM/yyyy); nula enquanto nao foi paga.</summary>
        public string? DataPagamentoStr { get; set; }

        /// <summary>Forma usada na baixa (PIX, CARTAO, BOLETO, DINHEIRO ou TRANSFERENCIA); nula enquanto nao foi paga.</summary>
        public string? Forma { get; set; }

        /// <summary>Status gravado: PENDENTE, PAGO, ATRASADO ou CANCELADO.</summary>
        public string Status { get; set; } = string.Empty;
    }
}
