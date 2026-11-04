using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela vistoria: inspecao de ENTRADA (retirada) ou SAIDA (devolucao).
    /// Uma unica vistoria de cada tipo por locacao (uq_vistoria_locacao_tipo).
    /// </summary>
    public class Vistoria : BaseEntities
    {
        [Column("id_vistoria")]
        public int IdVistoria { get; set; }

        [Column("id_locacao")]
        public int IdLocacao { get; set; }

        [Column("id_funcionario")]
        public int IdFuncionario { get; set; }

        [Column("tipo")]
        public TipoVistoria Tipo { get; set; }

        /// <summary>DEFAULT CURRENT_TIMESTAMP.</summary>
        [Column("data_hora")]
        public DateTime DataHora { get; set; }

        [Column("km_registrado")]
        public int KmRegistrado { get; set; }

        /// <summary>NUMERIC(4,2) entre 0 e 1 (ck_vistoria_combustivel): 0 = vazio, 1 = cheio.</summary>
        [Column("nivel_combustivel")]
        public decimal? NivelCombustivel { get; set; }

        [Column("observacoes")]
        public string? Observacoes { get; set; }

        // --- Navegacao ---
        public virtual Locacao Locacao { get; set; } = null!;
        public virtual Funcionario Funcionario { get; set; } = null!;
        public virtual List<Avaria> Avarias { get; set; } = new List<Avaria>();
    }
}
