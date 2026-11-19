using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarClienteDocumento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente_documento",
                columns: table => new
                {
                    id_cliente_documento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    chave_objeto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nome_arquivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo_conteudo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tamanho_bytes = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cliente_documento_pkey", x => x.id_cliente_documento);
                    table.CheckConstraint("ck_cliente_documento_tipo", "tipo IN ('CNH', 'COMPROVANTE_RESIDENCIA')");
                    table.ForeignKey(
                        name: "fk_cliente_documento_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                });

            migrationBuilder.CreateIndex(
                name: "idx_cliente_documento_cliente_tipo",
                table: "cliente_documento",
                columns: new[] { "id_cliente", "tipo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cliente_documento");
        }
    }
}
