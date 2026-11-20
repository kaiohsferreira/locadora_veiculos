using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Infrastructure.Migrations;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AutoVia.Tests.Vistorias;

public sealed class VistoriaSaidaSqlModelTests
{
    private static AutoViaDbContext CriarContexto(int idEmpresa)
    {
        ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
        if (idEmpresa > 0) contexto.Definir(idEmpresa, 20, CargoFuncionario.ATENDENTE, false);
        DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste").Options;
        return new AutoViaDbContext(opcoes, contexto);
    }

    [Theory]
    [InlineData(4242, "True")]
    [InlineData(0, "False")]
    public void ConsultasDeProducaoPreservamEmpresaAtivosETipo(int idEmpresa, string guarda)
    {
        using AutoViaDbContext db = CriarContexto(idEmpresa);
        VistoriaSaidaRepository repositorio = new VistoriaSaidaRepository(db, new FakeLogService());
        string saida = repositorio.ConsultaSaidaAtiva(77).ToQueryString();
        string avarias = repositorio.ConsultaAvariasAtivas(88).ToQueryString();
        foreach (string sql in new[] { saida, avarias })
        {
            Assert.Contains($"ef_filter__IdEmpresaContexto='{idEmpresa}'", sql);
            Assert.Contains($"='{guarda}'", sql);
            Assert.Contains("id_empresa", sql);
            Assert.Contains("disabled_at IS NULL", sql);
        }
        Assert.Contains("tipo = 'SAIDA'", saida);
        Assert.Contains("='77'", saida);
        Assert.Contains("='88'", avarias);
        Assert.Contains("id_vistoria = @", avarias);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public void ModeloMantemLegadoIndeterminadoEVinculoOpcionalSemUnicidade()
    {
        using AutoViaDbContext db = CriarContexto(1);
        IModel model = db.GetService<IDesignTimeModel>().Model;
        IEntityType avaria = model.FindEntityType(typeof(Avaria))!;
        IProperty origem = avaria.FindProperty(nameof(Avaria.IdAvariaEntrada))!;
        Assert.True(origem.IsNullable);
        Assert.Equal("id_avaria_entrada", origem.GetColumnName());
        IForeignKey fk = Assert.Single(avaria.GetForeignKeys(), x => x.Properties.Contains(origem));
        Assert.Equal(avaria, fk.PrincipalEntityType);
        Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior);
        Assert.Equal("fk_avaria_entrada", fk.GetConstraintName());
        IIndex indice = Assert.Single(avaria.GetIndexes(), x => x.Properties.Contains(origem));
        Assert.Equal("idx_avaria_entrada", indice.GetDatabaseName());
        Assert.False(indice.IsUnique);
        IProperty comparacao = model.FindEntityType(typeof(Vistoria))!.FindProperty(nameof(Vistoria.ComparacaoRealizada))!;
        Assert.False(comparacao.IsNullable);
        Assert.Equal(false, comparacao.GetDefaultValue());
        Assert.Equal("comparacao_realizada", comparacao.GetColumnName());
    }

    [Fact]
    public void MigrationSomenteAcrescentaDuasColunasIndiceComumEVinculo()
    {
        VincularAvariasVistoriaSaida migration = new VincularAvariasVistoriaSaida();
        Assert.Equal(4, migration.UpOperations.Count);
        AddColumnOperation[] colunas = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        Assert.Equal(2, colunas.Length);
        AddColumnOperation marcador = Assert.Single(colunas, x => x.Table == "vistoria");
        Assert.Equal("comparacao_realizada", marcador.Name);
        Assert.Equal(false, marcador.DefaultValue);
        Assert.False(marcador.IsNullable);
        AddColumnOperation origem = Assert.Single(colunas, x => x.Table == "avaria");
        Assert.Equal("id_avaria_entrada", origem.Name);
        Assert.True(origem.IsNullable);
        Assert.False(Assert.Single(migration.UpOperations.OfType<CreateIndexOperation>()).IsUnique);
        Assert.Single(migration.UpOperations.OfType<AddForeignKeyOperation>());
    }
}
