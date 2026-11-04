using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela pagamento: cobranca de uma locacao OU de uma venda, nunca das duas
    /// (ck_pagamento_origem). Exatamente um entre IdLocacao e IdVenda e preenchido.
    /// </summary>
    public class Pagamento
    {
        [Column("id_pagamento")]
        public int IdPagamento { get; set; }

        [Column("id_locacao")]
        public int? IdLocacao { get; set; }

        [Column("id_venda")]
        public int? IdVenda { get; set; }

        [Column("tipo")]
        public TipoPagamento Tipo { get; set; }

        /// <summary>NUMERIC(12,2), maior que zero (ck_pagamento_valor).</summary>
        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("data_vencimento")]
        public DateOnly DataVencimento { get; set; }

        [Column("data_pagamento")]
        public DateOnly? DataPagamento { get; set; }

        /// <summary>
        /// VARCHAR(20) livre: a especificacao nao define CHECK para esta coluna,
        /// por isso continua string (PIX, CARTAO, BOLETO...) e nao enum.
        /// </summary>
        [Column("forma")]
        public string? Forma { get; set; }

        /// <summary>DEFAULT 'PENDENTE'.</summary>
        [Column("status")]
        public StatusPagamento Status { get; set; } = StatusPagamento.PENDENTE;

        // --- Navegacao ---
        public virtual Locacao? Locacao { get; set; }
        public virtual Venda? Venda { get; set; }
    }
}
