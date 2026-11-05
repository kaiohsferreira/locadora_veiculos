using AutoVia.Core.Entities;
using AutoVia.Communication.Enums;
using AutoVia.Integrations.Helpers;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Integrations.Entities
{
    /// <summary>
    /// Tabela integration: dados de uma integracao externa (codigo, ambiente,
    /// URL base, timeout e se esta ativa). Herda BaseEntities (created_at,
    /// updated_at, disabled_at), como manda a especificacao do Apendice B.
    /// </summary>
    public class Integration : BaseEntities
    {
        [Column("id_integration")]
        public int IdIntegration { get; set; }

        /// <summary>UNIQUE. Ex.: PAGAMENTO, EMAIL. Ver ConstantsIntegration.</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [Column("descricao")]
        public string? Descricao { get; set; }

        /// <summary>DEFAULT 'PRODUCAO'.</summary>
        [Column("ambiente")]
        public AmbienteIntegracao Ambiente { get; set; } = AmbienteIntegracao.PRODUCAO;

        [Column("base_url")]
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>DEFAULT 30000. Usado como timeout de toda chamada da integracao.</summary>
        [Column("timeout_ms")]
        public int TimeoutMs { get; set; } = ConstantsIntegration.TimeoutPadraoMs;

        /// <summary>DEFAULT TRUE.</summary>
        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        // --- Navegacao ---
        public virtual List<IntegrationSetting> Settings { get; set; } = new List<IntegrationSetting>();
    }
}
