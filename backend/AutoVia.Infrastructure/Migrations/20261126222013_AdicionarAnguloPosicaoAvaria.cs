using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAnguloPosicaoAvaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "angulo",
                table: "avaria",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pos_x",
                table: "avaria",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "pos_y",
                table: "avaria",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_avaria_angulo",
                table: "avaria",
                sql: "angulo IS NULL OR angulo IN ('FRENTE', 'TRASEIRA', 'LATERAL_ESQUERDA', 'LATERAL_DIREITA')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_avaria_pos_x",
                table: "avaria",
                sql: "pos_x IS NULL OR (pos_x >= 0 AND pos_x <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_avaria_pos_y",
                table: "avaria",
                sql: "pos_y IS NULL OR (pos_y >= 0 AND pos_y <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_avaria_angulo",
                table: "avaria");

            migrationBuilder.DropCheckConstraint(
                name: "ck_avaria_pos_x",
                table: "avaria");

            migrationBuilder.DropCheckConstraint(
                name: "ck_avaria_pos_y",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "angulo",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "pos_x",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "pos_y",
                table: "avaria");
        }
    }
}
