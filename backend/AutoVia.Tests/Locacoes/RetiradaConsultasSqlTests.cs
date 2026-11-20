using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Valida a traducao SQL da consulta real sem abrir conexao PostgreSQL.</summary>
    public class RetiradaConsultasSqlTests
    {
        private const string Conexao = "Host=localhost;Database=autovia_modelo_teste";

        [Theory]
        [InlineData(1)]
        [InlineData(4242)]
        public void ConsultaRetiradaPreservaEmpresaAtivosEIdentificador(int empresa)
        {
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>().UseNpgsql(Conexao).Options;
            using AutoViaDbContext db = new AutoViaDbContext(opcoes, ContextoEmpresaDeTeste.Criar(empresa));
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());
            IQueryable<Reserva> consulta = repositorio.ConsultaReservaParaRetirada(77);
            string sql = consulta.ToQueryString();
            Assert.Contains($"ef_filter__IdEmpresaContexto='{empresa}'", sql);
            Assert.Contains("='True'", sql);
            Assert.Contains("id_reserva = @", sql);
            Assert.Contains("='77'", sql);
            Assert.Contains("cliente_locadora", sql);
            Assert.Contains("locacao", sql);
            Assert.Contains("id_empresa", sql);
            Assert.Contains("disabled_at IS NULL", sql);
            Assert.Matches(@"c\d*\.disabled_at IS NULL", sql);
            Assert.Matches(@"v\d*\.disabled_at IS NULL", sql);
            Assert.Matches(@"l\d*\.disabled_at IS NULL", sql);
            Assert.Contains("AsNoTracking", consulta.Expression.ToString());
            Assert.Empty(db.ChangeTracker.Entries());
        }

        [Fact]
        public void SemEmpresaConsultaFalhaFechada()
        {
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>().UseNpgsql(Conexao).Options;
            using AutoViaDbContext db = new AutoViaDbContext(opcoes, ContextoEmpresaDeTeste.Vazio());
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());
            string sql = repositorio.ConsultaReservaParaRetirada(77).ToQueryString();
            Assert.Contains("ef_filter__IdEmpresaContexto='0'", sql);
            Assert.Contains("='False'", sql);
        }
    }
}