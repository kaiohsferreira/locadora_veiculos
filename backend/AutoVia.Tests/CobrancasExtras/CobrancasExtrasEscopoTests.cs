using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.CobrancasExtras;

public sealed class CobrancasExtrasEscopoTests
{
    [Theory]
    [InlineData(4242)]
    [InlineData(0)]
    public void LeiturasReutilizadasFiltramEmpresaAtivosESemRastreamento(int idEmpresa)
    {
        ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
        if (idEmpresa > 0) contexto.Definir(idEmpresa, 20, CargoFuncionario.ATENDENTE, false);
        DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste").Options;
        using AutoViaDbContext db = new AutoViaDbContext(opcoes, contexto);
        LocacaoRepository locacoes = new LocacaoRepository(db, new FakeLogService());
        VistoriaRepository entradas = new VistoriaRepository(db, new FakeLogService());
        VistoriaSaidaRepository saidas = new VistoriaSaidaRepository(db, new FakeLogService());
        CobrancasExtrasRepository cobrancas = new CobrancasExtrasRepository(db, contexto, new FakeLogService());
        string[] consultas = new[]
        {
            locacoes.ConsultaLocacaoAtiva(10).ToQueryString(),
            entradas.ConsultaEntradaAtiva(10).ToQueryString(),
            saidas.ConsultaSaidaAtiva(10).ToQueryString(),
            saidas.ConsultaAvariasAtivas(2).ToQueryString(),
            cobrancas.ConsultaLocacao(10).ToQueryString(),
            cobrancas.ConsultaConfiguracoesAtivas().ToQueryString()
        };
        for (int indice = 0; indice < consultas.Length; indice++)
        {
            string sql = consultas[indice];
            Assert.Contains("id_empresa", sql);
            if (idEmpresa == 0 && indice >= 4)
            {
                // A ausência de empresa é avaliada antes da consulta: EF elimina os demais predicados.
                Assert.Contains("WHERE FALSE", sql);
                continue;
            }
            Assert.Contains("disabled_at IS NULL", sql);
            Assert.Contains(string.Concat("ef_filter__IdEmpresaContexto='", idEmpresa, "'"), sql);
            Assert.Contains(idEmpresa == 0 ? "='False'" : "='True'", sql);
        }
        Assert.Contains("tipo = 'ENTRADA'", consultas[1]);
        Assert.Contains("tipo = 'SAIDA'", consultas[2]);
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
