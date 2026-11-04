using AutoVia.Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoVia.Integrations.Entities
{
    /// <summary>
    /// Tabela integration_setting: campos dinamicos de uma integracao em pares
    /// chave/valor (client_id, client_secret, token...). Protegido = true marca
    /// valor que nunca pode aparecer em log nem em resposta de API.
    ///
    /// Herda BaseEntities com disabled_at mapeado (exclusao logica): leituras
    /// filtram DisabledAt == null e a chave e unica por integracao entre os
    /// settings ativos, regra validada no codigo (sem indice unico no banco).
    /// </summary>
    public class IntegrationSetting : BaseEntities
    {
        [Column("id_setting")]
        public int IdSetting { get; set; }

        [Column("id_integration")]
        public int IdIntegration { get; set; }

        [Column("chave")]
        public string Chave { get; set; } = string.Empty;

        /// <summary>TEXT. Para chave protegida, nunca logar nem devolver em API.</summary>
        [Column("valor")]
        public string Valor { get; set; } = string.Empty;

        /// <summary>DEFAULT FALSE.</summary>
        [Column("protegido")]
        public bool Protegido { get; set; }

        // --- Navegacao ---
        public virtual Integration Integration { get; set; } = null!;
    }
}
