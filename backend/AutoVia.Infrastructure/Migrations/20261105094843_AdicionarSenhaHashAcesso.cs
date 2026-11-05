using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSenhaHashAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "senha_hash",
                table: "funcionario",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "senha_hash",
                table: "cliente",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "senha_hash",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "senha_hash",
                table: "cliente");
        }
    }
}
