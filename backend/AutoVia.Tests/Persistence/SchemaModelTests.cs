using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Confere o modelo do EF contra a especificacao (Apendices A e B) sem abrir
    /// conexao com o PostgreSQL: o modelo e montado em memoria a partir das
    /// configuracoes. Se alguem mudar uma configuracao e quebrar o esquema, estes
    /// testes falham antes de qualquer migration nova.
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
            // UNIQUE nomeados
            "uq_vistoria_locacao_tipo", "uq_setting_chave",
            // UNIQUE em linha (nome no padrao do PostgreSQL)
            "cliente_cpf_key", "cliente_cnh_numero_key", "funcionario_cpf_key", "funcionario_matricula_key",
            "categoria_nome_key", "veiculo_placa_key", "veiculo_chassi_key", "veiculo_renavam_key",
            "locacao_id_reserva_key", "venda_id_veiculo_key", "venda_numero_nota_fiscal_key",
            "seguro_numero_apolice_key", "integration_codigo_key"
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
        public void TabelasDeNegocio_NaoTemColunasDeAuditoria()
        {
            IModel model = BuildModel();

            foreach (IEntityType entity in model.GetEntityTypes().Where(x => TabelasNegocio.Contains(x.GetTableName())))
            {
                StoreObjectIdentifier table = StoreObjectIdentifier.Table(entity.GetTableName()!, null);
                List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).ToList();

                Assert.DoesNotContain("created_at", colunas);
                Assert.DoesNotContain("updated_at", colunas);
                Assert.DoesNotContain("disabled_at", colunas);
            }
        }

        [Fact]
        public void IntegrationSetting_NaoTemDisabledAt()
        {
            IModel model = BuildModel();

            IEntityType setting = model.GetEntityTypes().Single(x => x.GetTableName() == "integration_setting");
            StoreObjectIdentifier table = StoreObjectIdentifier.Table("integration_setting", null);
            List<string> colunas = setting.GetProperties().Select(x => x.GetColumnName(table)!).ToList();

            Assert.Contains("created_at", colunas);
            Assert.Contains("updated_at", colunas);
            Assert.DoesNotContain("disabled_at", colunas);
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
