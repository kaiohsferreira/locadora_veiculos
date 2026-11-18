using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Migration de DADOS (sem mudanca de esquema) da integracao ARMAZENAMENTO (P4-INT-01; contrato armazenamento-disco v1, tarefa D4):
    ///   provedor: S3_COMPATIVEL -> DISCO_LOCAL (arquivo em pasta da maquina; leitura publica por link assinado).
    /// Altera a linha da chave 'provedor' que a migration 15 (20261116204428_MultasProvedor) criou. NUNCA cria segunda linha: o INSERT so age
    /// quando NAO existe nenhuma linha da chave (banco em que a 15 nao criou a linha).
    /// Up e idempotente: aplicar duas vezes da o mesmo resultado.
    /// Down volta a linha para S3_COMPATIVEL. Nao faz DELETE nem exclusao logica (disabled_at): so troca o valor.
    /// O Up nao filtra disabled_at de proposito: o repositorio de integracoes carrega as configuracoes sem esse filtro e o resolvedor
    /// fica com a primeira linha de cada chave; se existir linha desativada da mesma chave, ela tambem vira DISCO_LOCAL.
    ///
    /// Ordem de aplicacao: PRIMEIRO a DP-01 decidida. DEPOIS o codigo novo nos lugares que usam o banco (PC atualizado; Railway publicado,
    /// com o Volume e as duas variaveis; colegas avisados). SO ENTAO aplicar esta migration.
    /// AVISO: TODA maquina que usa este banco muda de provedor ao aplicar. Codigo antigo apontando para este banco reprova com EA-05 (502)
    /// em toda operacao de armazenamento, ate a maquina atualizar o codigo. Avisar a equipe ANTES de aplicar.
    /// Cache de 5 minutos do IntegrationResolver: depois de aplicar, cada API em execucao mantem o provedor antigo por ate 5 minutos;
    /// reinicie a API ou espere.
    /// Cada maquina atualizada passa a gravar na PROPRIA pasta: as fotos de uma maquina nao abrem na outra nem no site.
    /// Down com fotos ja gravadas: voltar a S3_COMPATIVEL nao apaga arquivo nem linha; as linhas de veiculo_foto e de avaria gravadas no
    /// periodo passam a apontar para chaves que so existem na pasta (com o S3 sem bucket, envio volta a 502 e as capas a erroFotoCapa).
    ///
    /// Conferencia (quem roda e o Kaio, no cliente SQL dele; nenhum agente consulta o banco):
    ///   ANTES:  SELECT s.id_setting, s.valor, s.disabled_at FROM integration_setting s JOIN integration i ON i.id_integration = s.id_integration WHERE i.codigo = 'ARMAZENAMENTO' AND s.chave = 'provedor';
    ///           esperado: uma linha com S3_COMPATIVEL (criada pela migration 15). Mais de uma, ou nenhuma: avisar antes de aplicar.
    ///   DEPOIS: a mesma consulta; todas as linhas da chave com DISCO_LOCAL.
    ///
    /// Sem mudanca de esquema: nao altera o snapshot nem o 00_ModeloAtual.sql.
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/18_ArmazenamentoDiscoLocal.sql).
    /// </summary>
    public partial class ArmazenamentoDiscoLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET valor = 'DISCO_LOCAL',
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND i.codigo = 'ARMAZENAMENTO'
  AND s.chave = 'provedor'
  AND s.valor <> 'DISCO_LOCAL';

INSERT INTO integration_setting (id_integration, chave, valor, protegido)
SELECT i.id_integration, 'provedor', 'DISCO_LOCAL', FALSE
FROM integration i
WHERE i.codigo = 'ARMAZENAMENTO'
  AND i.disabled_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM integration_setting x
                  WHERE x.id_integration = i.id_integration AND x.chave = 'provedor');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Volta o valor da mesma linha para S3_COMPATIVEL. Sem DELETE e sem disabled_at.
            migrationBuilder.Sql(@"
UPDATE integration_setting s
SET valor = 'S3_COMPATIVEL',
    updated_at = CURRENT_TIMESTAMP
FROM integration i
WHERE i.id_integration = s.id_integration
  AND i.codigo = 'ARMAZENAMENTO'
  AND s.chave = 'provedor'
  AND s.valor = 'DISCO_LOCAL';
");
        }
    }
}
