using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Modelo do EF da tabela contrato_locacao (desvio D10 do contrato-locacao v5, RF-21): colunas, tipos e nulidade, sem CHECK,
    /// FKs NO ACTION para a locacao e o funcionario, indice comum por locacao (sem unico), chave primaria com o nome padrao e o
    /// filtro "Empresa" declarado. Roda sem PostgreSQL: o modelo de design e montado em memoria.
    /// Desvio: a aprovacao do desvio no backend/docs/especificacao/desvios_aprovados.json fica com o orquestrador.
    /// </summary>
    public sealed class ContratoLocacaoSchemaModelTests
    {
        private const string NomeTabela = "contrato_locacao";

        private static IModel BuildModel()
        {
            DbContextOptions<AutoViaDbContext> options = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .Options;

            using AutoViaDbContext context = new AutoViaDbContext(options, ContextoEmpresaDeTeste.Vazio());

            return context.GetService<IDesignTimeModel>().Model;
        }

        [Fact]
        public void ContratoLocacao_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(NomeTabela, null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_contrato_locacao"] = "integer",
                ["id_locacao"] = "integer",
                ["id_funcionario"] = "integer",
                ["documento_json"] = "text",
                ["hash_sha256"] = "character varying(64)",
                ["nome_assinante"] = "character varying(100)",
                ["aceite_condicoes_gerais"] = "boolean",
                ["aceite_politica_combustivel"] = "boolean",
                ["assinatura_png_base64"] = "text",
                ["assinado_em"] = "timestamp without time zone",
                ["created_at"] = "timestamp without time zone",
                ["updated_at"] = "timestamp without time zone",
                ["disabled_at"] = "timestamp without time zone"
            };

            List<string> colunas = entity.GetProperties().Select(x => x.GetColumnName(table)!).OrderBy(x => x).ToList();
            Assert.Equal(tiposEsperados.Keys.OrderBy(x => x).ToList(), colunas);

            List<string> anulaveisEsperadas = new List<string> { "disabled_at" };
            foreach (IProperty propriedade in entity.GetProperties())
            {
                string coluna = propriedade.GetColumnName(table)!;
                Assert.Equal(tiposEsperados[coluna], propriedade.GetColumnType(table));
                Assert.Equal(anulaveisEsperadas.Contains(coluna), propriedade.IsNullable);
            }
        }

        [Fact]
        public void ContratoLocacao_SemCheckNemIndiceUnico()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            Assert.Empty(entity.GetCheckConstraints());
            Assert.DoesNotContain(entity.GetIndexes(), x => x.IsUnique);
        }

        [Fact]
        public void ContratoLocacao_TemDuasFksNoActionParaLocacaoEFuncionario()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            List<IForeignKey> fks = entity.GetForeignKeys().ToList();
            Assert.Equal(2, fks.Count);

            IForeignKey fkLocacao = fks.Single(x => x.PrincipalEntityType.ClrType == typeof(Locacao));
            Assert.Equal("fk_contrato_locacao_locacao", fkLocacao.GetConstraintName());
            Assert.Equal(DeleteBehavior.NoAction, fkLocacao.DeleteBehavior);

            IForeignKey fkFuncionario = fks.Single(x => x.PrincipalEntityType.ClrType == typeof(Funcionario));
            Assert.Equal("fk_contrato_locacao_funcionario", fkFuncionario.GetConstraintName());
            Assert.Equal(DeleteBehavior.NoAction, fkFuncionario.DeleteBehavior);

            Assert.Equal("contrato_locacao_pkey", entity.FindPrimaryKey()!.GetName());
        }

        [Fact]
        public void ContratoLocacao_TemUmIndiceComumPorLocacao()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(NomeTabela, null);

            IIndex indice = Assert.Single(entity.GetIndexes());

            Assert.Equal("idx_contrato_locacao_locacao", indice.GetDatabaseName());
            Assert.False(indice.IsUnique);
            Assert.Equal(new List<string> { "id_locacao" }, indice.Properties.Select(x => x.GetColumnName(table)!).ToList());
        }

        [Fact]
        public void ContratoLocacao_HerdaBaseEntitiesEFiltraPelaEmpresa()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.ClrType == typeof(ContratoLocacao));

            Assert.True(typeof(AutoVia.Core.Entities.BaseEntities).IsAssignableFrom(entity.ClrType));
            Assert.NotNull(entity.FindDeclaredQueryFilter(ConstantsValues.NomeFiltroEmpresa));
        }
    }
}
