namespace AutoVia.Communication.ViewObjects.Pagamento
{
    /// <summary>
    /// Saida de uma cobranca para o proprio cliente (GET /api/pagamentos/meus). Nunca traz o id da transacao, nem dado de
    /// outro cliente, nem dado da locadora alem da descricao do veiculo. Situacao e dias para vencer sao calculados no servidor.
    /// </summary>
    public class ReturnMeuPagamentoVO
    {
        /// <summary>Id da cobranca.</summary>
        public int IdPagamento { get; set; }

        /// <summary>Tipo: DIARIA, COBRANCA_EXTRA, CAUCAO ou VENDA.</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Valor em reais.</summary>
        public decimal Valor { get; set; }

        /// <summary>Valor formatado em reais (ex.: R$ 395,60).</summary>
        public string ValorStr { get; set; } = string.Empty;

        /// <summary>Data de vencimento (AAAA-MM-DD).</summary>
        public DateOnly DataVencimento { get; set; }

        /// <summary>Data de vencimento formatada (dd/MM/yyyy).</summary>
        public string DataVencimentoStr { get; set; } = string.Empty;

        /// <summary>Data da baixa formatada (dd/MM/yyyy); nula enquanto nao foi paga.</summary>
        public string? DataPagamentoStr { get; set; }

        /// <summary>Dias ate o vencimento: zero vence hoje, negativo ja venceu. Calculado no servidor.</summary>
        public int DiasParaVencer { get; set; }

        /// <summary>Situacao: PENDENTE, ATRASADO, PAGO ou CANCELADO (PENDENTE vencido vira ATRASADO). Calculada no servidor.</summary>
        public string Situacao { get; set; } = string.Empty;

        /// <summary>Forma usada na baixa; nula enquanto nao foi paga.</summary>
        public string? Forma { get; set; }

        /// <summary>Id da locacao da cobranca; nulo quando e de venda.</summary>
        public int? IdLocacao { get; set; }

        /// <summary>Id da venda da cobranca; nulo quando e de locacao.</summary>
        public int? IdVenda { get; set; }

        /// <summary>Marca e modelo do veiculo da locacao ou da venda; nulo quando nao houver veiculo.</summary>
        public string? VeiculoDescricao { get; set; }
    }
}
