using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Multi-locadora (T-BE-03a, desenho do arquiteto de 13/11/2026, secao 2.1 e desvio "empresa_locadora"):
    ///   1. cria a tabela empresa (CHECK ck_empresa_cnpj, indices comuns idx_empresa_cnpj e idx_empresa_email);
    ///   2. troca o CHECK ck_funcionario_cargo para aceitar ADMINISTRADOR (seis valores);
    ///   3. cria funcionario.id_empresa e veiculo.id_empresa NULAS;
    ///   4. cria a empresa inicial (operadora da plataforma), se ainda nao existir;
    ///   5. aponta todo funcionario e todo veiculo existentes (inclusive desativados) para a empresa inicial;
    ///   6. promove o administrador inicial de ADMINISTRATIVO para ADMINISTRADOR, somente quando existe
    ///      EXATAMENTE um candidato (tabela vazia nao faz nada; 0 ou mais de 1 candidato interrompe a migration
    ///      com RAISE EXCEPTION, sem promover ninguem);
    ///   7. torna id_empresa NOT NULL nas duas tabelas;
    ///   8. cria as FKs fk_funcionario_empresa e fk_veiculo_empresa (NO ACTION) e os indices comuns.
    ///
    /// NAO ha DEFAULT em id_empresa: uma INSERT de branch antiga sem empresa falha alto (NOT NULL).
    /// Nao ha indice unico: a unicidade de CNPJ e e-mail da empresa, matricula e CPF e validada no servico.
    ///
    /// ATENCAO: os dados da empresa inicial abaixo sao PROVISORIOS. O Kaio ainda vai informar os reais.
    /// Trocar as constantes ANTES de aplicar esta migration no banco.
    ///
    /// Down: volta ADMINISTRADOR para ADMINISTRATIVO antes de restaurar o CHECK antigo, depois remove
    /// FKs, indices, colunas e a tabela empresa (e, com ela, a empresa inicial que a tabela contem).
    /// </summary>
    public partial class AdicionarEmpresaLocadora : Migration
    {
        // --- Dados PROVISORIOS da empresa inicial (trocar antes de aplicar) ---
        private const string RazaoSocialProvisoria = "AutoVia (empresa inicial)";
        private const string NomeFantasiaProvisorio = "AutoVia";
        private const string CnpjProvisorio = "00000000000000";
        private const string TelefoneProvisorio = "a definir";
        private const string EmailProvisorio = "a definir";
        private const string CepProvisorio = "00000000";
        private const string LogradouroProvisorio = "a definir";
        private const string NumeroProvisorio = "S/N";
        private const string BairroProvisorio = "a definir";
        private const string CidadeProvisoria = "a definir";
        private const string UfProvisoria = "XX";

        // --- Passo 6: criterio unico do administrador inicial (usado na contagem, na promocao e na mensagem) ---
        private const string FiltroAdministradorInicial = "cargo = 'ADMINISTRATIVO' AND ativo = TRUE AND disabled_at IS NULL AND email IS NULL";

        // Mensagem do RAISE EXCEPTION: o unico % recebe o total de candidatos. Aspas dobradas por EscaparSql no uso.
        private const string MensagemPromocaoAdministradorAmbigua = "Migration AdicionarEmpresaLocadora, passo 6: encontrados % candidatos a administrador inicial (esperado: exatamente 1). Rode SELECT id_funcionario, nome, cpf, matricula, cargo, ativo, email FROM funcionario WHERE " + FiltroAdministradorInicial + "; para conferir quem e. Corrija os dados e aplique a migration de novo.";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Tabela empresa
            migrationBuilder.CreateTable(
                name: "empresa",
                columns: table => new
                {
                    id_empresa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    razao_social = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cep = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    logradouro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bairro = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    cidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    operadora = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("empresa_pkey", x => x.id_empresa);
                    table.CheckConstraint("ck_empresa_cnpj", "cnpj ~ '^[0-9]{14}$'");
                });

            migrationBuilder.CreateIndex(
                name: "idx_empresa_cnpj",
                table: "empresa",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "idx_empresa_email",
                table: "empresa",
                column: "email");

            // 2. Troca do CHECK de cargo: ADMINISTRADOR entra antes de qualquer UPDATE de cargo
            migrationBuilder.DropCheckConstraint(
                name: "ck_funcionario_cargo",
                table: "funcionario");

            migrationBuilder.AddCheckConstraint(
                name: "ck_funcionario_cargo",
                table: "funcionario",
                sql: "cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', 'ADMINISTRADOR')");

            // 3. Colunas id_empresa, nulas por enquanto (sem DEFAULT)
            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "funcionario",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id_empresa",
                table: "veiculo",
                type: "integer",
                nullable: true);

            // 4. Empresa inicial (operadora). Criada so se nao houver uma ativa com o mesmo CNPJ.
            migrationBuilder.Sql($@"
INSERT INTO empresa (razao_social, nome_fantasia, cnpj, telefone, email, cep, logradouro, numero, complemento, bairro, cidade, uf, ativo, operadora)
SELECT '{EscaparSql(RazaoSocialProvisoria)}', '{EscaparSql(NomeFantasiaProvisorio)}', '{EscaparSql(CnpjProvisorio)}', '{EscaparSql(TelefoneProvisorio)}', '{EscaparSql(EmailProvisorio)}', '{EscaparSql(CepProvisorio)}', '{EscaparSql(LogradouroProvisorio)}', '{EscaparSql(NumeroProvisorio)}', NULL, '{EscaparSql(BairroProvisorio)}', '{EscaparSql(CidadeProvisoria)}', '{EscaparSql(UfProvisoria)}', TRUE, TRUE
WHERE NOT EXISTS (SELECT 1 FROM empresa x WHERE x.cnpj = '{EscaparSql(CnpjProvisorio)}' AND x.disabled_at IS NULL);
");

            // 5. Todo funcionario e todo veiculo existentes (inclusive desativados) vao para a empresa inicial
            migrationBuilder.Sql($@"
UPDATE funcionario
SET id_empresa = (SELECT MIN(e.id_empresa) FROM empresa e WHERE e.cnpj = '{EscaparSql(CnpjProvisorio)}' AND e.disabled_at IS NULL)
WHERE id_empresa IS NULL;

UPDATE veiculo
SET id_empresa = (SELECT MIN(e.id_empresa) FROM empresa e WHERE e.cnpj = '{EscaparSql(CnpjProvisorio)}' AND e.disabled_at IS NULL)
WHERE id_empresa IS NULL;
");

            // 6. Administrador inicial vira ADMINISTRADOR (depois do CHECK novo do passo 2).
            // Criterio: ADMINISTRATIVO, ativo, sem e-mail e nao desativado (o que o AdminInicialService cria).
            // Tabela vazia: nada a fazer. Exatamente 1 candidato: promove. Qualquer outro numero: RAISE EXCEPTION,
            // que aborta a migration (ela roda numa transacao so) sem promover ninguem.
            migrationBuilder.Sql($@"
DO $$
DECLARE
    total_funcionarios INTEGER;
    total_candidatos INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_funcionarios FROM funcionario;

    IF total_funcionarios > 0 THEN
        SELECT COUNT(*) INTO total_candidatos FROM funcionario WHERE {FiltroAdministradorInicial};

        IF total_candidatos = 1 THEN
            UPDATE funcionario SET cargo = 'ADMINISTRADOR' WHERE {FiltroAdministradorInicial};
        ELSE
            RAISE EXCEPTION '{EscaparSql(MensagemPromocaoAdministradorAmbigua)}', total_candidatos;
        END IF;
    END IF;
END
$$;
");

            // 7. Obrigatoriedade, depois que todas as linhas receberam empresa
            migrationBuilder.AlterColumn<int>(
                name: "id_empresa",
                table: "funcionario",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_empresa",
                table: "veiculo",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // 8. Chaves estrangeiras (NO ACTION) e indices comuns
            migrationBuilder.AddForeignKey(
                name: "fk_funcionario_empresa",
                table: "funcionario",
                column: "id_empresa",
                principalTable: "empresa",
                principalColumn: "id_empresa");

            migrationBuilder.AddForeignKey(
                name: "fk_veiculo_empresa",
                table: "veiculo",
                column: "id_empresa",
                principalTable: "empresa",
                principalColumn: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "idx_funcionario_empresa",
                table: "funcionario",
                column: "id_empresa");

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_empresa",
                table: "veiculo",
                column: "id_empresa");
        }

        /// <summary>
        /// Escapa um valor para literal SQL entre aspas simples (dobra o apostrofo). Todo valor de constante que entra
        /// no SQL por interpolacao passa por aqui, para que uma razao social ou uma mensagem com apostrofo nao quebre o script.
        /// </summary>
        private static string EscaparSql(string valor)
        {
            string escapado = valor.Replace("'", "''");

            return escapado;
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Antes de restaurar o CHECK antigo, ADMINISTRADOR volta a ADMINISTRATIVO (o CHECK antigo recusa ADMINISTRADOR).
            migrationBuilder.Sql(@"
UPDATE funcionario
SET cargo = 'ADMINISTRATIVO'
WHERE cargo = 'ADMINISTRADOR';
");

            migrationBuilder.DropCheckConstraint(
                name: "ck_funcionario_cargo",
                table: "funcionario");

            migrationBuilder.AddCheckConstraint(
                name: "ck_funcionario_cargo",
                table: "funcionario",
                sql: "cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO')");

            migrationBuilder.DropForeignKey(
                name: "fk_funcionario_empresa",
                table: "funcionario");

            migrationBuilder.DropForeignKey(
                name: "fk_veiculo_empresa",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "idx_funcionario_empresa",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "idx_veiculo_empresa",
                table: "veiculo");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "id_empresa",
                table: "veiculo");

            migrationBuilder.DropTable(
                name: "empresa");
        }
    }
}
