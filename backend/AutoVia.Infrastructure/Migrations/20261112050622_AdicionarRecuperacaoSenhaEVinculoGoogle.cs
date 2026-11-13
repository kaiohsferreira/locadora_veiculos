using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarRecuperacaoSenhaEVinculoGoogle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "google_sub",
                table: "cliente",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "redefinicao_senha",
                columns: table => new
                {
                    id_redefinicao_senha = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: true),
                    id_funcionario = table.Column<int>(type: "integer", nullable: true),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    expira_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("redefinicao_senha_pkey", x => x.id_redefinicao_senha);
                    table.CheckConstraint("ck_redefinicao_titular", "(id_cliente IS NOT NULL AND id_funcionario IS NULL) OR (id_cliente IS NULL AND id_funcionario IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_redefinicao_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_redefinicao_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                });

            migrationBuilder.CreateIndex(
                name: "idx_cliente_google_sub",
                table: "cliente",
                column: "google_sub");

            migrationBuilder.CreateIndex(
                name: "idx_redefinicao_token",
                table: "redefinicao_senha",
                column: "token_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "redefinicao_senha");

            migrationBuilder.DropIndex(
                name: "idx_cliente_google_sub",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "google_sub",
                table: "cliente");
        }
    }
}
