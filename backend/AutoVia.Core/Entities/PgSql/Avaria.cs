using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela avaria: dano registrado em uma vistoria.</summary>
    public class Avaria : BaseEntities
    {
        [Column("id_avaria")]
        public int IdAvaria { get; set; }

        [Column("id_vistoria")]
        public int IdVistoria { get; set; }

        [Column("descricao")]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>DEFAULT 'LEVE'.</summary>
        [Column("gravidade")]
        public GravidadeAvaria Gravidade { get; set; } = GravidadeAvaria.LEVE;

        [Column("valor_estimado")]
        public decimal? ValorEstimado { get; set; }

        // --- Navegacao ---
        public virtual Vistoria Vistoria { get; set; } = null!;
    }
}
