using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) que acrescenta as chaves da caucao no cartao
    /// (P7-INT-01, RF-20) na integracao PAGAMENTO, exigidas pelo contrato integracao-pagamento-caucao:
    ///   public_key (DEFINIR_NO_BANCO, NAO protegida: e chave publica, usada pelo front para gerar o token do cartao)
    ///   caucao_validade_dias (5, opcional: validade estimada da reserva, faixa 1..30)
    ///
    /// Mesmo padrao da migration AjustarIntegracoesProvedores: SQL explicito, sem DDL.
    /// Up e idempotente: so insere a chave quando nao existe um setting ativo com o mesmo nome.
    ///
    /// Down faz EXCLUSAO LOGICA (disabled_at) somente destas duas chaves. Nao usa DELETE.
    /// </summary>
    public partial class AdicionarChavesCaucao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, s.chave, s.valor, s.protegido
FROM integration i
JOIN (VALUES
    ('PAGAMENTO', 'public_key',            'DEFINIR_NO_BANCO', FALSE),
    ('PAGAMENTO', 'caucao_validade_dias',  '5',                FALSE)
) AS s (codigo, chave, valor, protegido) ON s.codigo = i.codigo
WHERE i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = s.chave AND x.disabled_at IS NULL);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Exclusao logica somente das duas chaves desta migration (rollback de versao).
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET disabled_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND s.disabled_at IS NULL
  AND i.codigo = 'PAGAMENTO'
  AND s.chave IN ('public_key', 'caucao_validade_dias');
");
        }
    }
}
