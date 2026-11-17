using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <summary>
    /// Fornecedor e numero da nota fiscal de compra do veiculo, e modelo com 60 caracteres (pedido do Kaio em 16/11/2026,
    /// tela 05 do cadastro de veiculo). Desvio aprovado pelo Kaio em 16/11/2026 (backend/docs/especificacao/desvios_aprovados.json):
    /// a especificacao traz modelo VARCHAR(40) e nao tem as duas colunas.
    ///   1. altera veiculo.modelo de VARCHAR(40) para VARCHAR(60) (alargamento; os dados existentes continuam validos);
    ///   2. adiciona veiculo.fornecedor VARCHAR(100) NULL e veiculo.numero_nota_fiscal_compra VARCHAR(20) NULL, sem indice.
    /// GERADA, NAO APLICADA: quem aplica e o Kaio (script docs/sql/17_AdicionarFornecedorNotaFiscalCompraVeiculo.sql).
    /// Down volta o modelo para 40 e remove as duas colunas (com os valores gravados): uso so em desenvolvimento.
    /// </summary>
    public partial class AdicionarFornecedorNotaFiscalCompraVeiculo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                table: "veiculo",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.AddColumn<string>(
                name: "fornecedor",
                table: "veiculo",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_nota_fiscal_compra",
                table: "veiculo",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fornecedor",
                table: "veiculo");

            migrationBuilder.DropColumn(
                name: "numero_nota_fiscal_compra",
                table: "veiculo");

            migrationBuilder.AlterColumn<string>(
                name: "modelo",
                table: "veiculo",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);
        }
    }
}
