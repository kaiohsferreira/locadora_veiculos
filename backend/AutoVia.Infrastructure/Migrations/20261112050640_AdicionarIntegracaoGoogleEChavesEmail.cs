using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) para o login com Google (RF-51) e o e-mail
    /// da recuperacao de senha (RF-49):
    ///   integration GOOGLE: codigo GOOGLE, ambiente SANDBOX, base_url https://www.googleapis.com,
    ///                       timeout_ms 15000, ativo TRUE (criada se nao existir ativa)
    ///   GOOGLE.client_id:   DEFINIR_NO_BANCO (NAO protegida: o Client ID e publico)
    ///   GOOGLE.provedor:    FAKE (troca para GOOGLE quando o Client ID real estiver no banco)
    ///   EMAIL.provedor:     FAKE (provedor de e-mail de teste ate a T-INT-01)
    ///
    /// Mesmo padrao da migration AdicionarChavesCaucao: SQL explicito, sem DDL.
    /// Up e idempotente: so insere o que nao existe como registro ativo.
    ///
    /// Down faz EXCLUSAO LOGICA (disabled_at) somente do que esta migration criou. Nao usa DELETE.
    /// </summary>
    public partial class AdicionarIntegracaoGoogleEChavesEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO integration (codigo, nome, descricao, ambiente, base_url, timeout_ms, ativo)
SELECT 'GOOGLE', 'Login com Google', 'Confirmacao da identidade do cliente pela conta Google (RF-51)', 'SANDBOX', 'https://www.googleapis.com', 15000, TRUE
WHERE NOT EXISTS (SELECT 1 FROM integration x WHERE x.codigo = 'GOOGLE' AND x.disabled_at IS NULL);

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('GOOGLE', 'client_id', 'DEFINIR_NO_BANCO', FALSE),
    ('GOOGLE', 'provedor',  'FAKE',             FALSE),
    ('EMAIL',  'provedor',  'FAKE',             FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Exclusao logica somente do que esta migration criou (rollback de versao).
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND s.disabled_at IS NULL
  AND ((i.codigo = 'GOOGLE' AND s.chave IN ('client_id', 'provedor'))
    OR (i.codigo = 'EMAIL' AND s.chave = 'provedor'));

UPDATE integration
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
WHERE codigo = 'GOOGLE'
  AND disabled_at IS NULL;
");
        }
    }
}
