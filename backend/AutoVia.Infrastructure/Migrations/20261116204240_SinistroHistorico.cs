using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// P9-INT-01 (RF-43): linha do tempo do sinistro e protocolo da seguradora. Desvio "sinistro_acompanhamento"
    /// aprovado pelo Kaio em 16/11/2026 (o registro original foi escrito pela Maria em 13/11/2026, na branch dela):
    ///   1. cria sinistro.protocolo_seguradora VARCHAR(40) NULL;
    ///   2. cria a tabela sinistro_historico (11 colunas; CHECK ck_sinistro_historico_funcionario, ck_sinistro_historico_origem,
    ///      ck_sinistro_historico_status_anterior e ck_sinistro_historico_status_novo; FKs NO ACTION para funcionario e sinistro);
    ///   3. cria o indice comum idx_sinistro_historico_sinistro (id_sinistro, data_alteracao). Nenhum indice unico.
    /// Esta migration so muda o esquema. As migrations 14 (SeguradoraProvedor) e 15 (MultasProvedor) sao de dados, sem DDL.
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/13_SinistroHistorico.sql).
    /// Down desfaz o esquema e apaga a linha do tempo dos sinistros: uso so em desenvolvimento.
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
