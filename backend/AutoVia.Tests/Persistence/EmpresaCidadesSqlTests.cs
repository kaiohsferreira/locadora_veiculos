using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace AutoVia.Tests.Persistence
{
    public class EmpresaCidadesSqlTests
    {
        private readonly ITestOutputHelper _saida;

        public EmpresaCidadesSqlTests(ITestOutputHelper saida)
        {
            _saida = saida;
        }

        /// <summary>Texto exato que o EF gera para Where(!string.IsNullOrWhiteSpace(x.Cidade)) com Trim: cidade aparada diferente de vazio.</summary>
        private const string PredicadoCidadeNaoVazia = @"btrim(e.cidade, E' \t\n\r') <> ''";

        [Fact]
        public void ConsultaCidades_ProjetaSomenteCidadeEUfComDistinctEOrdenacao()
        {
            DbContextOptions<AutoViaDbContext> opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
                .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
                .Options;
            FakeLogService log = new FakeLogService();
            ContextoEmpresa contexto = new ContextoEmpresa(log);
            using AutoViaDbContext db = new AutoViaDbContext(opcoes, contexto);
            EmpresaRepository repositorio = new EmpresaRepository(db, log);

            string sqlAnonimo = repositorio.ConsultaCidadesAtivas().ToQueryString();
            _saida.WriteLine(sqlAnonimo);
            contexto.Definir(42, 1, CargoFuncionario.ATENDENTE, false);
            string sqlFuncionario = repositorio.ConsultaCidadesAtivas().ToQueryString();

            Assert.Equal(sqlAnonimo, sqlFuncionario);
            Assert.Contains("SELECT DISTINCT", sqlAnonimo);
            Assert.Contains("FROM empresa", sqlAnonimo);
            Assert.Matches(@"\b\w+\.ativo\s+AND\s+\w+\.disabled_at IS NULL", sqlAnonimo);
            Assert.True(sqlAnonimo.Contains(PredicadoCidadeNaoVazia, StringComparison.Ordinal),
                $"O predicado de cidade nao vazia (EmpresaRepository.cs, ConsultaCidadesAtivas) nao esta no SQL real:{Environment.NewLine}{sqlAnonimo}");
            Assert.Matches(@"(upper\(\w+\.uf\)|\w+\.\x22?[Uu]f\x22?)\s*=\s*ANY", sqlAnonimo);
            Assert.Matches(@"ORDER BY [^\r\n]+[Cc]idade\x22?, [^\r\n]+[Uu]f\x22?", sqlAnonimo);
            Assert.DoesNotMatch(new Regex(@"\b(id_empresa|nome_fantasia|razao_social|cnpj|email|logradouro|telefone|veiculo)\b"), sqlAnonimo);
            Assert.DoesNotContain("ef_filter__IdEmpresaContexto", sqlAnonimo);
        }
    }
}
