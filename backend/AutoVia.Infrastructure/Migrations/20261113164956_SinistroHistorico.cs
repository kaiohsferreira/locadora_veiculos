using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// P9-INT-01 (RF-43): linha do tempo do sinistro e protocolo da seguradora. Desvios
    /// aprovados pelo Kaio em 13/11/2026 (docs/especificacao/desvios_aprovados.json,
    /// secao sinistro_acompanhamento):
    ///   sinistro.protocolo_seguradora VARCHAR(40) NULL
    ///   tabela sinistro_historico, com CHECK dos cinco status em status_anterior/status_novo,
    ///   CHECK da origem e CHECK que exige id_funcionario quando a origem e FUNCIONARIO.
    ///
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script em docs/sql/09_SinistroHistorico.sql).
    /// Down desfaz o esquema (padrao das migrations de esquema do projeto).
    /// </summary>
    public partial class SinistroHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "protocolo_seguradora",
                table: "sinistro",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sinistro_historico",
                columns: table => new
                {
                    id_sinistro_historico = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_sinistro = table.Column<int>(type: "integer", nullable: false),
                    status_anterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status_novo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    origem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: true),
                    observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    data_alteracao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("sinistro_historico_pkey", x => x.id_sinistro_historico);
                    table.CheckConstraint("ck_sinistro_historico_funcionario", "origem <> 'FUNCIONARIO' OR id_funcionario IS NOT NULL");
                    table.CheckConstraint("ck_sinistro_historico_origem", "origem IN ('FUNCIONARIO', 'SEGURADORA')");
                    table.CheckConstraint("ck_sinistro_historico_status_anterior", "status_anterior IS NULL OR status_anterior IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')");
                    table.CheckConstraint("ck_sinistro_historico_status_novo", "status_novo IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')");
                    table.ForeignKey(
                        name: "fk_sinistro_historico_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_sinistro_historico_sinistro",
                        column: x => x.id_sinistro,
                        principalTable: "sinistro",
                        principalColumn: "id_sinistro");
                });

            migrationBuilder.CreateIndex(
                name: "idx_sinistro_historico_sinistro",
                table: "sinistro_historico",
                columns: new[] { "id_sinistro", "data_alteracao" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sinistro_historico");

            migrationBuilder.DropColumn(
                name: "protocolo_seguradora",
                table: "sinistro");
        }
    }
}
