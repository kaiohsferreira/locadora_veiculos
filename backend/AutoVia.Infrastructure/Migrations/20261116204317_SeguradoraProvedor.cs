using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) da P9-INT-01: acrescenta a chave de provedor da integracao SEGURADORA,
    /// exigida pelo contrato integracao-seguradora:
    ///   SEGURADORA: provedor (FAKE)
    ///
    /// Mesmo padrao da AjustarIntegracoesProvedores: SQL explicito, sem DDL. As credenciais reais (usuario, senha,
    /// codigo_corretora, apolice_master) continuam DEFINIR_NO_BANCO.
    ///
    /// Up e idempotente: so insere a chave quando nao existe um setting ativo com o mesmo nome para a integracao.
    ///
    /// Down faz EXCLUSAO LOGICA (disabled_at) da chave desta migration. Nao usa DELETE: a regra do projeto proibe exclusao fisica.
    /// Ressalva conhecida (pendencia da fundacao de integracoes, item L13 da sintese do Eduardo, 16/11/2026): a exclusao logica
    /// nao tira a chave de uso, porque o repositorio de integracoes carrega as configuracoes sem filtrar disabled_at; com duas
    /// linhas da mesma chave, o resolvedor fica com a primeira. Um Down seguido de Up cria nova linha ativa e deixa a desativada.
    ///
    /// Sem mudanca de esquema: nao altera o snapshot nem o 00_ModeloAtual.sql.
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/14_SeguradoraProvedor.sql).
    /// </summary>
    public partial class SeguradoraProvedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('SEGURADORA', 'provedor', 'FAKE', FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Exclusao logica somente da chave desta migration (rollback de versao).
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND s.disabled_at IS NULL
  AND i.codigo = 'SEGURADORA' AND s.chave = 'provedor';
");
        }
    }
}
