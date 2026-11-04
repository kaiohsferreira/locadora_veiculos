using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela categoria: grupo de veiculos com diaria base (ex.: Economico, SUV).</summary>
    public class Categoria : BaseEntities
    {
        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [Column("descricao")]
        public string? Descricao { get; set; }

        /// <summary>NUMERIC(10,2), maior que zero (ck_categoria_valor).</summary>
        [Column("valor_diaria_base")]
        public decimal ValorDiariaBase { get; set; }

        // --- Navegacao ---
        public virtual List<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    }
}
