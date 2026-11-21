using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPixPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pix_copia_e_cola",
                table: "pagamento",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pix_expira_em",
                table: "pagamento",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "qr_code_base64",
                table: "pagamento",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pix_copia_e_cola",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "pix_expira_em",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "qr_code_base64",
                table: "pagamento");
        }
    }
}
