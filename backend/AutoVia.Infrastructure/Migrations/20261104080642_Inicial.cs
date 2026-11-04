using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoVia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categoria",
                columns: table => new
                {
                    id_categoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    valor_diaria_base = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categoria_pkey", x => x.id_categoria);
                    table.CheckConstraint("ck_categoria_valor", "valor_diaria_base > 0");
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cpf = table.Column<string>(type: "character(11)", fixedLength: true, maxLength: 11, nullable: false),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cnh_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cnh_categoria = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    cnh_validade = table.Column<DateOnly>(type: "date", nullable: false),
                    logradouro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    bairro = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    cidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    uf = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    cep = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: true),
                    data_cadastro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status_cadastro = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PENDENTE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("cliente_pkey", x => x.id_cliente);
                    table.CheckConstraint("ck_cliente_cnh_cat", "cnh_categoria IN ('A', 'B', 'AB', 'C', 'D', 'E')");
                    table.CheckConstraint("ck_cliente_status", "status_cadastro IN ('PENDENTE', 'APROVADO', 'RECUSADO')");
                });

            migrationBuilder.CreateTable(
                name: "funcionario",
                columns: table => new
                {
                    id_funcionario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cpf = table.Column<string>(type: "character(11)", fixedLength: true, maxLength: 11, nullable: false),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cargo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    data_admissao = table.Column<DateOnly>(type: "date", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("funcionario_pkey", x => x.id_funcionario);
                    table.CheckConstraint("ck_funcionario_cargo", "cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO')");
                });

            migrationBuilder.CreateTable(
                name: "integration",
                columns: table => new
                {
                    id_integration = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ambiente = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PRODUCAO"),
                    base_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    timeout_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 30000),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    disabled_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("integration_pkey", x => x.id_integration);
                    table.CheckConstraint("ck_integration_ambiente", "ambiente IN ('SANDBOX', 'HOMOLOGACAO', 'PRODUCAO')");
                    table.CheckConstraint("ck_integration_timeout", "timeout_ms > 0");
                });

            migrationBuilder.CreateTable(
                name: "veiculo",
                columns: table => new
                {
                    id_veiculo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_categoria = table.Column<int>(type: "integer", nullable: false),
                    placa = table.Column<string>(type: "character(7)", fixedLength: true, maxLength: 7, nullable: false),
                    chassi = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    renavam = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    marca = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    modelo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ano_fabricacao = table.Column<short>(type: "smallint", nullable: false),
                    ano_modelo = table.Column<short>(type: "smallint", nullable: false),
                    cor = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    km_atual = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_venda = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DISPONIVEL"),
                    data_aquisicao = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("veiculo_pkey", x => x.id_veiculo);
                    table.CheckConstraint("ck_veiculo_ano", "ano_modelo >= ano_fabricacao");
                    table.CheckConstraint("ck_veiculo_diaria", "valor_diaria > 0");
                    table.CheckConstraint("ck_veiculo_km", "km_atual >= 0");
                    table.CheckConstraint("ck_veiculo_status", "status IN ('DISPONIVEL', 'RESERVADO', 'LOCADO', 'MANUTENCAO', 'VENDIDO', 'BAIXADO')");
                    table.ForeignKey(
                        name: "fk_veiculo_categoria",
                        column: x => x.id_categoria,
                        principalTable: "categoria",
                        principalColumn: "id_categoria");
                });

            migrationBuilder.CreateTable(
                name: "integration_setting",
                columns: table => new
                {
                    id_setting = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_integration = table.Column<int>(type: "integer", nullable: false),
                    chave = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    valor = table.Column<string>(type: "text", nullable: false),
                    protegido = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("integration_setting_pkey", x => x.id_setting);
                    table.ForeignKey(
                        name: "fk_setting_integration",
                        column: x => x.id_integration,
                        principalTable: "integration",
                        principalColumn: "id_integration",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manutencao",
                columns: table => new
                {
                    id_manutencao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    data_entrada = table.Column<DateOnly>(type: "date", nullable: false),
                    data_saida = table.Column<DateOnly>(type: "date", nullable: true),
                    km_registrado = table.Column<int>(type: "integer", nullable: false),
                    oficina = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    custo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("manutencao_pkey", x => x.id_manutencao);
                    table.CheckConstraint("ck_manutencao_periodo", "data_saida IS NULL OR data_saida >= data_entrada");
                    table.CheckConstraint("ck_manutencao_tipo", "tipo IN ('PREVENTIVA', 'CORRETIVA')");
                    table.ForeignKey(
                        name: "fk_manutencao_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "reserva",
                columns: table => new
                {
                    id_reserva = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    data_reserva = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    data_retirada_prevista = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_devolucao_prevista = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    valor_estimado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ATIVA")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reserva_pkey", x => x.id_reserva);
                    table.CheckConstraint("ck_reserva_periodo", "data_devolucao_prevista > data_retirada_prevista");
                    table.CheckConstraint("ck_reserva_status", "status IN ('ATIVA', 'CONFIRMADA', 'CANCELADA', 'CONCLUIDA')");
                    table.ForeignKey(
                        name: "fk_reserva_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_reserva_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "seguro",
                columns: table => new
                {
                    id_seguro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    seguradora = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    numero_apolice = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_premio = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    cobertura = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("seguro_pkey", x => x.id_seguro);
                    table.CheckConstraint("ck_seguro_vigencia", "data_fim > data_inicio");
                    table.ForeignKey(
                        name: "fk_seguro_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "venda",
                columns: table => new
                {
                    id_venda = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    data_venda = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    valor_venda = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    forma_pagamento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    numero_nota_fiscal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status_documentacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "PENDENTE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("venda_pkey", x => x.id_venda);
                    table.CheckConstraint("ck_venda_doc", "status_documentacao IN ('PENDENTE', 'EM_TRANSFERENCIA', 'CONCLUIDA')");
                    table.CheckConstraint("ck_venda_forma", "forma_pagamento IN ('A_VISTA', 'FINANCIAMENTO', 'CONSORCIO')");
                    table.CheckConstraint("ck_venda_valor", "valor_venda > 0");
                    table.ForeignKey(
                        name: "fk_venda_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_venda_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_venda_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "locacao",
                columns: table => new
                {
                    id_locacao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_reserva = table.Column<int>(type: "integer", nullable: true),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    data_retirada = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_devolucao_prevista = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_devolucao_efetiva = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    km_saida = table.Column<int>(type: "integer", nullable: false),
                    km_chegada = table.Column<int>(type: "integer", nullable: true),
                    valor_diaria = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_caucao = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValueSql: "0"),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "EM_ANDAMENTO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("locacao_pkey", x => x.id_locacao);
                    table.CheckConstraint("ck_locacao_devolucao", "data_devolucao_efetiva IS NULL OR data_devolucao_efetiva >= data_retirada");
                    table.CheckConstraint("ck_locacao_km", "km_chegada IS NULL OR km_chegada >= km_saida");
                    table.CheckConstraint("ck_locacao_status", "status IN ('EM_ANDAMENTO', 'FINALIZADA', 'ATRASADA', 'CANCELADA')");
                    table.ForeignKey(
                        name: "fk_locacao_cliente",
                        column: x => x.id_cliente,
                        principalTable: "cliente",
                        principalColumn: "id_cliente");
                    table.ForeignKey(
                        name: "fk_locacao_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_locacao_reserva",
                        column: x => x.id_reserva,
                        principalTable: "reserva",
                        principalColumn: "id_reserva");
                    table.ForeignKey(
                        name: "fk_locacao_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "cobranca_extra",
                columns: table => new
                {
                    id_cobranca = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    data_lancamento = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("cobranca_extra_pkey", x => x.id_cobranca);
                    table.CheckConstraint("ck_cobranca_tipo", "tipo IN ('MULTA_TRANSITO', 'KM_EXCEDENTE', 'ATRASO', 'AVARIA', 'COMBUSTIVEL', 'LIMPEZA')");
                    table.CheckConstraint("ck_cobranca_valor", "valor >= 0");
                    table.ForeignKey(
                        name: "fk_cobranca_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagamento",
                columns: table => new
                {
                    id_pagamento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: true),
                    id_venda = table.Column<int>(type: "integer", nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    data_vencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    forma = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "PENDENTE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pagamento_pkey", x => x.id_pagamento);
                    table.CheckConstraint("ck_pagamento_origem", "(id_locacao IS NOT NULL AND id_venda IS NULL) OR (id_locacao IS NULL AND id_venda IS NOT NULL)");
                    table.CheckConstraint("ck_pagamento_status", "status IN ('PENDENTE', 'PAGO', 'ATRASADO', 'CANCELADO')");
                    table.CheckConstraint("ck_pagamento_tipo", "tipo IN ('CAUCAO', 'DIARIA', 'COBRANCA_EXTRA', 'VENDA')");
                    table.CheckConstraint("ck_pagamento_valor", "valor > 0");
                    table.ForeignKey(
                        name: "fk_pagamento_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao");
                    table.ForeignKey(
                        name: "fk_pagamento_venda",
                        column: x => x.id_venda,
                        principalTable: "venda",
                        principalColumn: "id_venda");
                });

            migrationBuilder.CreateTable(
                name: "sinistro",
                columns: table => new
                {
                    id_sinistro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_veiculo = table.Column<int>(type: "integer", nullable: false),
                    id_seguro = table.Column<int>(type: "integer", nullable: true),
                    id_locacao = table.Column<int>(type: "integer", nullable: true),
                    data_ocorrencia = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    valor_prejuizo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "ABERTO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("sinistro_pkey", x => x.id_sinistro);
                    table.CheckConstraint("ck_sinistro_status", "status IN ('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')");
                    table.CheckConstraint("ck_sinistro_tipo", "tipo IN ('COLISAO', 'ROUBO', 'FURTO', 'INCENDIO', 'FENOMENO_NATURAL', 'TERCEIROS')");
                    table.ForeignKey(
                        name: "fk_sinistro_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao");
                    table.ForeignKey(
                        name: "fk_sinistro_seguro",
                        column: x => x.id_seguro,
                        principalTable: "seguro",
                        principalColumn: "id_seguro");
                    table.ForeignKey(
                        name: "fk_sinistro_veiculo",
                        column: x => x.id_veiculo,
                        principalTable: "veiculo",
                        principalColumn: "id_veiculo");
                });

            migrationBuilder.CreateTable(
                name: "vistoria",
                columns: table => new
                {
                    id_vistoria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_locacao = table.Column<int>(type: "integer", nullable: false),
                    id_funcionario = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    km_registrado = table.Column<int>(type: "integer", nullable: false),
                    nivel_combustivel = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("vistoria_pkey", x => x.id_vistoria);
                    table.CheckConstraint("ck_vistoria_combustivel", "nivel_combustivel BETWEEN 0 AND 1");
                    table.CheckConstraint("ck_vistoria_tipo", "tipo IN ('ENTRADA', 'SAIDA')");
                    table.ForeignKey(
                        name: "fk_vistoria_funcionario",
                        column: x => x.id_funcionario,
                        principalTable: "funcionario",
                        principalColumn: "id_funcionario");
                    table.ForeignKey(
                        name: "fk_vistoria_locacao",
                        column: x => x.id_locacao,
                        principalTable: "locacao",
                        principalColumn: "id_locacao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "avaria",
                columns: table => new
                {
                    id_avaria = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    id_vistoria = table.Column<int>(type: "integer", nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    gravidade = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, defaultValue: "LEVE"),
                    valor_estimado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avaria_pkey", x => x.id_avaria);
                    table.CheckConstraint("ck_avaria_gravidade", "gravidade IN ('LEVE', 'MEDIA', 'GRAVE')");
                    table.ForeignKey(
                        name: "fk_avaria_vistoria",
                        column: x => x.id_vistoria,
                        principalTable: "vistoria",
                        principalColumn: "id_vistoria",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "categoria_nome_key",
                table: "categoria",
                column: "nome",
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
                name: "integration_codigo_key",
                table: "integration",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_setting_integration",
                table: "integration_setting",
                column: "id_integration");

            migrationBuilder.CreateIndex(
                name: "uq_setting_chave",
                table: "integration_setting",
                columns: new[] { "id_integration", "chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_locacao_cliente",
                table: "locacao",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "idx_locacao_status",
                table: "locacao",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_locacao_veiculo",
                table: "locacao",
                column: "id_veiculo");

            migrationBuilder.CreateIndex(
                name: "locacao_id_reserva_key",
                table: "locacao",
                column: "id_reserva",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_manutencao_veiculo",
                table: "manutencao",
                columns: new[] { "id_veiculo", "data_entrada" });

            migrationBuilder.CreateIndex(
                name: "idx_pagamento_status",
                table: "pagamento",
                columns: new[] { "status", "data_vencimento" });

            migrationBuilder.CreateIndex(
                name: "idx_reserva_cliente",
                table: "reserva",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "idx_reserva_periodo",
                table: "reserva",
                columns: new[] { "data_retirada_prevista", "data_devolucao_prevista" });

            migrationBuilder.CreateIndex(
                name: "seguro_numero_apolice_key",
                table: "seguro",
                column: "numero_apolice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sinistro_veiculo",
                table: "sinistro",
                columns: new[] { "id_veiculo", "data_ocorrencia" });

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_categoria",
                table: "veiculo",
                column: "id_categoria");

            migrationBuilder.CreateIndex(
                name: "idx_veiculo_status",
                table: "veiculo",
                column: "status");

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
                name: "uq_vistoria_locacao_tipo",
                table: "vistoria",
                columns: new[] { "id_locacao", "tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avaria");

            migrationBuilder.DropTable(
                name: "cobranca_extra");

            migrationBuilder.DropTable(
                name: "integration_setting");

            migrationBuilder.DropTable(
                name: "manutencao");

            migrationBuilder.DropTable(
                name: "pagamento");

            migrationBuilder.DropTable(
                name: "sinistro");

            migrationBuilder.DropTable(
                name: "vistoria");

            migrationBuilder.DropTable(
                name: "integration");

            migrationBuilder.DropTable(
                name: "venda");

            migrationBuilder.DropTable(
                name: "seguro");

            migrationBuilder.DropTable(
                name: "locacao");

            migrationBuilder.DropTable(
                name: "funcionario");

            migrationBuilder.DropTable(
                name: "reserva");

            migrationBuilder.DropTable(
                name: "cliente");

            migrationBuilder.DropTable(
                name: "veiculo");

            migrationBuilder.DropTable(
                name: "categoria");
        }
    }
}
