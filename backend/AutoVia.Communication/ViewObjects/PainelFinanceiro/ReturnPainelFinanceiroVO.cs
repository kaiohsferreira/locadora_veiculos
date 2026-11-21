using System;

namespace AutoVia.Communication.ViewObjects.PainelFinanceiro
{
    /// <summary>
    /// Indicadores do painel financeiro (GET /api/painel/financeiro): recebido (pela data do pagamento), a receber, em atraso
    /// e ticket medio (pelo vencimento). Cada valor vem com o texto em moeda. Caucao fica fora de tudo.
    /// </summary>
    public class ReturnPainelFinanceiroVO
    {
        /// <summary>Data inicial do periodo usado (AAAA-MM-DD).</summary>
        public DateOnly De { get; set; }

        /// <summary>Data final do periodo usado (AAAA-MM-DD).</summary>
        public DateOnly Ate { get; set; }

        /// <summary>Data inicial em dd/MM/yyyy.</summary>
        public string DeStr { get; set; } = string.Empty;

        /// <summary>Data final em dd/MM/yyyy.</summary>
        public string AteStr { get; set; } = string.Empty;

        /// <summary>Soma dos pagamentos recebidos no periodo (data de pagamento no periodo).</summary>
        public decimal RecebidoValor { get; set; }

        /// <summary>Recebido em moeda.</summary>
        public string RecebidoStr { get; set; } = string.Empty;

        /// <summary>Quantidade de pagamentos recebidos no periodo.</summary>
        public int QuantidadeRecebido { get; set; }

        /// <summary>Soma dos pagamentos a receber (pendentes ainda nao vencidos, vencimento no periodo).</summary>
        public decimal AReceberValor { get; set; }

        /// <summary>A receber em moeda.</summary>
        public string AReceberStr { get; set; } = string.Empty;

        /// <summary>Quantidade a receber.</summary>
        public int QuantidadeAReceber { get; set; }

        /// <summary>Soma dos pagamentos em atraso (atrasados ou pendentes vencidos, vencimento no periodo).</summary>
        public decimal EmAtrasoValor { get; set; }

        /// <summary>Em atraso em moeda.</summary>
        public string EmAtrasoStr { get; set; } = string.Empty;

        /// <summary>Quantidade em atraso.</summary>
        public int QuantidadeEmAtraso { get; set; }

        /// <summary>Ticket medio: soma dos pagos com vencimento no periodo dividida pela quantidade (duas casas).</summary>
        public decimal TicketMedioValor { get; set; }

        /// <summary>Ticket medio em moeda.</summary>
        public string TicketMedioStr { get; set; } = string.Empty;

        /// <summary>Divisor do ticket medio: quantidade de pagamentos pagos no periodo.</summary>
        public int QuantidadeTicket { get; set; }
    }
}
