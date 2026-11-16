using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) das integracoes do Eduardo (P4-INT-01 e P5-INT-01, merge S1 de 16/11/2026):
    ///   MULTAS: provedor (FAKE). O gateway de multas exige a chave (MultasGateway, GetRequired); ela nao existia em banco nenhum.
    ///   ARMAZENAMENTO: provedor (S3_COMPATIVEL). Explicita o default que o codigo ja usa; nao muda o comportamento.
    ///
    /// Mesmo padrao da SeguradoraProvedor: SQL explicito, sem DDL. Nenhuma chave deste lote e protegida. As credenciais
    /// (token da MULTAS, access_key e secret_key do ARMAZENAMENTO) continuam DEFINIR_NO_BANCO e nao sao tocadas aqui.
    ///
    /// Up e idempotente: so insere a chave quando nao existe um setting ativo com o mesmo nome para a integracao.
    ///
    /// Down faz EXCLUSAO LOGICA (disabled_at) das duas chaves desta migration. Nao usa DELETE: a regra do projeto proibe exclusao fisica.
    /// Ressalva conhecida (item L13 da sintese do Eduardo, 16/11/2026): a exclusao logica nao tira a chave de uso, porque o repositorio
    /// de integracoes carrega as configuracoes sem filtrar disabled_at; com duas linhas da mesma chave, o resolvedor fica com a primeira.
    /// Um Down seguido de Up cria nova linha ativa e deixa a desativada.
    ///
    /// Depois de aplicar, a API em execucao mantem a configuracao antiga por ate 5 minutos (cache de IntegrationResolver, item L10):
    /// reinicie a API ou espere.
    ///
    /// Sem mudanca de esquema: nao altera o snapshot nem o 00_ModeloAtual.sql.
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/15_MultasProvedor.sql).
    /// </summary>
    public partial class MultasProvedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('MULTAS',        'provedor', 'FAKE',          FALSE),
    ('ARMAZENAMENTO', 'provedor', 'S3_COMPATIVEL', FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Exclusao logica somente das chaves desta migration (rollback de versao).
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND s.disabled_at IS NULL
  AND ((i.codigo = 'MULTAS' AND s.chave = 'provedor')
    OR (i.codigo = 'ARMAZENAMENTO' AND s.chave = 'provedor'));
");
        }
    }
}
