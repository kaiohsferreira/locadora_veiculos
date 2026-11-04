using AutoVia.Core.Entities;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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
            "avaria", "cobranca_extra", "venda", "pagamento", "manutencao", "seguro", "sinistro"
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
            "idx_seguro_numero_apolice", "idx_integration_codigo"
        };

        private static IModel BuildModel()
        {
            DbContextOptions<AutoViaDbContext> options = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .Options;

            using (AutoViaDbContext context = new AutoViaDbContext(options))
            {
                // Modelo de design (o mesmo que gera as migrations): traz CHECK e
                // nomes de indice, que o modelo de execucao nao guarda.
                return context.GetService<IDesignTimeModel>().Model;
            }
        }

        [Fact]
        public void Modelo_TemExatamenteAs16TabelasDaEspecificacao()
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
        public void Modelo_TemAs32RestricoesCheckDaEspecificacao()
        {
            IModel model = BuildModel();

            int totalChecks = model.GetEntityTypes().SelectMany(x => x.GetCheckConstraints()).Count();

            Assert.Equal(32, totalChecks);
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
    }
}
