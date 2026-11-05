using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) que acrescenta as chaves de
    /// provedor das integracoes PAGAMENTO e DOCUMENTO, exigidas pelos contratos
    /// integracao-pagamento-pix e integracao-documento:
    ///   PAGAMENTO: provedor (FAKE), access_token (DEFINIR_NO_BANCO, protegido),
    ///              pix_expiracao_segundos (3600)
    ///   DOCUMENTO: provedor (FAKE)
    ///
    /// Mesmo padrao da SeedIntegracoes: SQL explicito, sem DDL. Valores sensiveis
    /// entram como DEFINIR_NO_BANCO; o token real e preenchido direto no banco de
    /// cada ambiente, nunca em migration.
    ///
    /// Up e idempotente: so insere a chave quando nao existe um setting ativo com
    /// o mesmo nome para a integracao.
    ///
    /// Down faz EXCLUSAO LOGICA (disabled_at) das quatro chaves desta migration.
    /// Nao usa DELETE: a regra do projeto proibe exclusao fisica. Como a Up e
    /// idempotente, uma nova aplicacao cria a chave de novo, sem duplicar as ativas.
    /// </summary>
    public partial class AjustarIntegracoesProvedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('PAGAMENTO', 'provedor',               'FAKE',             FALSE),
    ('PAGAMENTO', 'access_token',           'DEFINIR_NO_BANCO', TRUE),
    ('PAGAMENTO', 'pix_expiracao_segundos', '3600',             FALSE),
    ('DOCUMENTO', 'provedor',               'FAKE',             FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Exclusao logica somente das quatro chaves desta migration (rollback de versao).
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND s.disabled_at IS NULL
  AND ((i.codigo = 'PAGAMENTO' AND s.chave IN ('provedor', 'access_token', 'pix_expiracao_segundos'))
    OR (i.codigo = 'DOCUMENTO' AND s.chave = 'provedor'));
");
        }
    }
}
