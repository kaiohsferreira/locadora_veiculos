using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>Tabela veiculo: frota da locadora (para aluguel e para venda).</summary>
    public class Veiculo : BaseEntities
    {
        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        /// <summary>CHAR(7), sem hifen (padrao antigo ou Mercosul).</summary>
        [Column("placa")]
        public string Placa { get; set; } = string.Empty;

        [Column("chassi")]
        public string Chassi { get; set; } = string.Empty;

        [Column("renavam")]
        public string Renavam { get; set; } = string.Empty;

        [Column("marca")]
        public string Marca { get; set; } = string.Empty;

        [Column("modelo")]
        public string Modelo { get; set; } = string.Empty;

        /// <summary>SMALLINT.</summary>
        [Column("ano_fabricacao")]
        public short AnoFabricacao { get; set; }

        /// <summary>SMALLINT, nunca menor que ano_fabricacao (ck_veiculo_ano).</summary>
        [Column("ano_modelo")]
        public short AnoModelo { get; set; }

        [Column("cor")]
        public string? Cor { get; set; }

        /// <summary>DEFAULT 0, nunca negativo (ck_veiculo_km).</summary>
        [Column("km_atual")]
        public int KmAtual { get; set; }

        /// <summary>NUMERIC(10,2), maior que zero (ck_veiculo_diaria).</summary>
        [Column("valor_diaria")]
        public decimal ValorDiaria { get; set; }

        /// <summary>NUMERIC(12,2), preenchido quando o veiculo esta a venda.</summary>
        [Column("valor_venda")]
        public decimal? ValorVenda { get; set; }

        /// <summary>DEFAULT 'DISPONIVEL'.</summary>
        [Column("status")]
        public StatusVeiculo Status { get; set; } = StatusVeiculo.DISPONIVEL;

        [Column("data_aquisicao")]
        public DateOnly? DataAquisicao { get; set; }

        // --- Navegacao ---
        public virtual Categoria Categoria { get; set; } = null!;
        public virtual List<Reserva> Reservas { get; set; } = new List<Reserva>();
        public virtual List<Locacao> Locacoes { get; set; } = new List<Locacao>();
        public virtual List<Manutencao> Manutencoes { get; set; } = new List<Manutencao>();
        public virtual List<Seguro> Seguros { get; set; } = new List<Seguro>();
        public virtual List<Sinistro> Sinistros { get; set; } = new List<Sinistro>();

        /// <summary>
        /// Vendas do veiculo. Regra de negocio: no maximo UMA venda ativa (DisabledAt nulo)
        /// por veiculo, validada no servico, sem indice unico no banco.
        /// </summary>
        public virtual List<Venda> Vendas { get; set; } = new List<Venda>();
    }
}
