using AutoVia.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela locacao: retirada (check-in) ate a devolucao (check-out).
    ///
    /// km_chegada, data_devolucao_efetiva e valor_total so sao preenchidos na
    /// devolucao, por isso sao anulaveis: uma locacao EM_ANDAMENTO nao os tem.
    /// </summary>
    public class Locacao
    {
        [Column("id_locacao")]
        public int IdLocacao { get; set; }

        /// <summary>UNIQUE e anulavel (a especificacao permite locacao sem reserva de origem).</summary>
        [Column("id_reserva")]
        public int? IdReserva { get; set; }

        [Column("id_cliente")]
        public int IdCliente { get; set; }

        [Column("id_veiculo")]
        public int IdVeiculo { get; set; }

        [Column("id_funcionario")]
        public int IdFuncionario { get; set; }

        [Column("data_retirada")]
        public DateTime DataRetirada { get; set; }

        [Column("data_devolucao_prevista")]
        public DateTime DataDevolucaoPrevista { get; set; }

        /// <summary>Preenchida na devolucao; nunca anterior a data_retirada (ck_locacao_devolucao).</summary>
        [Column("data_devolucao_efetiva")]
        public DateTime? DataDevolucaoEfetiva { get; set; }

        [Column("km_saida")]
        public int KmSaida { get; set; }

        /// <summary>Preenchido na devolucao; nunca menor que km_saida (ck_locacao_km).</summary>
        [Column("km_chegada")]
        public int? KmChegada { get; set; }

        [Column("valor_diaria")]
        public decimal ValorDiaria { get; set; }

        /// <summary>DEFAULT 0.</summary>
        [Column("valor_caucao")]
        public decimal ValorCaucao { get; set; }

        /// <summary>NUMERIC(12,2), apurado na devolucao.</summary>
        [Column("valor_total")]
        public decimal? ValorTotal { get; set; }

        /// <summary>DEFAULT 'EM_ANDAMENTO'.</summary>
        [Column("status")]
        public StatusLocacao Status { get; set; } = StatusLocacao.EM_ANDAMENTO;

        // --- Navegacao ---
        public virtual Reserva? Reserva { get; set; }
        public virtual Cliente Cliente { get; set; } = null!;
        public virtual Veiculo Veiculo { get; set; } = null!;
        public virtual Funcionario Funcionario { get; set; } = null!;
        public virtual List<Vistoria> Vistorias { get; set; } = new List<Vistoria>();
        public virtual List<CobrancaExtra> CobrancasExtras { get; set; } = new List<CobrancaExtra>();
        public virtual List<Pagamento> Pagamentos { get; set; } = new List<Pagamento>();
        public virtual List<Sinistro> Sinistros { get; set; } = new List<Sinistro>();
    }
}
