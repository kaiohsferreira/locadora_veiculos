using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela reserva: veiculo reservado por um cliente para um periodo.</summary>
    public class Reserva
    {
        [Column("id_reserva")]
        public int IdReserva { get; set; }

        [Column("id_cliente")]
        public int IdCliente { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        /// <summary>DEFAULT CURRENT_TIMESTAMP.</summary>
        [Column("data_reserva")]
        public DateTime DataReserva { get; set; }

        [Column("data_retirada_prevista")]
        public DateTime DataRetiradaPrevista { get; set; }

        /// <summary>Sempre posterior a data_retirada_prevista (ck_reserva_periodo).</summary>
        [Column("data_devolucao_prevista")]
        public DateTime DataDevolucaoPrevista { get; set; }

        [Column("valor_estimado")]
        public decimal? ValorEstimado { get; set; }

        /// <summary>DEFAULT 'ATIVA'.</summary>
        [Column("status")]
        public StatusReserva Status { get; set; } = StatusReserva.ATIVA;

        // --- Navegacao ---
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Veiculo Veiculo { get; set; } = null!;

        /// <summary>Uma reserva origina no maximo uma locacao (locacao.id_reserva e UNIQUE).</summary>
        public virtual Locacao? Locacao { get; set; }
    }
}
