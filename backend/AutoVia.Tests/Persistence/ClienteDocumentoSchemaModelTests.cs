using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Modelo do EF da tabela cliente_documento (desvio aprovado por Kaio em 19/11/2026): colunas, tipos e nulidade, CHECK do
    /// tipo, FK NO ACTION para o cliente, indice comum (sem unico) e a chave primaria com o nome padrao. Roda sem PostgreSQL:
    /// o modelo de design e montado em memoria.
    /// </summary>
    public sealed class ClienteDocumentoSchemaModelTests
    {
        private const string NomeTabela = "cliente_documento";

        private static IModel BuildModel()
        {
            DbContextOptions<AutoViaDbContext> options = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .Options;

            using AutoViaDbContext context = new AutoViaDbContext(options, ContextoEmpresaDeTeste.Vazio());

            return context.GetService<IDesignTimeModel>().Model;
        }

        [Fact]
        public void ClienteDocumento_TemAsColunasTiposENulabilidadeDoDesvio()
        {
            IModel model = BuildModel();
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(NomeTabela, null);
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            Dictionary<string, string> tiposEsperados = new Dictionary<string, string>
            {
                ["id_cliente_documento"] = "integer",
                ["id_cliente"] = "integer",
                ["tipo"] = "character varying(30)",
                ["chave_objeto"] = "character varying(200)",
                ["nome_arquivo"] = "character varying(120)",
                ["tipo_conteudo"] = "character varying(50)",
                ["tamanho_bytes"] = "integer",
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
        public void ClienteDocumento_TipoEhGravadoComoTextoComOCheckDosDoisTipos()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            IProperty tipo = entity.FindProperty(nameof(ClienteDocumento.Tipo))!;
            Assert.Equal(typeof(string), tipo.GetProviderClrType());
            Assert.Equal(30, tipo.GetMaxLength());

            ICheckConstraint check = Assert.Single(entity.GetCheckConstraints());
            Assert.Equal("ck_cliente_documento_tipo", check.Name);
            Assert.Equal("tipo IN ('CNH', 'COMPROVANTE_RESIDENCIA')", check.Sql);
        }

        [Fact]
        public void ClienteDocumento_TemUmaFkNoActionParaClienteEChavePrimariaComNomePadrao()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);

            IForeignKey fk = Assert.Single(entity.GetForeignKeys());
            Assert.Equal("fk_cliente_documento_cliente", fk.GetConstraintName());
            Assert.Equal(typeof(Cliente), fk.PrincipalEntityType.ClrType);
            Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
            Assert.Equal("cliente_documento_pkey", entity.FindPrimaryKey()!.GetName());
        }

        [Fact]
        public void ClienteDocumento_TemUmIndiceComumPorClienteETipoENenhumUnico()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.GetTableName() == NomeTabela);
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(NomeTabela, null);

            IIndex indice = Assert.Single(entity.GetIndexes());

            Assert.Equal("idx_cliente_documento_cliente_tipo", indice.GetDatabaseName());
            Assert.False(indice.IsUnique);
            Assert.Equal(new List<string> { "id_cliente", "tipo" }, indice.Properties.Select(x => x.GetColumnName(table)!).ToList());
        }

        [Fact]
        public void ClienteDocumento_HerdaBaseEntitiesEFicaNaListaDeGlobaisSemFiltroDeEmpresa()
        {
            IModel model = BuildModel();
            IEntityType entity = model.GetEntityTypes().Single(x => x.ClrType == typeof(ClienteDocumento));

            Assert.True(typeof(AutoVia.Core.Entities.BaseEntities).IsAssignableFrom(entity.ClrType));
            Assert.Null(entity.FindDeclaredQueryFilter(AutoVia.Helpers.ConstantsValues.NomeFiltroEmpresa));
        }
    }
}
