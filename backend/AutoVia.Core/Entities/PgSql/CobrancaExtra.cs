using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela cobranca_extra: valores apurados na devolucao (km excedente, atraso, avaria...).</summary>
    public class CobrancaExtra : BaseEntities
    {
        [Column("id_cobranca")]
        public int IdCobranca { get; set; }

        [Column("id_locacao")]
        public int IdLocacao { get; set; }

        [Column("tipo")]
        public TipoCobrancaExtra Tipo { get; set; }

        [Column("descricao")]
        public string? Descricao { get; set; }

        /// <summary>NUMERIC(10,2), nunca negativo (ck_cobranca_valor).</summary>
        [Column("valor")]
        public decimal Valor { get; set; }

        /// <summary>DEFAULT CURRENT_DATE.</summary>
        [Column("data_lancamento")]
        public DateOnly DataLancamento { get; set; }

        // --- Navegacao ---
        public virtual Locacao Locacao { get; set; } = null!;
    }
}
