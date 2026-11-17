using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Fotos do veiculo (RF-94 a RF-101 e RF-105; pedido do Kaio em 16/11/2026, ate 15 fotos por veiculo). Desvio "veiculo_foto"
    /// aprovado pelo Kaio em 16/11/2026 (backend/docs/especificacao/desvios_aprovados.json): a especificacao nao tem tabela para isso.
    ///   1. cria a tabela veiculo_foto (10 colunas: 7 de negocio e 3 de auditoria do BaseEntities); PK veiculo_foto_pkey;
    ///      CHECK ck_veiculo_foto_ordem (ordem maior ou igual a 1); FKs NO ACTION para veiculo, funcionario que enviou e funcionario que removeu;
    ///   2. cria o indice comum idx_veiculo_foto_veiculo (id_veiculo, ordem). Nenhum indice unico; o teto de 15 fotos ativas fica no servico.
    /// Esta migration so muda o esquema e a tabela nasce vazia (sem migracao de dados).
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/16_AdicionarVeiculoFoto.sql).
    /// Down remove a tabela e as fotos gravadas: uso so em desenvolvimento.
    /// </summary>
    public partial class AdicionarVeiculoFoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "veiculo_foto",
                columns: table => new
                {
                    id_veiculo_foto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    chave_objeto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    chave_miniatura = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    id_funcionario_envio = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario_remocao = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("veiculo_foto_pkey", x => x.id_veiculo_foto);
                    table.CheckConstraint("ck_veiculo_foto_ordem", "ordem >= 1");
                    table.ForeignKey(
                        name: "fk_veiculo_foto_funcionario_envio",
                        column: x => x.id_funcionario_envio,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_veiculo_foto_funcionario_remocao",
                        column: x => x.id_funcionario_remocao,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_veiculo_foto_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_foto_veiculo",
                table: "veiculo_foto",
                columns: new[] { "id_veiculo", "ordem" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "veiculo_foto");
        }
    }
}
