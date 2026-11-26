using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarContratoLocacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contrato_locacao",
                columns: table => new
                {
                    id_contrato_locacao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    documento_json = table.Column<string>(type: "text", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nome_assinante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    aceite_condicoes_gerais = table.Column<bool>(type: "boolean", nullable: false),
                    aceite_politica_combustivel = table.Column<bool>(type: "boolean", nullable: false),
                    assinatura_png_base64 = table.Column<string>(type: "text", nullable: false),
                    assinado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contrato_locacao_pkey", x => x.id_contrato_locacao);
                    table.ForeignKey(
                        name: "fk_contrato_locacao_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_contrato_locacao_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao");
                });

            migrationBuilder.CreateIndex(
                name: "idx_contrato_locacao_locacao",
                table: "contrato_locacao",
                column: "id_locacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contrato_locacao");
        }
    }
}
