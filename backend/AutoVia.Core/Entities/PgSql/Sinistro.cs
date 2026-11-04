using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela sinistro: ocorrencia com o veiculo, opcionalmente ligada a apolice e a locacao.</summary>
    public class Sinistro : BaseEntities
    {
        [Column("id_sinistro")]
        public int IdSinistro { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("id_seguro")]
        public int? IdSeguro { get; set; }

        [Column("id_locacao")]
        public int? IdLocacao { get; set; }

        [Column("data_ocorrencia")]
        public DateOnly DataOcorrencia { get; set; }

        [Column("tipo")]
        public TipoSinistro Tipo { get; set; }

        [Column("descricao")]
        public string? Descricao { get; set; }

        [Column("valor_prejuizo")]
        public decimal? ValorPrejuizo { get; set; }

        /// <summary>DEFAULT 'ABERTO'.</summary>
        [Column("status")]
        public StatusSinistro Status { get; set; } = StatusSinistro.ABERTO;

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual Seguro? Seguro { get; set; }
        public virtual Locacao? Locacao { get; set; }
    }
}
