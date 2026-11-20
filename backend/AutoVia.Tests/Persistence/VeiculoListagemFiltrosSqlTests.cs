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
    /// Filtros da listagem de veiculos (VeiculoRepository.ConsultaFrotaPaginada) traduzidos pelo EF sem banco (ToQueryString):
    /// categoria, status e placa, cada um isolado e os tres combinados. Prova: cada filtro so entra no WHERE quando e
    /// informado, com o valor certo (parametro ou literal), e os combinados ficam ligados por AND, sem OR. A placa chega
    /// normalizada e casa por contem (LIKE). NAO prova o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class VeiculoListagemFiltrosSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdCategoriaDoTeste = 7;
        private const StatusVeiculo StatusDoTeste = StatusVeiculo.DISPONIVEL;
        private const string PlacaDoTeste = "ABC1";
        private const string ValorDoStatusNoSql = "DISPONIVEL";
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string PredicadoDaCategoria = @"v\.id_categoria = (?<operando>@\w+|\d+)";
        private const string PredicadoDoStatus = @"v\.status = (?<operando>@\w+|'[^']*')";
        private const string PredicadoDaPlaca = @"v\.placa LIKE (?<operando>@\w+|'[^']*')";
        private const string OperadorOu = " OR ";
        private const string OperadorE = " AND ";
        private const string PalavraWhere = "WHERE";
        private const string PalavraOrdenacao = "ORDER BY";
        private const string SinalDeParametroNoComentario = "@";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeFuncionario()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>
        /// Trecho do WHERE da consulta principal: do primeiro WHERE ate o ORDER BY que vem logo depois dele. A consulta
        /// principal vem antes da subconsulta de fotos, entao o primeiro WHERE e o dos filtros da listagem.
        /// </summary>
        private static string TrechoDoWhere(string sql)
        {
            int inicioDoWhere = sql.IndexOf(PalavraWhere, StringComparison.Ordinal);

            Assert.True(inicioDoWhere >= 0, "A consulta deveria ter WHERE: " + sql);

            int inicioDaOrdenacao = sql.IndexOf(PalavraOrdenacao, inicioDoWhere, StringComparison.Ordinal);

            Assert.True(inicioDaOrdenacao > inicioDoWhere, "A consulta deveria ter ORDER BY depois do WHERE: " + sql);

            return sql.Substring(inicioDoWhere, inicioDaOrdenacao - inicioDoWhere);
        }

        /// <summary>Valor do operando de um predicado: o valor do parametro (linha "-- @nome='valor'") ou o literal do SQL.</summary>
        private static string ValorDoOperando(string sql, string predicado)
        {
            Match casamento = Regex.Match(sql, predicado);

            Assert.True(casamento.Success, "O predicado deveria estar no SQL: " + predicado + "\n" + sql);

            string operando = casamento.Groups["operando"].Value;

            if (operando.StartsWith(SinalDeParametroNoComentario, StringComparison.Ordinal))
            {
                return ValorDoParametro(sql, operando.Substring(SinalDeParametroNoComentario.Length));
            }

            return operando.Trim('\'');
        }

        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString: {sql}");

            return valor.Groups[1].Value;
        }

        [Fact]
        public void SemFiltros_NaoHaPredicadoDeCategoriaStatusNemPlaca()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, null, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.False(Regex.IsMatch(where, PredicadoDaCategoria), where);
            Assert.False(Regex.IsMatch(where, PredicadoDoStatus), where);
            Assert.False(Regex.IsMatch(where, PredicadoDaPlaca), where);
            Assert.Contains(ParametroEmpresa, where);
        }

        [Fact]
        public void SoCategoria_TemSoOPredicadoDaCategoriaComOValorInformado()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(IdCategoriaDoTeste, null, null, null, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.Equal(IdCategoriaDoTeste.ToString(), ValorDoOperando(sql, PredicadoDaCategoria));
            Assert.False(Regex.IsMatch(where, PredicadoDoStatus), where);
            Assert.False(Regex.IsMatch(where, PredicadoDaPlaca), where);
        }

        [Fact]
        public void SoStatus_TemSoOPredicadoDoStatusComOValorInformado()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, StatusDoTeste, null, null, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.Equal(ValorDoStatusNoSql, ValorDoOperando(sql, PredicadoDoStatus));
            Assert.False(Regex.IsMatch(where, PredicadoDaCategoria), where);
            Assert.False(Regex.IsMatch(where, PredicadoDaPlaca), where);
        }

        [Fact]
        public void SoPlaca_TemSoOPredicadoDaPlacaComOTermoEntreCuringas()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, PlacaDoTeste, null, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.Equal("%" + PlacaDoTeste + "%", ValorDoOperando(sql, PredicadoDaPlaca));
            Assert.False(Regex.IsMatch(where, PredicadoDaCategoria), where);
            Assert.False(Regex.IsMatch(where, PredicadoDoStatus), where);
            Assert.DoesNotContain(OperadorOu, where);
        }

        [Fact]
        public void OsTresFiltrosJuntos_TemOsTresPredicadosLigadosPorAndSemOr()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(IdCategoriaDoTeste, StatusDoTeste, PlacaDoTeste, null, 0, 20).ToQueryString();

            string where = TrechoDoWhere(sql);
            Assert.Equal(IdCategoriaDoTeste.ToString(), ValorDoOperando(sql, PredicadoDaCategoria));
            Assert.Equal(ValorDoStatusNoSql, ValorDoOperando(sql, PredicadoDoStatus));
            Assert.Equal("%" + PlacaDoTeste + "%", ValorDoOperando(sql, PredicadoDaPlaca));
            Assert.DoesNotContain(OperadorOu, where);
            Assert.Contains(OperadorE, where);
            Assert.Contains(ParametroEmpresa, where);
        }
    }
}
