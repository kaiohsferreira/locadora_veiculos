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
    /// Predicado real do catalogo de venda (item 7 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026), traduzido
    /// pelo EF sem banco (ToQueryString): so entra veiculo ativo com status DISPONIVEL e com valor de venda preenchido (RF-26).
    /// Vale para a consulta sem paginacao e para a paginada. NAO prova o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class CatalogoPredicadoVendaSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ValorDoStatusDisponivel = "DISPONIVEL";
        private const string PredicadoDoStatus = @"\.status = (?<operando>@\w+|'[^']*')";
        private const string PredicadoDoValorDeVenda = @"\.valor_venda IS NOT NULL";
        private const string InicioDaTabelaVeiculoNoFrom = @"FROM veiculo AS (\w+)";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static VeiculoRepository CriarRepositorioDoCatalogo()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new VeiculoRepository(db, new FakeLogService());
        }

        /// <summary>Valor que o ToQueryString mostra para um parametro (linha "-- @nome='valor'").</summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString: {sql}");

            return valor.Groups[1].Value;
        }

        /// <summary>Valor do status no SQL: literal ou parametro. Falha se o predicado de status nao existir.</summary>
        private static string ValorDoStatusNoSql(string sql)
        {
            Match casamento = Regex.Match(sql, PredicadoDoStatus);

            Assert.True(casamento.Success, "O catalogo deveria filtrar pelo status no SQL: " + sql);

            string operando = casamento.Groups["operando"].Value;

            if (operando.StartsWith("@", StringComparison.Ordinal))
            {
                return ValorDoParametro(sql, operando.Substring(1));
            }

            return operando.Trim('\'');
        }

        /// <summary>
        /// Alias que o EF deu a tabela veiculo no FROM (ex.: "FROM veiculo AS v"). O disabled_at da locadora tambem aparece no SQL
        /// (ApenasEmpresasAtivas), entao o predicado do veiculo tem de ser casado pelo alias dela, nao so pelo texto.
        /// </summary>
        private static string AliasDaTabelaVeiculo(string sql)
        {
            Match alias = Regex.Match(sql, InicioDaTabelaVeiculoNoFrom);

            Assert.True(alias.Success, "O catalogo deveria consultar a tabela veiculo: " + sql);

            return alias.Groups[1].Value;
        }

        /// <summary>Consulta do catalogo (sem paginacao): so DISPONIVEL e com valor de venda, junto com ativo e locadora ativa.</summary>
        [Fact]
        public void ConsultaCatalogo_SoVeiculoDisponivelComValorDeVenda()
        {
            VeiculoRepository repositorio = CriarRepositorioDoCatalogo();

            string sql = repositorio.ConsultaCatalogo(null, null, null, null, null, null).ToQueryString();

            Assert.Equal(ValorDoStatusDisponivel, ValorDoStatusNoSql(sql));
            Assert.Matches(PredicadoDoValorDeVenda, sql);
            Assert.Matches(@"\b" + Regex.Escape(AliasDaTabelaVeiculo(sql)) + @"\.disabled_at IS NULL", sql);
        }

        /// <summary>A pagina do catalogo usa o mesmo predicado: DISPONIVEL e valor de venda preenchido.</summary>
        [Fact]
        public void ConsultaCatalogoPaginada_MesmoPredicadoDoCatalogo()
        {
            VeiculoRepository repositorio = CriarRepositorioDoCatalogo();

            string sql = repositorio.ConsultaCatalogoPaginada(null, null, null, null, null, null, 0, 20).ToQueryString();

            Assert.Equal(ValorDoStatusDisponivel, ValorDoStatusNoSql(sql));
            Assert.Matches(PredicadoDoValorDeVenda, sql);
        }
    }
}
