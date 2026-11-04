using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela manutencao: ordem de servico preventiva ou corretiva de um veiculo.</summary>
    public class Manutencao
    {
        [Column("id_manutencao")]
        public int IdManutencao { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("tipo")]
        public TipoManutencao Tipo { get; set; }

        [Column("data_entrada")]
        public DateOnly DataEntrada { get; set; }

        /// <summary>Nula enquanto a manutencao esta aberta; nunca anterior a data_entrada.</summary>
        [Column("data_saida")]
        public DateOnly? DataSaida { get; set; }

        [Column("km_registrado")]
        public int KmRegistrado { get; set; }

        [Column("oficina")]
        public string? Oficina { get; set; }

        [Column("descricao")]
        public string? Descricao { get; set; }

        [Column("custo")]
        public decimal? Custo { get; set; }

        // --- Navegacao ---
        public virtual Veiculo Veiculo { get; set; } = null!;
    }
}
