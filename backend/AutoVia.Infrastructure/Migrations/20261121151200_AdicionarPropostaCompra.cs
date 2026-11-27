using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPropostaCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proposta_compra",
                columns: table => new
                {
                    id_proposta_compra = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    valor_venda = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_entrada = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    numero_parcelas = table.Column<int>(type: "integer", nullable: false),
                    valor_parcela = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    valor_ultima_parcela = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ENVIADA"),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("proposta_compra_pkey", x => x.id_proposta_compra);
                    table.CheckConstraint("ck_proposta_compra_entrada", "valor_entrada >= 0 AND valor_entrada < valor_venda");
                    table.CheckConstraint("ck_proposta_compra_parcela", "valor_parcela > 0 AND valor_ultima_parcela >= valor_parcela");
                    table.CheckConstraint("ck_proposta_compra_parcelas", "numero_parcelas BETWEEN 1 AND 60");
                    table.CheckConstraint("ck_proposta_compra_status", "status IN ('ENVIADA', 'ACEITA', 'RECUSADA', 'CANCELADA')");
                    table.CheckConstraint("ck_proposta_compra_valor", "valor_venda > 0");
                    table.ForeignKey(
                        name: "fk_proposta_compra_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_proposta_compra_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateIndex(
                name: "idx_proposta_compra_cliente",
                table: "proposta_compra",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "idx_proposta_compra_veiculo",
                table: "proposta_compra",
                column: "id_veiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proposta_compra");
        }
    }
}
