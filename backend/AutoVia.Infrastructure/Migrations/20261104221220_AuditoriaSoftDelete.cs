using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_vistoria_locacao_tipo",
                table: "vistoria");

            migrationBuilder.DropIndex(
                name: "venda_id_veiculo_key",
                table: "venda");

            migrationBuilder.DropIndex(
                name: "venda_numero_nota_fiscal_key",
                table: "venda");

            migrationBuilder.DropIndex(
                name: "veiculo_chassi_key",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "veiculo_placa_key",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "veiculo_renavam_key",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "seguro_numero_apolice_key",
                table: "seguro");

            migrationBuilder.DropIndex(
                name: "locacao_id_reserva_key",
                table: "locacao");

            migrationBuilder.DropIndex(
                name: "uq_setting_chave",
                table: "integration_setting");

            migrationBuilder.DropIndex(
                name: "integration_codigo_key",
                table: "integration");

            migrationBuilder.DropIndex(
                name: "funcionario_cpf_key",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "funcionario_matricula_key",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "cliente_cnh_numero_key",
                table: "cliente");

            migrationBuilder.DropIndex(
                name: "cliente_cpf_key",
                table: "cliente");

            migrationBuilder.DropIndex(
                name: "categoria_nome_key",
                table: "categoria");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "vistoria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "vistoria",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "vistoria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "venda",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "venda",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "venda",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "veiculo",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "veiculo",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "veiculo",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "sinistro",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "sinistro",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "sinistro",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "seguro",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "seguro",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "seguro",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "reserva",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "reserva",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "reserva",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "pagamento",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "pagamento",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "pagamento",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "manutencao",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "manutencao",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "manutencao",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "locacao",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "locacao",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "locacao",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "integration_setting",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "funcionario",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "funcionario",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "funcionario",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "cobranca_extra",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "cobranca_extra",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "cobranca_extra",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "cliente",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "cliente",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "cliente",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "categoria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "categoria",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "categoria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "avaria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "avaria",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "avaria",
                type: "timestamp without time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "idx_vistoria_locacao_tipo",
                table: "vistoria",
                columns: new[] { "id_locacao", "tipo" });

            migrationBuilder.CreateIndex(
                name: "idx_venda_numero_nota_fiscal",
                table: "venda",
                column: "numero_nota_fiscal");

            migrationBuilder.CreateIndex(
                name: "idx_venda_veiculo",
                table: "venda",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_chassi",
                table: "veiculo",
                column: "chassi");

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_placa",
                table: "veiculo",
                column: "placa");

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_renavam",
                table: "veiculo",
                column: "renavam");

            migrationBuilder.CreateIndex(
                name: "idx_seguro_numero_apolice",
                table: "seguro",
                column: "numero_apolice");

            migrationBuilder.CreateIndex(
                name: "idx_locacao_reserva",
                table: "locacao",
                column: "id_reserva");

            migrationBuilder.CreateIndex(
                name: "idx_setting_chave",
                table: "integration_setting",
                columns: new[] { "id_integration", "chave" });

            migrationBuilder.CreateIndex(
                name: "idx_integration_codigo",
                table: "integration",
                column: "codigo");

            migrationBuilder.CreateIndex(
                name: "idx_funcionario_cpf",
                table: "funcionario",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "idx_funcionario_matricula",
                table: "funcionario",
                column: "matricula");

            migrationBuilder.CreateIndex(
                name: "idx_cliente_cnh_numero",
                table: "cliente",
                column: "cnh_numero");

            migrationBuilder.CreateIndex(
                name: "idx_cliente_cpf",
                table: "cliente",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "idx_categoria_nome",
                table: "categoria",
                column: "nome");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_vistoria_locacao_tipo",
                table: "vistoria");

            migrationBuilder.DropIndex(
                name: "idx_venda_numero_nota_fiscal",
                table: "venda");

            migrationBuilder.DropIndex(
                name: "idx_venda_veiculo",
                table: "venda");

            migrationBuilder.DropIndex(
                name: "idx_veiculo_chassi",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "idx_veiculo_placa",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "idx_veiculo_renavam",
                table: "veiculo");

            migrationBuilder.DropIndex(
                name: "idx_seguro_numero_apolice",
                table: "seguro");

            migrationBuilder.DropIndex(
                name: "idx_locacao_reserva",
                table: "locacao");

            migrationBuilder.DropIndex(
                name: "idx_setting_chave",
                table: "integration_setting");

            migrationBuilder.DropIndex(
                name: "idx_integration_codigo",
                table: "integration");

            migrationBuilder.DropIndex(
                name: "idx_funcionario_cpf",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "idx_funcionario_matricula",
                table: "funcionario");

            migrationBuilder.DropIndex(
                name: "idx_cliente_cnh_numero",
                table: "cliente");

            migrationBuilder.DropIndex(
                name: "idx_cliente_cpf",
                table: "cliente");

            migrationBuilder.DropIndex(
                name: "idx_categoria_nome",
                table: "categoria");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "vistoria");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "vistoria");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "vistoria");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "venda");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "venda");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "venda");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "veiculo");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "veiculo");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "veiculo");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "sinistro");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "sinistro");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "sinistro");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "seguro");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "seguro");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "seguro");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "reserva");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "reserva");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "reserva");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "pagamento");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "manutencao");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "manutencao");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "manutencao");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "locacao");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "locacao");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "locacao");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "integration_setting");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "funcionario");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "cobranca_extra");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "cobranca_extra");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "cobranca_extra");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "cliente");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "categoria");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "categoria");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "categoria");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "avaria");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "avaria");

            migrationBuilder.CreateIndex(
                name: "uq_vistoria_locacao_tipo",
                table: "vistoria",
                columns: new[] { "id_locacao", "tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "venda_id_veiculo_key",
                table: "venda",
                column: "id_veiculo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "venda_numero_nota_fiscal_key",
                table: "venda",
                column: "numero_nota_fiscal",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "veiculo_chassi_key",
                table: "veiculo",
                column: "chassi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "veiculo_placa_key",
                table: "veiculo",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "veiculo_renavam_key",
                table: "veiculo",
                column: "renavam",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "seguro_numero_apolice_key",
                table: "seguro",
                column: "numero_apolice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "locacao_id_reserva_key",
                table: "locacao",
                column: "id_reserva",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_setting_chave",
                table: "integration_setting",
                columns: new[] { "id_integration", "chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "integration_codigo_key",
                table: "integration",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "funcionario_cpf_key",
                table: "funcionario",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "funcionario_matricula_key",
                table: "funcionario",
                column: "matricula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cliente_cnh_numero_key",
                table: "cliente",
                column: "cnh_numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "cliente_cpf_key",
                table: "cliente",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "categoria_nome_key",
                table: "categoria",
                column: "nome",
                unique: true);
        }
    }
}
