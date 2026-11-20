using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class VincularAvariasVistoriaSaida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "comparacao_realizada",
                table: "vistoria",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "id_avaria_entrada",
                table: "avaria",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_avaria_entrada",
                table: "avaria",
                column: "id_avaria_entrada");

            migrationBuilder.AddForeignKey(
                name: "fk_avaria_entrada",
                table: "avaria",
                column: "id_avaria_entrada",
                principalTable: "avaria",
                principalColumn: "id_avaria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_avaria_entrada",
                table: "avaria");

            migrationBuilder.DropIndex(
                name: "idx_avaria_entrada",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "comparacao_realizada",
                table: "vistoria");

            migrationBuilder.DropColumn(
                name: "id_avaria_entrada",
                table: "avaria");
        }
    }
}
