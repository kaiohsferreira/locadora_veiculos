using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CadastrarRegrasEApurarCobrancasExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id_apuracao",
                table: "cobranca_extra",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "configuracao_cobranca_extra",
                columns: table => new
                {
                    id_configuracao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_empresa = table.Column<int>(type: "integer", nullable: false),
                    id_categoria = table.Column<int>(type: "integer", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    base_franquia = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    franquia_km = table.Column<int>(type: "integer", nullable: false),
                    valor_km_excedente = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_tanque_completo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tolerancia_atraso_minutos = table.Column<int>(type: "integer", nullable: false),
                    unidade_atraso = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    fracionamento_atraso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor_unidade_atraso = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    base_avarias = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("configuracao_cobranca_extra_pkey", x => x.id_configuracao);
                    table.CheckConstraint("ck_configuracao_extra_atraso", "tolerancia_atraso_minutos >= 0 AND unidade_atraso IN ('HORA', 'DIARIA') AND fracionamento_atraso IN ('PROPORCIONAL', 'INICIADA')");
                    table.CheckConstraint("ck_configuracao_extra_avarias", "base_avarias = 'ESTIMATIVA_VISTORIA'");
                    table.CheckConstraint("ck_configuracao_extra_franquia", "franquia_km >= 0 AND base_franquia IN ('TOTAL', 'POR_DIARIA')");
                    table.CheckConstraint("ck_configuracao_extra_tarifas", "valor_km_excedente > 0 AND valor_tanque_completo > 0 AND valor_unidade_atraso > 0");
                    table.CheckConstraint("ck_configuracao_extra_versao", "versao > 0");
                    table.ForeignKey(
                        name: "fk_configuracao_extra_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categoria",
                        principalColumn: "id_categoria");
                    table.ForeignKey(
                        name: "fk_configuracao_extra_empresa",
                        column: x => x.id_empresa,
                        principalTable: "empresa",
                        principalColumn: "id_empresa");
                });

            migrationBuilder.CreateTable(
                name: "apuracao_cobranca_extra",
                columns: table => new
                {
                    id_apuracao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    id_configuracao = table.Column<int>(type: "integer", nullable: false),
                    versao_configuracao = table.Column<int>(type: "integer", nullable: false),
                    id_vistoria_entrada = table.Column<int>(type: "integer", nullable: false),
                    id_vistoria_saida = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    versao_snapshot = table.Column<int>(type: "integer", nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("apuracao_cobranca_extra_pkey", x => x.id_apuracao);
                    table.CheckConstraint("ck_apuracao_extra_total", "total >= 0");
                    table.CheckConstraint("ck_apuracao_extra_versoes", "versao_configuracao > 0 AND versao_snapshot > 0");
                    table.ForeignKey(
                        name: "fk_apuracao_extra_configuracao",
                        column: x => x.id_configuracao,
                        principalTable: "configuracao_cobranca_extra",
                        principalColumn: "id_configuracao");
                    table.ForeignKey(
                        name: "fk_apuracao_extra_entrada",
                        column: x => x.id_vistoria_entrada,
                        principalTable: "vistoria",
                        principalColumn: "id_vistoria");
                    table.ForeignKey(
                        name: "fk_apuracao_extra_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_apuracao_extra_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao");
                    table.ForeignKey(
                        name: "fk_apuracao_extra_saida",
                        column: x => x.id_vistoria_saida,
                        principalTable: "vistoria",
                        principalColumn: "id_vistoria");
                });

            migrationBuilder.CreateIndex(
                name: "idx_cobranca_apuracao",
                table: "cobranca_extra",
                column: "id_apuracao");

            migrationBuilder.CreateIndex(
                name: "idx_apuracao_extra_locacao",
                table: "apuracao_cobranca_extra",
                column: "id_locacao");

            migrationBuilder.CreateIndex(
                name: "idx_configuracao_extra_empresa_categoria",
                table: "configuracao_cobranca_extra",
                columns: new[] { "id_empresa", "id_categoria" });

            migrationBuilder.AddForeignKey(
                name: "fk_cobranca_apuracao",
                table: "cobranca_extra",
                column: "id_apuracao",
                principalTable: "apuracao_cobranca_extra",
                principalColumn: "id_apuracao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cobranca_apuracao",
                table: "cobranca_extra");

            migrationBuilder.DropTable(
                name: "apuracao_cobranca_extra");

            migrationBuilder.DropTable(
                name: "configuracao_cobranca_extra");

            migrationBuilder.DropIndex(
                name: "idx_cobranca_apuracao",
                table: "cobranca_extra");

            migrationBuilder.DropColumn(
                name: "id_apuracao",
                table: "cobranca_extra");
        }
    }
}
