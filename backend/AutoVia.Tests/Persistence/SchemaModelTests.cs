using AutoVia.Communication.Enums;
using AutoVia.Core.Entities;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Integrations.Entities;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Confere o modelo do EF contra a especificacao (Apendices A e B) mais os desvios
    /// aprovados em 04/11/2026 (docs/especificacao/desvios_aprovados.json): toda tabela
    /// com created_at/updated_at/disabled_at e nenhum indice unico no banco. Roda sem
    /// conexao com o PostgreSQL: o modelo e montado em memoria a partir das configuracoes.
    /// </summary>
    public class SchemaModelTests
    {
        private static readonly string[] TabelasNegocio = new string[]
        {
            "cliente", "funcionario", "categoria", "veiculo", "reserva", "locacao", "vistoria",
            "avaria", "cobranca_extra", "venda", "pagamento", "manutencao", "seguro", "sinistro",
            // Desvio aprovado em 12/11/2026 (RF-49): link de recuperacao de senha.
            "redefinicao_senha",
            // Desvio aprovado em 13/11/2026 (multi-locadora): tabela empresa.
            "empresa",
            // Desvio aprovado em 14/11/2026 (aprovacao do cadastro por locadora): vinculo cliente x locadora.
            "cliente_locadora",
            // Desvio aprovado por Kaio em 16/11/2026 (P9-INT-01; registro original de 13/11/2026): linha do tempo do sinistro.
            "sinistro_historico",
            // Desvio aprovado por Kaio em 16/11/2026 (fotos do veiculo, RF-94 a RF-101 e RF-105): tabela veiculo_foto.
            "veiculo_foto",
            // RF-23: cadastro e apuracao autorizados por Eduardo em 18/11/2026.
            "configuracao_cobranca_extra", "apuracao_cobranca_extra",
            // Desvio aprovado por Kaio em 21/11/2026 (P6-BE-02, RF-27): proposta de compra de seminovo.
            "proposta_compra"
        };

        private static readonly string[] TabelasIntegracao = new string[] { "integration", "integration_setting" };

        private static readonly string[] IndicesEsperados = new string[]
        {
            // indices de apoio (secao 6 do Apendice A + Apendice B)
            "idx_veiculo_status", "idx_veiculo_categoria", "idx_reserva_periodo", "idx_reserva_cliente",
            "idx_locacao_cliente", "idx_locacao_veiculo", "idx_locacao_status", "idx_pagamento_status",
            "idx_manutencao_veiculo", "idx_sinistro_veiculo", "idx_setting_integration",
            // Antigos UNIQUE da especificacao, agora indices comuns (desvio aprovado em 04/11/2026):
            // a unicidade e validada no servico sobre os registros ativos (disabled_at nulo).
            "idx_vistoria_locacao_tipo", "idx_setting_chave",
            "idx_cliente_cpf", "idx_cliente_cnh_numero", "idx_funcionario_cpf", "idx_funcionario_matricula",
            "idx_categoria_nome", "idx_veiculo_placa", "idx_veiculo_chassi", "idx_veiculo_renavam",
            "idx_locacao_reserva", "idx_venda_veiculo", "idx_venda_numero_nota_fiscal",
            "idx_seguro_numero_apolice", "idx_integration_codigo",
            // Desvio aprovado em 12/11/2026 (RF-49 e RF-51): indices comuns, sem UNIQUE.
            "idx_redefinicao_token", "idx_cliente_google_sub",
            // Desvio aprovado em 13/11/2026 (multi-locadora): indices comuns, sem UNIQUE.
            "idx_empresa_cnpj", "idx_empresa_email", "idx_funcionario_empresa", "idx_veiculo_empresa",
            // Desvio aprovado em 14/11/2026 (aprovacao por locadora): indices comuns, sem UNIQUE.
            "idx_cliente_locadora_empresa_cliente", "idx_cliente_locadora_cliente",
            // Desvio aprovado por Kaio em 16/11/2026 (P9-INT-01): indice comum, sem UNIQUE.
            "idx_sinistro_historico_sinistro",
            // Desvio aprovado por Kaio em 16/11/2026 (fotos do veiculo): indice comum, sem UNIQUE.
            "idx_veiculo_foto_veiculo",
            // RF-22: vínculo aditivo autorizado por Eduardo Freire em 18/11/2026.
            "idx_avaria_entrada",
            // RF-23: indices comuns do desvio autorizado.
            "idx_configuracao_extra_empresa_categoria", "idx_apuracao_extra_locacao", "idx_cobranca_apuracao",
            // Desvio aprovado por Kaio em 21/11/2026 (P6-BE-02): indices comuns, sem UNIQUE.
            "idx_proposta_compra_veiculo", "idx_proposta_compra_cliente"
        };

        /// <summary>
        /// Entidades SEM o filtro "Empresa" (lista fechada, multi-locadora): catalogo e dados globais da plataforma.
        /// Qualquer entidade nova precisa de filtro "Empresa" ou entrar aqui, em revisao explicita.
        /// </summary>
        private static readonly string[] EntidadesGlobais = new string[]
        {
            nameof(Cliente), nameof(Categoria), nameof(RedefinicaoSenha), nameof(Empresa), nameof(Integration), nameof(IntegrationSetting)
        };

        private static IModel BuildModel()
        {
            DbContextOptions<AutoViaDbContext> options = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .Options;

            using (AutoViaDbContext context = new AutoViaDbContext(options, ContextoEmpresaDeTeste.Vazio()))
            {
                // Modelo de design (o mesmo que gera as migrations): traz CHECK e
                // nomes de indice, que o modelo de execucao nao guarda.
                return context.GetService<IDesignTimeModel>().Model;
            }
        }

        [Fact]
        public void Modelo_TemExatamenteAsTabelasDaEspecificacaoMaisOsDesvios()
        {
            IModel model = BuildModel();

            List<string> tabelas = model.GetEntityTypes().Select(x => x.GetTableName()!).OrderBy(x => x).ToList();
            List<string> esperadas = TabelasNegocio.Concat(TabelasIntegracao).OrderBy(x => x).ToList();

            Assert.Equal(esperadas, tabelas);
        }

        [Fact]
        public void TodasAsEntidades_HerdamBaseEntities()
        {
            IModel model = BuildModel();

            foreach (IEntityType entity in model.GetEntityTypes())
            {
                Assert.True(typeof(BaseEntities).IsAssignableFrom(entity.ClrType), $"{entity.ClrType.Name} precisa herdar BaseEntities.");
            }
        }

        [Fact]
        public void TodasAsTabelas_TemColunasDeAuditoriaComExclusaoLogica()
        {
            IModel model = BuildModel();

            foreach (IEntityType entity in model.GetEntityTypes())
            {
                StoreObjectIdentifier table = StoreObjectIdentifier.Table(entity.GetTableName()!, null);
                IProperty createdAt = entity.FindProperty(nameof(BaseEntities.CreatedAt))!;
                IProperty updatedAt = entity.FindProperty(nameof(BaseEntities.UpdatedAt))!;
                IProperty disabledAt = entity.FindProperty(nameof(BaseEntities.DisabledAt))!;

                Assert.Equal("created_at", createdAt.GetColumnName(table));
                Assert.Equal("updated_at", updatedAt.GetColumnName(table));
                Assert.Equal("disabled_at", disabledAt.GetColumnName(table));
                Assert.False(createdAt.IsNullable);
                Assert.False(updatedAt.IsNullable);
                Assert.True(disabledAt.IsNullable);
                Assert.Equal("CURRENT_TIMESTAMP", createdAt.GetDefaultValueSql());
                Assert.Equal("CURRENT_TIMESTAMP", updatedAt.GetDefaultValueSql());
            }
        }

        [Fact]
        public void Banco_NaoTemIndiceUnico_UnicidadeFicaNoCodigo()
        {
            IModel model = BuildModel();

            List<string> unicos = model.GetEntityTypes()
                .SelectMany(x => x.GetIndexes())
                .Where(x => x.IsUnique)
                .Select(x => x.GetDatabaseName()!)
                .ToList();

            Assert.Empty(unicos);
        }

        [Fact]
        public void Disable_MarcaExclusaoLogicaSemApagar()
        {
            Categoria categoria = new Categoria { IdCategoria = 1, Nome = "SUV", ValorDiariaBase = 199.90m };

            categoria.Disable();

            Assert.NotNull(categoria.DisabledAt);
            Assert.Equal(categoria.DisabledAt, categoria.UpdatedAt);
            Assert.Equal("SUV", categoria.Nome);
        }

        [Fact]
        public void Indices_SaoExatamenteOsDaEspecificacao_SemIndicesAutomaticosDeFk()
        {
            IModel model = BuildModel();

            List<string> indices = model.GetEntityTypes()
                .SelectMany(x => x.GetIndexes())
                .Select(x => x.GetDatabaseName()!)
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(IndicesEsperados.OrderBy(x => x).ToList(), indices);
        }

        [Fact]
        public void Modelo_TemAsRestricoesCheckDaEspecificacaoMaisOsDesvios()
        {
            IModel model = BuildModel();

            int totalChecks = model.GetEntityTypes().SelectMany(x => x.GetCheckConstraints()).Count();

            // 35 anteriores + 4 da sinistro_historico (desvio aprovado por Kaio em 16/11/2026) + 1 da veiculo_foto (mesmo desvio de fotos).
            // RF-23 autorizado: 5 checks configuracao + 2 apuracao, preservando os 40 anteriores.
            Assert.Equal(52, totalChecks);
        }

        [Fact]
        public void SinistroHistorico_TemOsChecksDeStatusOrigemEFuncionario()
        {
            IModel model = BuildModel();

            IEntityType historico = model.GetEntityTypes().Single(x => x.GetTableName() == "sinistro_historico");
            Dictionary<string, string> checks = historico.GetCheckConstraints().ToDictionary(x => x.Name!, x => x.Sql);
            string statusValidos = "('ABERTO', 'EM_ANALISE', 'APROVADO', 'NEGADO', 'ENCERRADO')";

            Assert.Equal("status_anterior IS NULL OR status_anterior IN " + statusValidos, checks["ck_sinistro_historico_status_anterior"]);
            Assert.Equal("status_novo IN " + statusValidos, checks["ck_sinistro_historico_status_novo"]);
            Assert.Equal("origem IN ('FUNCIONARIO', 'SEGURADORA')", checks["ck_sinistro_historico_origem"]);
            Assert.Equal("origem <> 'FUNCIONARIO' OR id_funcionario IS NOT NULL", checks["ck_sinistro_historico_funcionario"]);
        }

        [Fact]
        public void SinistroHistorico_ChavesEstrangeirasSemCascataEColunasNulaveisCertas()
        {
            IModel model = BuildModel();

            IEntityType historico = model.GetEntityTypes().Single(x => x.GetTableName() == "sinistro_historico");
            List<string> fks = historico.GetForeignKeys().Select(x => x.GetConstraintName()!).OrderBy(x => x).ToList();

            Assert.Equal(new List<string> { "fk_sinistro_historico_funcionario", "fk_sinistro_historico_sinistro" }, fks);
            Assert.All(historico.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
            Assert.True(historico.FindProperty(nameof(SinistroHistorico.StatusAnterior))!.IsNullable);
            Assert.False(historico.FindProperty(nameof(SinistroHistorico.StatusNovo))!.IsNullable);
            Assert.True(historico.FindProperty(nameof(SinistroHistorico.IdFuncionario))!.IsNullable);
            Assert.Equal(typeof(string), historico.FindProperty(nameof(SinistroHistorico.Origem))!.GetProviderClrType());

            IEntityType sinistro = model.GetEntityTypes().Single(x => x.GetTableName() == "sinistro");
            IProperty protocolo = sinistro.FindProperty(nameof(Sinistro.ProtocoloSeguradora))!;
            Assert.True(protocolo.IsNullable);
            Assert.Equal(40, protocolo.GetMaxLength());
        }

        [Fact]
        public void ClienteLocadora_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("cliente_locadora", null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "cliente_locadora");

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_cliente_locadora"] = "integer",
                ["id_cliente"] = "integer",
                ["id_empresa"] = "integer",
                ["status"] = "character varying(15)",
                ["justificativa_recusa"] = "character varying(500)",
                ["id_funcionario_verificador"] = "integer",
                ["data_solicitacao"] = "timestamp without time zone",
                ["data_verificacao"] = "timestamp without time zone",
                ["created_at"] = "timestamp without time zone",
                ["updated_at"] = "timestamp without time zone",
                ["disabled_at"] = "timestamp without time zone"
            };

            List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).OrderBy(x => x).ToList();
            Assert.Equal(tiposEsperados.Keys.OrderBy(x => x).ToList(), colunas);

            List<string> anulaveisEsperadas = new List<string> { "justificativa_recusa", "id_funcionario_verificador", "data_verificacao", "disabled_at" };
            foreach (IProperty propriedade in entity.GetProperties())
            {
                string coluna = propriedade.GetColumnName(table)!;
                Assert.Equal(tiposEsperados[coluna], propriedade.GetColumnType(table));

                bool anulavelEsperado = anulaveisEsperadas.Contains(coluna);
                Assert.Equal(anulavelEsperado, propriedade.IsNullable);
            }
        }

        [Fact]
        public void ClienteLocadora_TemOCheckDeStatusDoCadastro()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "cliente_locadora");

            List<ICheckConstraint> checks = entity.GetCheckConstraints().ToList();

            Assert.Single(checks);
            Assert.Equal("ck_cliente_locadora_status", checks[0].Name);
            Assert.Equal("status IN ('PENDENTE', 'APROVADO', 'RECUSADO')", checks[0].Sql);
        }

        [Fact]
        public void ClienteLocadora_TemTresFksNoActionEChavePrimariaComNomePadrao()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "cliente_locadora");

            Dictionary<string, Type> fksEsperadas = new Dictionary<string, Type>
            {
                ["fk_cliente_locadora_cliente"] = typeof(Cliente),
                ["fk_cliente_locadora_empresa"] = typeof(Empresa),
                ["fk_cliente_locadora_funcionario"] = typeof(Funcionario)
            };

            List<IForeignKey> fks = entity.GetForeignKeys().ToList();
            Assert.Equal(fksEsperadas.Count, fks.Count);

            foreach (KeyValuePair<string, Type> esperada in fksEsperadas)
            {
                IForeignKey fk = fks.Single(x => x.GetConstraintName() == esperada.Key);

                Assert.Equal(esperada.Value, fk.PrincipalEntityType.ClrType);
                Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
            }

            Assert.Equal("cliente_locadora_pkey", entity.FindPrimaryKey()!.GetName());
        }

        [Fact]
        public void ClienteLocadora_TemDoisIndicesComunsENenhumUnico()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "cliente_locadora");

            List<string> nomes = entity.GetIndexes().Select(x => x.GetDatabaseName()!).OrderBy(x => x).ToList();
            List<string> esperados = new List<string> { "idx_cliente_locadora_cliente", "idx_cliente_locadora_empresa_cliente" };

            Assert.Equal(esperados, nomes);
            Assert.All(entity.GetIndexes(), indice => Assert.False(indice.IsUnique));
        }

        [Fact]
        public void Empresa_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("empresa", null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "empresa");

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_empresa"] = "integer",
                ["razao_social"] = "character varying(150)",
                ["nome_fantasia"] = "character varying(100)",
                ["cnpj"] = "character(14)",
                ["telefone"] = "character varying(20)",
                ["email"] = "character varying(120)",
                ["cep"] = "character(8)",
                ["logradouro"] = "character varying(120)",
                ["numero"] = "character varying(10)",
                ["complemento"] = "character varying(60)",
                ["bairro"] = "character varying(60)",
                ["cidade"] = "character varying(60)",
                ["uf"] = "character(2)",
                ["ativo"] = "boolean",
                ["operadora"] = "boolean",
                ["created_at"] = "timestamp without time zone",
                ["updated_at"] = "timestamp without time zone",
                ["disabled_at"] = "timestamp without time zone"
            };

            List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).OrderBy(x => x).ToList();
            Assert.Equal(tiposEsperados.Keys.OrderBy(x => x).ToList(), colunas);

            List<string> anulaveisEsperadas = new List<string> { "complemento", "disabled_at" };
            foreach (IProperty propriedade in entity.GetProperties())
            {
                string coluna = propriedade.GetColumnName(table)!;
                Assert.Equal(tiposEsperados[coluna], propriedade.GetColumnType(table));

                bool anulavelEsperado = anulaveisEsperadas.Contains(coluna);
                Assert.Equal(anulavelEsperado, propriedade.IsNullable);
            }
        }

        [Fact]
        public void Empresa_TemOCheckDeCnpjComExatamente14Digitos()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "empresa");

            List<ICheckConstraint> checks = entity.GetCheckConstraints().ToList();

            Assert.Single(checks);
            Assert.Equal("ck_empresa_cnpj", checks[0].Name);
            Assert.Equal("cnpj ~ '^[0-9]{14}$'", checks[0].Sql);
        }

        [Fact]
        public void IdEmpresa_ObrigatorioComFkNoActionEmFuncionarioEVeiculo()
        {
            IModel model = BuildModel();
            Dictionary<string, string> fksEsperadas = new Dictionary<string, string>
            {
                ["funcionario"] = "fk_funcionario_empresa",
                ["veiculo"] = "fk_veiculo_empresa"
            };

            foreach (KeyValuePair<string, string> esperado in fksEsperadas)
            {
                IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == esperado.Key);
                StoreObjectIdentifier table = StoreObjectIdentifier.Table(esperado.Key, null);
                IProperty idEmpresa = entity.FindProperty("IdEmpresa")!;

                Assert.Equal("id_empresa", idEmpresa.GetColumnName(table));
                Assert.False(idEmpresa.IsNullable);

                IForeignKey fk = entity.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(Empresa));
                Assert.Equal(esperado.Value, fk.GetConstraintName());
                Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
            }
        }

        [Fact]
        public void CheckDeCargoDeFuncionario_AceitaAdministrador()
        {
            IModel model = BuildModel();
            IEntityType funcionario = model.GetEntityTypes().Single(x => x.GetTableName() == "funcionario");

            ICheckConstraint cargo = funcionario.GetCheckConstraints().Single(x => x.Name == "ck_funcionario_cargo");

            Assert.Equal(
                "cargo IN ('ATENDENTE', 'VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', 'ADMINISTRADOR')",
                cargo.Sql);
        }

        [Fact]
        public void Administrador_EhOUltimoMembroDoEnumCargoFuncionario()
        {
            CargoFuncionario[] valores = Enum.GetValues<CargoFuncionario>();

            Assert.Equal(CargoFuncionario.ADMINISTRADOR, valores[valores.Length - 1]);
            Assert.Equal(CargoFuncionario.ATENDENTE, valores[0]);
        }

        [Fact]
        public void CamposDeValoresFixos_SaoGravadosComoTexto()
        {
            IModel model = BuildModel();

            IEntityType veiculo = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo");
            IProperty status = veiculo.FindProperty("Status")!;

            Assert.Equal(typeof(string), status.GetProviderClrType());
            Assert.Equal(20, status.GetMaxLength());
        }

        [Fact]
        public void Veiculo_ModeloTem60Caracteres_FornecedorETemNotaFiscalSaoAnulaveisComOTamanhoDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("veiculo", null);
            IEntityType veiculo = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo");

            IProperty modelo = veiculo.FindProperty(nameof(Veiculo.Modelo))!;
            IProperty fornecedor = veiculo.FindProperty(nameof(Veiculo.Fornecedor))!;
            IProperty notaFiscal = veiculo.FindProperty(nameof(Veiculo.NumeroNotaFiscalCompra))!;

            Assert.Equal("modelo", modelo.GetColumnName(table));
            Assert.Equal(60, modelo.GetMaxLength());
            Assert.Equal("fornecedor", fornecedor.GetColumnName(table));
            Assert.Equal(100, fornecedor.GetMaxLength());
            Assert.True(fornecedor.IsNullable);
            Assert.Equal("numero_nota_fiscal_compra", notaFiscal.GetColumnName(table));
            Assert.Equal(20, notaFiscal.GetMaxLength());
            Assert.True(notaFiscal.IsNullable);
        }

        [Fact]
        public void Veiculo_FornecedorENotaFiscalNaoTemIndice()
        {
            IModel model = BuildModel();
            IEntityType veiculo = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo");

            List<string> colunasIndexadas = veiculo.GetIndexes()
                .SelectMany(x => x.Properties)
                .Select(x => x.Name)
                .ToList();

            Assert.DoesNotContain(nameof(Veiculo.Fornecedor), colunasIndexadas);
            Assert.DoesNotContain(nameof(Veiculo.NumeroNotaFiscalCompra), colunasIndexadas);
        }

        [Fact]
        public void ChavesPrimarias_UsamSerialComONomePadraoDoPostgres()
        {
            IModel model = BuildModel();

            foreach (IEntityType entity in model.GetEntityTypes())
            {
                IKey key = entity.FindPrimaryKey()!;
                Assert.Equal($"{entity.GetTableName()}_pkey", key.GetName());
            }
        }

        [Fact]
        public void ExclusaoEmCascata_SoNasComposicoesDaEspecificacao()
        {
            IModel model = BuildModel();

            List<string> cascatas = model.GetEntityTypes()
                .SelectMany(x => x.GetForeignKeys())
                .Where(x => x.DeleteBehavior == DeleteBehavior.Cascade)
                .Select(x => x.GetConstraintName()!)
                .OrderBy(x => x)
                .ToList();

            List<string> esperadas = new List<string>
            {
                "fk_avaria_vistoria", "fk_cobranca_locacao", "fk_setting_integration", "fk_vistoria_locacao"
            };

            Assert.Equal(esperadas, cascatas);
        }

        [Fact]
        public void RedefinicaoSenha_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("redefinicao_senha", null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "redefinicao_senha");

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_redefinicao_senha"] = "integer",
                ["id_cliente"] = "integer",
                ["id_funcionario"] = "integer",
                ["token_hash"] = "character(64)",
                ["expira_em"] = "timestamp without time zone",
                ["created_at"] = "timestamp without time zone",
                ["updated_at"] = "timestamp without time zone",
                ["disabled_at"] = "timestamp without time zone"
            };

            List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).OrderBy(x => x).ToList();
            Assert.Equal(tiposEsperados.Keys.OrderBy(x => x).ToList(), colunas);

            List<string> anulaveisEsperadas = new List<string> { "id_cliente", "id_funcionario", "disabled_at" };
            foreach (IProperty propriedade in entity.GetProperties())
            {
                string coluna = propriedade.GetColumnName(table)!;
                Assert.Equal(tiposEsperados[coluna], propriedade.GetColumnType(table));

                bool anulavelEsperado = anulaveisEsperadas.Contains(coluna);
                Assert.Equal(anulavelEsperado, propriedade.IsNullable);
            }
        }

        [Fact]
        public void RedefinicaoSenha_TemUmSoCheckDeTitularUnico()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "redefinicao_senha");

            List<ICheckConstraint> checks = entity.GetCheckConstraints().ToList();

            Assert.Single(checks);
            Assert.Equal("ck_redefinicao_titular", checks[0].Name);
            Assert.Equal(
                "(id_cliente IS NOT NULL AND id_funcionario IS NULL) OR (id_cliente IS NULL AND id_funcionario IS NOT NULL)",
                checks[0].Sql);
        }

        [Fact]
        public void VeiculoFoto_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("veiculo_foto", null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo_foto");

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_veiculo_foto"] = "integer",
                ["id_veiculo"] = "integer",
                ["ordem"] = "integer",
                ["chave_objeto"] = "character varying(200)",
                ["chave_miniatura"] = "character varying(200)",
                ["id_funcionario_envio"] = "integer",
                ["id_funcionario_remocao"] = "integer",
                ["created_at"] = "timestamp without time zone",
                ["updated_at"] = "timestamp without time zone",
                ["disabled_at"] = "timestamp without time zone"
            };

            List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).OrderBy(x => x).ToList();
            Assert.Equal(tiposEsperados.Keys.OrderBy(x => x).ToList(), colunas);

            List<string> anulaveisEsperadas = new List<string> { "chave_miniatura", "id_funcionario_remocao", "disabled_at" };
            foreach (IProperty propriedade in entity.GetProperties())
            {
                string coluna = propriedade.GetColumnName(table)!;
                Assert.Equal(tiposEsperados[coluna], propriedade.GetColumnType(table));

                bool anulavelEsperado = anulaveisEsperadas.Contains(coluna);
                Assert.Equal(anulavelEsperado, propriedade.IsNullable);
            }
        }

        [Fact]
        public void VeiculoFoto_TemOCheckDeOrdemMaiorOuIgualAUm()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo_foto");

            List<ICheckConstraint> checks = entity.GetCheckConstraints().ToList();

            Assert.Single(checks);
            Assert.Equal("ck_veiculo_foto_ordem", checks[0].Name);
            Assert.Equal("ordem >= 1", checks[0].Sql);
        }

        [Fact]
        public void VeiculoFoto_TemTresFksNoActionEChavePrimariaComNomePadrao()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo_foto");

            Dictionary<string, Type> fksEsperadas = new Dictionary<string, Type>
            {
                ["fk_veiculo_foto_veiculo"] = typeof(Veiculo),
                ["fk_veiculo_foto_funcionario_envio"] = typeof(Funcionario),
                ["fk_veiculo_foto_funcionario_remocao"] = typeof(Funcionario)
            };

            List<IForeignKey> fks = entity.GetForeignKeys().ToList();
            Assert.Equal(fksEsperadas.Count, fks.Count);

            foreach (KeyValuePair<string, Type> esperada in fksEsperadas)
            {
                IForeignKey fk = fks.Single(x => x.GetConstraintName() == esperada.Key);

                Assert.Equal(esperada.Value, fk.PrincipalEntityType.ClrType);
                Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
            }

            Assert.Equal("veiculo_foto_pkey", entity.FindPrimaryKey()!.GetName());
        }

        [Fact]
        public void VeiculoFoto_TemUmIndiceComumPorVeiculoEOrdemENenhumUnico()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == "veiculo_foto");
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("veiculo_foto", null);

            IIndex indice = Assert.Single(entity.GetIndexes());

            Assert.Equal("idx_veiculo_foto_veiculo", indice.GetDatabaseName());
            Assert.False(indice.IsUnique);
            Assert.Equal(new List<string> { "id_veiculo", "ordem" }, indice.Properties.Select(x => x.GetColumnName(table)!).ToList());
        }

        [Fact]
        public void Cliente_GoogleSubTem255CaracteresEAnulavel()
        {
            IModel model = BuildModel();
            IEntityType cliente = model.GetEntityTypes().Single(x => x.ClrType == typeof(Cliente));
            IProperty googleSub = cliente.FindProperty(nameof(Cliente.GoogleSub))!;
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("cliente", null);

            Assert.Equal("google_sub", googleSub.GetColumnName(table));
            Assert.Equal(255, googleSub.GetMaxLength());
            Assert.True(googleSub.IsNullable);
        }

        [Fact]
        public void TodaEntidade_TemOFiltroEmpresaOuEstaNaListaDeGlobais()
        {
            IModel model = BuildModel();

            List<string> semClassificacao = model.GetEntityTypes()
                .Where(x => x.FindDeclaredQueryFilter(ConstantsValues.NomeFiltroEmpresa) == null)
                .Select(x => x.ClrType.Name)
                .Where(nome => !EntidadesGlobais.Contains(nome))
                .OrderBy(x => x)
                .ToList();

            Assert.Empty(semClassificacao);
        }

        [Fact]
        public void ListaDeGlobais_BateComAsEntidadesSemFiltroDeEmpresa()
        {
            IModel model = BuildModel();

            List<string> semFiltro = model.GetEntityTypes()
                .Where(x => x.FindDeclaredQueryFilter(ConstantsValues.NomeFiltroEmpresa) == null)
                .Select(x => x.ClrType.Name)
                .OrderBy(x => x)
                .ToList();

            Assert.Equal(EntidadesGlobais.OrderBy(x => x).ToList(), semFiltro);
        }

        [Fact]
        public void Modelo_NaoGeraAvisoDeNavegacaoObrigatoriaComFiltro()
        {
            DbContextOptions<AutoViaDbContext> options = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .ConfigureWarnings(avisos => avisos.Throw(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options;

            // O modelo de execucao e validado ao ser montado: um aviso vira excecao neste teste.
            using AutoViaDbContext context = new AutoViaDbContext(options, ContextoEmpresaDeTeste.Vazio());
            IModel model = context.Model;

            Assert.NotNull(model);
        }

        [Fact]
        public void IndicesDoDesvio_SaoComuns_NenhumEUnico()
        {
            IModel model = BuildModel();
            List<string> novos = new List<string> { "idx_redefinicao_token", "idx_cliente_google_sub" };

            List<IIndex> encontrados = model.GetEntityTypes()
                .SelectMany(x => x.GetIndexes())
                .Where(x => novos.Contains(x.GetDatabaseName()!))
                .ToList();

            List<string> nomes = encontrados.Select(x => x.GetDatabaseName()!).OrderBy(x => x).ToList();
            Assert.Equal(novos.OrderBy(x => x).ToList(), nomes);

            foreach (IIndex indice in encontrados)
            {
                Assert.False(indice.IsUnique);
            }
        }
    }
}
