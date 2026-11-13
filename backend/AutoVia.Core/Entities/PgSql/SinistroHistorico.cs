using AutoVia.Communication.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities.PgSql
{
    /// <summary>
    /// Tabela sinistro_historico: linha do tempo do sinistro (RF-43). Cada linha e uma
    /// mudanca de status, com quem mudou (origem e, se funcionario, o id) e quando.
    /// Tabela fora da especificacao original: desvio aprovado pelo Kaio em 13/11/2026
    /// (backend/docs/especificacao/desvios_aprovados.json).
    /// </summary>
    public class SinistroHistorico : BaseEntities
    {
        [Column("id_sinistro_historico")]
        public int IdSinistroHistorico { get; set; }

        [Column("id_sinistro")]
        public int IdSinistro { get; set; }

        /// <summary>Status antes da mudanca; nulo no registro da abertura.</summary>
        [Column("status_anterior")]
        public StatusSinistro? StatusAnterior { get; set; }

        [Column("status_novo")]
        public StatusSinistro StatusNovo { get; set; }

        /// <summary>Quem mudou: FUNCIONARIO (exige IdFuncionario, ck_sinistro_historico_funcionario) ou SEGURADORA.</summary>
        [Column("origem")]
        public OrigemHistoricoSinistro Origem { get; set; }

        [Column("id_funcionario")]
        public int? IdFuncionario { get; set; }

        [Column("observacao")]
        public string? Observacao { get; set; }

        /// <summary>Quando o status mudou (pode ser a data informada pela seguradora).</summary>
        [Column("data_alteracao")]
        public DateTime DataAlteracao { get; set; }

        // --- Navegacao ---
        public virtual Sinistro Sinistro { get; set; } = null!;
        public virtual Funcionario? Funcionario { get; set; }
    }
}
