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
    /// paginacao nao as trazem; (4) a marca do catalogo chega normalizada ao SQL, mesmo sem vir normalizada do chamador;
    /// (5) a foto de capa e lida so na subconsulta de veiculo_foto: ativa (disabled_at IS NULL), ordenada por ordem e
    /// id da foto, e uma por veiculo (row &lt;= 1).
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
        private const string TabelaDeFotos = "veiculo_foto";
        private const string FromDaTabelaDeFotos = "FROM veiculo_foto";
        private const string ParticaoDaPrimeiraFoto = "ROW_NUMBER() OVER(PARTITION BY";
        private const string OrdemDaPrimeiraFotoNaJanela = @"ROW_NUMBER\(\) OVER\(PARTITION BY \w+\.id_veiculo ORDER BY \w+\.ordem, \w+\.id_veiculo_foto\)";
        private const string LimiteDaPrimeiraFoto = "row <= 1";
        private const string JuncaoEsquerdaDasFotos = "LEFT JOIN";

        private static readonly DateOnly RetiradaDoTeste = new DateOnly(2026, 12, 10);
        private static readonly DateOnly DevolucaoDoTeste = new DateOnly(2026, 12, 12);

        /// <summary>Alias da tabela empresa no JOIN gerado pelo EF (ex.: "JOIN empresa AS e ON").</summary>
        private static readonly Regex JoinDaEmpresa = new Regex(@"JOIN empresa AS (\w+) ON", RegexOptions.CultureInvariant);

        /// <summary>Alias da tabela veiculo_foto na subconsulta da capa (ex.: "FROM veiculo_foto AS v0").</summary>
        private static readonly Regex AliasDaTabelaDeFotos = new Regex(@"FROM veiculo_foto AS (\w+)", RegexOptions.CultureInvariant);

        /// <summary>Alias da tabela veiculo na consulta principal (ex.: "FROM veiculo AS v"), sem casar com veiculo_foto.</summary>
        private static readonly Regex AliasDaTabelaDeVeiculo = new Regex(@"FROM veiculo AS (\w+)", RegexOptions.CultureInvariant);

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
        /// Prova o veiculo ativo (nao desativado): o WHERE da consulta principal filtra disabled_at IS NULL do alias da
        /// tabela veiculo. O alias da subconsulta de fotos (v0) nao conta: o predicado tem de estar no alias do veiculo.
        /// </summary>
        private static void AssertVeiculoAtivoNoWhere(string sql)
        {
            Match tabela = AliasDaTabelaDeVeiculo.Match(sql);

            Assert.True(tabela.Success, $"A consulta deveria ler a tabela veiculo com alias. SQL: {sql}");

            string aposWhere = TrechoAPartirDe(sql, PalavraWhere);
            string alias = Regex.Escape(tabela.Groups[1].Value);

            Assert.True(
                Regex.IsMatch(aposWhere, @"\b" + alias + @"\." + Regex.Escape(ColunaDisabledAt) + @" IS NULL\b"),
                $"O WHERE deveria filtrar o veiculo nao desativado (disabled_at IS NULL). SQL: {sql}");
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

        /// <summary>
        /// Subconsulta da capa: do parentese que abre a subconsulta do LEFT JOIN (o SELECT das fotos, com a janela
        /// ROW_NUMBER, e o FROM veiculo_foto) ate o parentese que a fecha. Fica de fora o WHERE do veiculo e da locadora
        /// (antes do LEFT JOIN) e o WHERE final da consulta, se houver (depois dele).
        /// </summary>
        private static string TrechoDaSubconsultaDeFotos(string sql)
        {
            int inicioDaJuncao = sql.IndexOf(JuncaoEsquerdaDasFotos, StringComparison.Ordinal);

            Assert.True(inicioDaJuncao >= 0, $"A consulta deveria ter o LEFT JOIN das fotos. SQL: {sql}");

            int aberturaDaSubconsulta = sql.IndexOf('(', inicioDaJuncao);
            int fechamentoDaSubconsulta = PosicaoDoParentesePorFechar(sql, aberturaDaSubconsulta);
            string subconsulta = sql.Substring(aberturaDaSubconsulta, fechamentoDaSubconsulta - aberturaDaSubconsulta + 1);

            Assert.Contains(FromDaTabelaDeFotos, subconsulta);

            return subconsulta;
        }

        /// <summary>Posicao do parentese que fecha o parentese aberto na posicao "abertura" (conta a profundidade).</summary>
        private static int PosicaoDoParentesePorFechar(string sql, int abertura)
        {
            int profundidade = 0;
            int posicao = -1;

            for (int indice = abertura; indice < sql.Length && posicao < 0; indice++)
            {
                if (sql[indice] == '(')
                {
                    profundidade++;
                }
                else if (sql[indice] == ')')
                {
                    profundidade--;

                    if (profundidade == 0)
                    {
                        posicao = indice;
                    }
                }
            }

            Assert.True(posicao > abertura, $"Parenteses da subconsulta de fotos sem fechamento. SQL: {sql}");

            return posicao;
        }

        /// <summary>Prova que a subconsulta de fotos so traz fotos ativas: o alias de veiculo_foto tem disabled_at IS NULL dentro dela.</summary>
        private static void AssertFotoAtivaNaSubconsulta(string subconsulta)
        {
            Match tabela = AliasDaTabelaDeFotos.Match(subconsulta);

            Assert.True(tabela.Success, $"A subconsulta deveria ler veiculo_foto com alias. Trecho: {subconsulta}");
            Assert.True(
                Regex.IsMatch(subconsulta, @"\b" + Regex.Escape(tabela.Groups[1].Value) + @"\." + Regex.Escape(ColunaDisabledAt) + @" IS NULL\b"),
                $"A foto de capa deveria filtrar disabled_at IS NULL dentro da subconsulta de fotos. Trecho: {subconsulta}");
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

            AssertVeiculoAtivoNoWhere(sql);
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
            AssertVeiculoAtivoNoWhere(sql);
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

            AssertVeiculoAtivoNoWhere(sql);
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
            AssertVeiculoAtivoNoWhere(sql);
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

        [Fact]
        public void ConsultaDisponiveisPaginada_TrazAFotoDeCapa_SemParametroDeEmpresa_EmTodosOsContextos()
        {
            foreach (AutoViaDbContext db in CriarTresContextos())
            {
                using AutoViaDbContext contextoDoTeste = db;
                VeiculoRepository repositorio = new VeiculoRepository(contextoDoTeste, new FakeLogService());

                string sql = repositorio.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, null, null, null, 0, 20).ToQueryString();

                Assert.Contains(TabelaDeFotos, sql);
                Assert.DoesNotContain(ParametroEmpresa, sql);
            }
        }

        [Fact]
        public void ConsultaCatalogoPaginada_TrazAFotoDeCapa_SemParametroDeEmpresa_EmTodosOsContextos()
        {
            foreach (AutoViaDbContext db in CriarTresContextos())
            {
                using AutoViaDbContext contextoDoTeste = db;
                VeiculoRepository repositorio = new VeiculoRepository(contextoDoTeste, new FakeLogService());

                string sql = repositorio.ConsultaCatalogoPaginada(null, null, null, null, null, null, 0, 20).ToQueryString();

                Assert.Contains(TabelaDeFotos, sql);
                Assert.DoesNotContain(ParametroEmpresa, sql);
            }
        }

        [Fact]
        public void ConsultaDisponiveisPaginada_AFotoDeCapaEAtivaOrdenadaELimitadaAUmaPorVeiculo()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, null, null, null, 0, 20).ToQueryString();

            AssertFotoDeCapaEAtivaOrdenadaELimitadaAUmaPorVeiculo(sql);
        }

        [Fact]
        public void ConsultaCatalogoPaginada_AFotoDeCapaEAtivaOrdenadaELimitadaAUmaPorVeiculo()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCatalogoPaginada(null, null, null, null, null, null, 0, 20).ToQueryString();

            AssertFotoDeCapaEAtivaOrdenadaELimitadaAUmaPorVeiculo(sql);
        }

        /// <summary>
        /// Prova a foto de capa na subconsulta de veiculo_foto: foto ativa (disabled_at IS NULL do alias da foto), janela
        /// ordenada por ordem e id da foto dentro de cada veiculo, e filtro row &lt;= 1 (uma foto por veiculo).
        /// </summary>
        private static void AssertFotoDeCapaEAtivaOrdenadaELimitadaAUmaPorVeiculo(string sql)
        {
            string subconsulta = TrechoDaSubconsultaDeFotos(sql);

            Assert.Contains(TabelaDeFotos, subconsulta);
            AssertFotoAtivaNaSubconsulta(subconsulta);
            Assert.Contains(ParticaoDaPrimeiraFoto, subconsulta);
            Assert.Matches(OrdemDaPrimeiraFotoNaJanela, subconsulta);
            Assert.Contains(LimiteDaPrimeiraFoto, subconsulta);
        }

        [Fact]
        public void ConsultasPaginadasComCapa_ContinuamExigindoLocadoraAtivaENaoDesativada()
        {
            using AutoViaDbContext dbDisponiveis = CriarDbComContextoDeFuncionario(IdEmpresaDoTeste);
            using AutoViaDbContext dbCatalogo = CriarDbComContextoDeFuncionario(IdEmpresaDoTeste);
            VeiculoRepository repositorioDisponiveis = new VeiculoRepository(dbDisponiveis, new FakeLogService());
            VeiculoRepository repositorioCatalogo = new VeiculoRepository(dbCatalogo, new FakeLogService());

            string sqlDisponiveis = repositorioDisponiveis.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, null, null, null, 0, 20).ToQueryString();
            string sqlCatalogo = repositorioCatalogo.ConsultaCatalogoPaginada(null, null, null, null, null, null, 0, 20).ToQueryString();

            AssertVeiculoAtivoNoWhere(sqlDisponiveis);
            AssertVeiculoAtivoNoWhere(sqlCatalogo);
            AssertLocadoraAtivaNoWhere(sqlDisponiveis);
            AssertLocadoraAtivaNoWhere(sqlCatalogo);
        }

        [Fact]
        public void ConsultasPaginadasComCapa_PaginamAntesDoJoinDasFotos()
        {
            using AutoViaDbContext dbDisponiveis = CriarDbSemContexto();
            using AutoViaDbContext dbCatalogo = CriarDbSemContexto();
            VeiculoRepository repositorioDisponiveis = new VeiculoRepository(dbDisponiveis, new FakeLogService());
            VeiculoRepository repositorioCatalogo = new VeiculoRepository(dbCatalogo, new FakeLogService());

            string sqlDisponiveis = repositorioDisponiveis.ConsultaDisponiveisPaginada(RetiradaDoTeste, DevolucaoDoTeste, null, null, null, 20, 10).ToQueryString();
            string sqlCatalogo = repositorioCatalogo.ConsultaCatalogoPaginada(null, null, null, null, null, null, 20, 10).ToQueryString();

            Assert.True(sqlDisponiveis.IndexOf(Limit, StringComparison.Ordinal) < sqlDisponiveis.IndexOf(JuncaoEsquerdaDasFotos, StringComparison.Ordinal), $"A paginacao precisa vir antes do join das fotos. SQL: {sqlDisponiveis}");
            Assert.True(sqlCatalogo.IndexOf(Limit, StringComparison.Ordinal) < sqlCatalogo.IndexOf(JuncaoEsquerdaDasFotos, StringComparison.Ordinal), $"A paginacao precisa vir antes do join das fotos. SQL: {sqlCatalogo}");
        }

        [Fact]
        public void ConsultasSemPaginacao_NaoCarregamAFotoDeCapa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sqlDisponiveis = repositorio.ConsultaDisponiveis(RetiradaDoTeste, DevolucaoDoTeste, null, null, null).ToQueryString();
            string sqlCatalogo = repositorio.ConsultaCatalogo(null, null, null, null, null, null).ToQueryString();

            Assert.DoesNotContain(TabelaDeFotos, sqlDisponiveis);
            Assert.DoesNotContain(TabelaDeFotos, sqlCatalogo);
        }
    }
}
