using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela seguro: apolice de um veiculo.</summary>
    public class Seguro : BaseEntities
    {
        [Column("id_seguro")]
        public int IdSeguro { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("seguradora")]
        public string Seguradora { get; set; } = string.Empty;

        /// <summary>Unico entre os seguros ativos: validado no servico, sem indice unico no banco.</summary>
        [Column("numero_apolice")]
        public string NumeroApolice { get; set; } = string.Empty;

        [Column("data_inicio")]
        public DateOnly DataInicio { get; set; }

        /// <summary>Sempre posterior a data_inicio (ck_seguro_vigencia).</summary>
        [Column("data_fim")]
        public DateOnly DataFim { get; set; }

        [Column("valor_premio")]
        public decimal ValorPremio { get; set; }

        [Column("cobertura")]
        public string? Cobertura { get; set; }

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual List<Sinistro> Sinistros { get; set; } = new List<Sinistro>();
    }
}
