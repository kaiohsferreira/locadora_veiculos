using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIdTransacaoPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "id_transacao",
                table: "pagamento",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id_transacao",
                table: "pagamento");
        }
    }
}
