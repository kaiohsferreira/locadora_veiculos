using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Aprovacao do cadastro do cliente por locadora (contrato cliente-locadora v1, desvio cliente_locadora de 14/11/2026):
    ///   1. cria a tabela cliente_locadora (CHECK ck_cliente_locadora_status; FKs NO ACTION; indices comuns, nenhum unico);
    ///   2. preenche um vinculo para cada par (cliente ativo, locadora) com reserva, locacao ou venda ativa em veiculo dela,
    ///      com o MESMO status global do cliente, verificador e justificativa nulos.
    /// A coluna cliente.status_cadastro fica na tabela, congelada (nenhuma regra nova a le). Down: remove a tabela.
    /// </summary>
    public partial class AdicionarClienteLocadora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente_locadora",
                columns: table => new
                {
                    id_cliente_locadora = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PENDENTE"),
                    justificativa_recusa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    id_funcionario_verificador = table.Column<int>(type: "integer", nullable: true),
                    data_solicitacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    data_verificacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cliente_locadora_pkey", x => x.id_cliente_locadora);
                    table.CheckConstraint("ck_cliente_locadora_status", "status IN ('PENDENTE', 'APROVADO', 'RECUSADO')");
                    table.ForeignKey(
                        name: "fk_cliente_locadora_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_cliente_locadora_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresa",
                        principalColumn: "id_empresa");
                    table.ForeignKey(
                        name: "fk_cliente_locadora_funcionario",
                        column: x => x.id_funcionario_verificador,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                });

            migrationBuilder.CreateIndex(
                name: "idx_cliente_locadora_cliente",
                table: "cliente_locadora",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "idx_cliente_locadora_empresa_cliente",
                table: "cliente_locadora",
                columns: new[] { "id_empresa", "id_cliente" });

            // Preenchimento (contrato, migration.preenchimento). Colunas conferidas nas entidades e em 01_Inicial.sql:
            // reserva, locacao e venda tem id_cliente e id_veiculo; veiculo tem id_empresa; cliente tem status_cadastro e disabled_at.
            // Quem ja negociou com a locadora (reserva, locacao ou venda ativa) ganha um vinculo com o status global do cliente.
            // So APROVADO e RECUSADO: cliente PENDENTE nao ganha vinculo (RF-83, decisao do QA de 14/11). Ele entra na fila
            // da locadora na primeira tentativa de reserva.
            migrationBuilder.Sql(@"
INSERT INTO cliente_locadora (id_cliente, id_empresa, status)
SELECT c.id_cliente, v.id_empresa, c.status_cadastro
FROM cliente c
JOIN (SELECT id_cliente, id_veiculo FROM reserva WHERE disabled_at IS NULL
      UNION SELECT id_cliente, id_veiculo FROM locacao WHERE disabled_at IS NULL
      UNION SELECT id_cliente, id_veiculo FROM venda WHERE disabled_at IS NULL) n ON n.id_cliente = c.id_cliente
JOIN veiculo v ON v.id_veiculo = n.id_veiculo
WHERE c.disabled_at IS NULL
  AND c.status_cadastro IN ('APROVADO', 'RECUSADO')
GROUP BY c.id_cliente, v.id_empresa, c.status_cadastro;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cliente_locadora");
        }
    }
}
