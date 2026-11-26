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

        /// <summary>Lado do carro do desenho da vistoria; nulo em avarias anteriores a v4 e nas novas avarias da saida.</summary>
        [Column("angulo")]
        public AnguloAvaria? Angulo { get; set; }

        /// <summary>Posicao X do marcador no quadrante do desenho, em percentual de 0 a 100 (NUMERIC(5,2)); nula quando nao informada.</summary>
        [Column("pos_x")]
        public decimal? PosX { get; set; }

        /// <summary>Posicao Y do marcador no quadrante do desenho, em percentual de 0 a 100 (NUMERIC(5,2)); nula quando nao informada.</summary>
        [Column("pos_y")]
        public decimal? PosY { get; set; }

        [Column("id_avaria_entrada")]
        public int? IdAvariaEntrada { get; set; }

        public virtual Avaria? AvariaEntrada { get; set; }

        // --- Navegacao ---
        public virtual Vistoria Vistoria { get; set; } = null!;
    }
}
