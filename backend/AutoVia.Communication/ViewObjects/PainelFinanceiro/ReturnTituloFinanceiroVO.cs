namespace AutoVia.Communication.ViewObjects.PainelFinanceiro
{
    /// <summary>
    /// Uma linha da lista de titulos do painel financeiro. O CPF sai mascarado (NNN.***.***-NN) ou nulo; o CPF inteiro nunca sai.
    /// A situacao ja mostra PENDENTE vencido como ATRASADO (mesma regra da tela 30). CAUCAO nunca aparece.
    /// </summary>
    public class ReturnTituloFinanceiroVO
    {
        /// <summary>Id do pagamento (cobranca).</summary>
        public int IdPagamento { get; set; }

        /// <summary>Origem: LOCACAO ou VENDA.</summary>
        public string Origem { get; set; } = string.Empty;

        /// <summary>Texto da origem: Locação nº {id} ou Venda nº {id}.</summary>
        public string OrigemStr { get; set; } = string.Empty;

        /// <summary>Tipo do lancamento: DIARIA, COBRANCA_EXTRA ou VENDA.</summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>Rotulo do tipo: Diária, Cobrança extra ou Venda.</summary>
        public string TipoStr { get; set; } = string.Empty;

        /// <summary>Nome do cliente dono da origem.</summary>
        public string ClienteNome { get; set; } = string.Empty;

        /// <summary>CPF mascarado NNN.***.***-NN, ou nulo quando o CPF nao tem 11 digitos.</summary>
        public string? ClienteCpfMascarado { get; set; }

        /// <summary>Valor da cobranca.</summary>
        public decimal Valor { get; set; }

        /// <summary>Valor em moeda.</summary>
        public string ValorStr { get; set; } = string.Empty;

        /// <summary>Vencimento em dd/MM/yyyy.</summary>
        public string DataVencimentoStr { get; set; } = string.Empty;

        /// <summary>Dias do vencimento ate hoje: zero vence hoje, positivo falta, negativo ja venceu.</summary>
        public int DiasParaVencer { get; set; }

        /// <summary>Situacao: PENDENTE, ATRASADO ou PAGO (PENDENTE vencido aparece como ATRASADO).</summary>
        public string Situacao { get; set; } = string.Empty;

        /// <summary>Data do pagamento em dd/MM/yyyy, ou nulo quando nao foi pago.</summary>
        public string? DataPagamentoStr { get; set; }

        /// <summary>Forma do pagamento, ou nulo.</summary>
        public string? Forma { get; set; }

        /// <summary>Verdadeiro quando a situacao e PENDENTE ou ATRASADO (o botao Dar baixa aparece).</summary>
        public bool PodeDarBaixa { get; set; }
    }
}
