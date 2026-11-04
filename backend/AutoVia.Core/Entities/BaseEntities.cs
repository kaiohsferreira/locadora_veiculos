using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Core.Entities
{
    /// <summary>
    /// Base de auditoria (created_at, updated_at, disabled_at) com exclusao logica.
    ///
    /// TODAS as entidades herdam desta classe (decisao do Kaio em 04/11/2026); uma
    /// excecao so com justificativa registrada. Regras que acompanham:
    /// - leituras do repositorio filtram DisabledAt == null;
    /// - exclusao e sempre Disable(), nunca Remove();
    /// - unicidade (placa, CPF etc.) e validada no servico sobre os registros ativos,
    ///   sem indice unico no banco.
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
