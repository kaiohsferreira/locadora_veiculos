using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// SQL das leituras publicas de veiculo entre locadoras (busca de disponiveis e catalogo de seminovos), traduzido
    /// pelo EF sem banco (ToQueryString). Prova: (1) nenhum dos dois metodos nomeados carrega o parametro do filtro
    /// "Empresa", em contexto vazio, de cliente e de funcionario, inclusive dentro da subconsulta de reservas (EXISTS);
    /// (2) a locadora so entra quando ativa e nao desativada: um JOIN empresa cujo alias tem ativo e disabled_at IS NULL
    /// no WHERE; (3) a paginacao traz as colunas da locadora (Include(Empresa)) e a categoria, e as consultas sem
    /// paginacao nao as trazem; (4) a marca do catalogo chega normalizada ao SQL, mesmo sem vir normalizada do chamador.
    /// NAO prova o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class VeiculoPublicoSqlTests
    {
        private const int IdEmpresaDoTeste = 4242;
        private const int IdClienteDoTeste = 5;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string Exists = "EXISTS";
        private const string JoinCategoria = "JOIN categoria";
        private const string PalavraWhere = "WHERE";
        private const string PalavraFrom = "FROM";
        private const string Limit = "LIMIT";
        private const string Offset = "OFFSET";
        private const string OrderBy = "ORDER BY";
        private const string ColunaAtivo = "ativo";
        private const string ColunaDisabledAt = "disabled_at";
        private const string ColunaNomeFantasia = "nome_fantasia";
        private const string ColunaCidade = "cidade";
        private const string ColunaUf = "uf";
        private const string MarcaNormalizadaDoTeste = "FIAT";
        private const string MarcaComEspacosDoTeste = "  fiat  ";
        private const string MarcaCrua = "fiat";

        private static readonly DateOnly RetiradaDoTeste = new DateOnly(2026, 12, 10);
        private static readonly DateOnly DevolucaoDoTeste = new DateOnly(2026, 12, 12);

        /// <summary>Alias da tabela empresa no JOIN gerado pelo EF (ex.: "JOIN empresa AS e ON").</summary>
        private static readonly Regex JoinDaEmpresa = new Regex(@"JOIN empresa AS (\w+) ON", RegexOptions.CultureInvariant);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbComContextoDeCliente(int idCliente)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.DefinirCliente(idCliente);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>Os tres contextos que a leitura publica pode ter: vazio (anonimo), de cliente e de funcionario da locadora A.</summary>
        private static List<AutoViaDbContext> CriarTresContextos()
        {
            return new List<AutoViaDbContext>
            {
                CriarDbSemContexto(),
                CriarDbComContextoDeCliente(IdClienteDoTeste),
                CriarDbComContextoDeFuncionario(IdEmpresaDoTeste)
            };
        }

        /// <summary>Trecho do SQL a partir da palavra-chave (WHERE, FROM...), ou vazio se ela nao existe.</summary>
        private static string TrechoAPartirDe(string sql, string palavra)
        {
            Match casamento = Regex.Match(sql, @"\b" + palavra + @"\b");

            return casamento.Success ? sql.Substring(casamento.Index) : string.Empty;
        }

        /// <summary>Lista de colunas do SELECT, antes do FROM.</summary>
        private static string SelectDoSql(string sql)
        {
            Match casamento = Regex.Match(sql, @"\b" + PalavraFrom + @"\b");

            Assert.True(casamento.Success, $"A consulta deveria ter FROM. SQL: {sql}");

            return sql.Substring(0, casamento.Index);
        }

        /// <summary>Confere se o WHERE tem o predicado da locadora (ativo e disabled_at IS NULL) sobre o alias informado.</summary>
        private static bool PredicadoDaEmpresaNoWhere(string aposWhere, string alias)
        {
            string aliasEscapado = Regex.Escape(alias);
            bool ativa = Regex.IsMatch(aposWhere, @"\b" + aliasEscapado + @"\." + Regex.Escape(ColunaAtivo) + @"\b");
            bool naoDesativada = Regex.IsMatch(aposWhere, @"\b" + aliasEscapado + @"\." + Regex.Escape(ColunaDisabledAt) + @" IS NULL\b");

            return ativa && naoDesativada;
        }

        /// <summary>
        /// Prova a locadora ativa e nao desativada: existe um JOIN empresa cujo alias aparece no WHERE com ativo e com
        /// disabled_at IS NULL. Os predicados do proprio veiculo usam o alias v e nao contam.
        /// </summary>
        private static void AssertLocadoraAtivaNoWhere(string sql)
        {
            string aposWhere = TrechoAPartirDe(sql, PalavraWhere);
            MatchCollection joins = JoinDaEmpresa.Matches(sql);
            bool provado = false;

            Assert.True(joins.Count > 0, $"A consulta deveria ter JOIN empresa. SQL: {sql}");

            foreach (Match join in joins)
            {
                provado = provado || PredicadoDaEmpresaNoWhere(aposWhere, join.Groups[1].Value);
            }

            Assert.True(provado, $"O WHERE deveria filtrar a empresa por ativo e disabled_at IS NULL. SQL: {sql}");
        }

        /// <summary>A paginada traz as colunas da locadora no SELECT (nome fantasia, cidade e UF), ou seja, Include(Empresa).</summary>
        private static void AssertColunasDaLocadoraNoSelect(string sql)
        {
            string select = SelectDoSql(sql);

            Assert.True(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaNomeFantasia) + @"\b"), $"O SELECT deveria trazer nome_fantasia da empresa. SQL: {sql}");
            Assert.True(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaCidade) + @"\b"), $"O SELECT deveria trazer cidade da empresa. SQL: {sql}");
            Assert.True(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaUf) + @"\b"), $"O SELECT deveria trazer uf da empresa. SQL: {sql}");
        }

        /// <summary>A consulta sem paginacao nao carrega a locadora: nenhuma coluna da empresa no SELECT.</summary>
        private static void AssertSemColunasDaLocadoraNoSelect(string sql)
        {
            string select = SelectDoSql(sql);

            Assert.False(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaNomeFantasia) + @"\b"), $"O SELECT nao deveria trazer nome_fantasia. SQL: {sql}");
            Assert.False(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaCidade) + @"\b"), $"O SELECT nao deveria trazer cidade. SQL: {sql}");
            Assert.False(Regex.IsMatch(select, @"\." + Regex.Escape(ColunaUf) + @"\b"), $"O SELECT nao deveria trazer uf. SQL: {sql}");
        }

        [Fact]
        public void ConsultaDisponiveis_NaoCarregaFiltroDeEmpresa_NemNaSubconsultaDeReservas()
        {
            foreach (AutoViaDbContext db in CriarTresContextos())
            {
                using AutoViaDbContext contextoDoTeste = db;
                VeiculoRepository repositorio = new VeiculoRepository(contextoDoTeste, new FakeLogService());

                string sql = repositorio.ConsultaDisponiveis(RetiradaDoTeste, DevolucaoDoTeste, null, null, null).ToQueryString();

                Assert.Contains(Exists, sql);
                Assert.DoesNotContain(ParametroEmpresa, sql);
            }
        }

        [Fact]
        public void ConsultaDisponiveisPaginada_NaoCarregaFiltroDeEmpresa_EmTodosOsContextos()
        {
            foreach (AutoViaDbContext db in CriarTresContextos())
            {
                using AutoViaDbContext contextoDoTeste = db;
                VeiculoRepository repositorio = new VeiculoRepository(contextoDoTeste, new FakeLogService());

                string sql = repositorio.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, 1, 10m, 300m, 0, 20).ToQueryString();

                Assert.Contains(Exists, sql);
                Assert.DoesNotContain(ParametroEmpresa, sql);
            }
        }

        [Fact]
        public void ConsultaDisponiveis_SoLocadorasAtivasENaoDesativadas_ESemDadosDaLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(IdEmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDisponiveis(RetiradaDoTeste, DevolucaoDoTeste, null, null, null).ToQueryString();

            AssertLocadoraAtivaNoWhere(sql);
            AssertSemColunasDaLocadoraNoSelect(sql);
        }

        [Fact]
        public void ConsultaDisponiveisPaginada_TrazCategoriaELocadoraOrdenadaEPaginada()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, null, null, null, 20, 10).ToQueryString();

            Assert.Contains(JoinCategoria, sql);
            AssertLocadoraAtivaNoWhere(sql);
            AssertColunasDaLocadoraNoSelect(sql);
            Assert.Contains(OrderBy, sql);
            Assert.Contains(Limit, sql);
            Assert.Contains(Offset, sql);
        }

        [Fact]
        public void ConsultaCatalogo_NaoCarregaFiltroDeEmpresa_EmTodosOsContextos()
        {
            foreach (AutoViaDbContext db in CriarTresContextos())
            {
                using AutoViaDbContext contextoDoTeste = db;
                VeiculoRepository repositorio = new VeiculoRepository(contextoDoTeste, new FakeLogService());

                string sql = repositorio.ConsultaCatalogo(null, null, null, null, null, null).ToQueryString();

                Assert.DoesNotContain(ParametroEmpresa, sql);
            }
        }

        [Fact]
        public void ConsultaCatalogo_SoLocadorasAtivasENaoDesativadas_ESemDadosDaLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(IdEmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCatalogo(null, null, null, null, null, null).ToQueryString();

            AssertLocadoraAtivaNoWhere(sql);
            AssertSemColunasDaLocadoraNoSelect(sql);
        }

        [Fact]
        public void ConsultaCatalogoPaginada_TrazCategoriaELocadoraOrdenadaEPaginada()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCatalogoPaginada(null, null, null, null, null, null, 0, 20).ToQueryString();

            Assert.Contains(JoinCategoria, sql);
            AssertLocadoraAtivaNoWhere(sql);
            AssertColunasDaLocadoraNoSelect(sql);
            Assert.Contains(OrderBy, sql);
            Assert.Contains(Limit, sql);
            Assert.Contains(Offset, sql);
        }

        [Fact]
        public void ConsultaCatalogo_NormalizaAMarcaMesmoQuandoOChamadorNaoNormaliza()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCatalogo(MarcaComEspacosDoTeste, null, null, null, null, null).ToQueryString();

            Assert.Contains(MarcaNormalizadaDoTeste, sql);
            Assert.DoesNotContain(MarcaCrua, sql);
            Assert.DoesNotContain("  FIAT", sql);
        }

    }
}
