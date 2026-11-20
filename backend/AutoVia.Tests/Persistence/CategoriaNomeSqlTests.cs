using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consulta de nome repetido de categoria (CategoriaRepository.ConsultaNomeRepetido) traduzida pelo EF sem banco (ToQueryString).
    /// Prova que a comparacao ignora maiusculas e espacos nas pontas (lower e btrim no SQL, com o nome normalizado no parametro),
    /// que so categoria ativa entra e que a categoria ignorada sai da comparacao. NAO prova o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class CategoriaNomeSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdCategoriaIgnoradaDoTeste = 3;
        private const string NomeComEspacosECaixaDoTeste = "  Sedan ATIVA  ";
        private const string NomeNormalizadoEsperado = "sedan ativa";
        private const string FuncaoLowerNoSql = @"(?i)\bLOWER\(";
        private const string FuncaoTrimNoSql = @"(?i)\bbtrim\(";
        private const string PredicadoDoDisabledAt = @"\.disabled_at IS NULL";
        private const string PredicadoDaCategoriaIgnorada = @"\.id_categoria <> @";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static CategoriaRepository CriarRepositorio()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new CategoriaRepository(db, new FakeLogService());
        }

        /// <summary>O nome com espacos e maiusculas tem de chegar normalizado (minusculo, sem espacos nas pontas) ao parametro do SQL.</summary>
        [Fact]
        public void ConsultaNomeRepetido_ComparaSemCaixaEEspacosNasPontas()
        {
            CategoriaRepository repositorio = CriarRepositorio();

            string sql = repositorio.ConsultaNomeRepetido(NomeComEspacosECaixaDoTeste, null).ToQueryString();

            Assert.Matches(FuncaoLowerNoSql, sql);
            Assert.Matches(FuncaoTrimNoSql, sql);
            Assert.Contains("='" + NomeNormalizadoEsperado + "'", sql);
            Assert.DoesNotContain(NomeComEspacosECaixaDoTeste, sql);
        }

        /// <summary>So categoria ativa entra na comparacao, e a categoria ignorada (a propria, na edicao) sai dela.</summary>
        [Fact]
        public void ConsultaNomeRepetido_SoAtivasESemAIgnorada()
        {
            CategoriaRepository repositorio = CriarRepositorio();

            string sql = repositorio.ConsultaNomeRepetido(NomeComEspacosECaixaDoTeste, IdCategoriaIgnoradaDoTeste).ToQueryString();

            Assert.Matches(PredicadoDoDisabledAt, sql);
            Assert.Matches(PredicadoDaCategoriaIgnorada, sql);
        }
    }
}
