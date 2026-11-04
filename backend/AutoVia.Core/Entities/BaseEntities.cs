using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities
{
    /// <summary>
    /// Base de auditoria (created_at, updated_at, disabled_at).
    ///
    /// ATENCAO: aplicada SOMENTE as entidades da biblioteca de integracoes
    /// (Integration e IntegrationSetting). As 14 tabelas de negocio da
    /// especificacao nao possuem essas colunas e nao devem herdar desta classe.
    ///
    /// As colunas sao TIMESTAMP (sem fuso), entao os valores usam DateTime.Now.
    /// O Npgsql recusa DateTime com Kind=Utc em coluna "timestamp without time zone".
    /// </summary>
    public abstract class BaseEntities
    {
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("disabled_at")]
        public DateTime? DisabledAt { get; set; }

        /// <summary>
        /// Exclusao logica: marca o registro como desativado. Nunca remover a
        /// linha fisicamente.
        /// </summary>
        public void Disable()
        {
            DateTime now = DateTime.Now;
            DisabledAt = now;
            UpdatedAt = now;
        }
    }
}
