using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Predicado de sobreposicao de periodo traduzido pelo EF sem banco (ToQueryString), com as datas do teste. Prova
    /// (1) o conflito de criacao de reserva (ConsultaReservasDoVeiculo com RegrasReserva.SobrepoePeriodo): a retirada da
    /// reserva existente e menor que a devolucao pedida, e a devolucao da existente e maior que a retirada pedida, com
    /// comparacao estrita; (2) o mesmo predicado na busca publica de disponiveis (ConsultaDisponiveis), dentro da
    /// subconsulta de reservas. Os valores dos parametros tem de ser os das datas pedidas, na ordem certa.
    /// NAO prova o resultado no PostgreSQL: so a forma do SQL e os valores enviados.
    /// </summary>
    public class ReservaSobreposicaoSqlTests
    {
        private const int IdVeiculoDoTeste = 8;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int DiaDaRetiradaPedida = 10;
        private const int DiaDaDevolucaoPedida = 12;
        private const int AnoDoTeste = 2026;
        private const int MesDoTeste = 12;
        private const string DataDaRetiradaPedidaNoSql = "2026-12-10";
        private const string DataDaDevolucaoPedidaNoSql = "2026-12-12";
        private const string PredicadoRetiradaDaReservaMenorQueDevolucaoPedida = @"\b\w+\.data_retirada_prevista < (?<operando>@\w+)";
        private const string PredicadoDevolucaoDaReservaMaiorQueRetiradaPedida = @"\b\w+\.data_devolucao_prevista > (?<operando>@\w+)";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeFuncionario()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>Valor que o ToQueryString mostra para um parametro (linha "-- @nome='valor'").</summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString: {sql}");

            return valor.Groups[1].Value;
        }

        /// <summary>Valor enviado ao SQL pelo operando de um predicado (o nome do parametro e lido do proprio SQL).</summary>
        private static string ValorDoOperando(string sql, string predicado)
        {
            Match casamento = Regex.Match(sql, predicado);

            Assert.True(casamento.Success, "O predicado deveria estar no SQL: " + predicado + "\n" + sql);

            string nome = casamento.Groups["operando"].Value.Substring(1);

            return ValorDoParametro(sql, nome);
        }

        [Fact]
        public void ConflitoDeCriacao_RetiradaDaExistenteMenorQueDevolucaoPedidaEDevolucaoMaiorQueRetirada()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario();
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());
            DateTime retiradaPedida = new DateTime(AnoDoTeste, MesDoTeste, DiaDaRetiradaPedida);
            DateTime devolucaoPedida = new DateTime(AnoDoTeste, MesDoTeste, DiaDaDevolucaoPedida);

            string sql = repositorio.ConsultaReservasDoVeiculo(IdVeiculoDoTeste)
                .Where(RegrasReserva.SobrepoePeriodo(retiradaPedida, devolucaoPedida))
                .ToQueryString();

            Assert.Contains(DataDaDevolucaoPedidaNoSql, ValorDoOperando(sql, PredicadoRetiradaDaReservaMenorQueDevolucaoPedida));
            Assert.Contains(DataDaRetiradaPedidaNoSql, ValorDoOperando(sql, PredicadoDevolucaoDaReservaMaiorQueRetiradaPedida));
        }

        [Fact]
        public void BuscaDeDisponiveis_MesmaSobreposicaoNaSubconsultaDeReservas()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());
            DateOnly retirada = new DateOnly(AnoDoTeste, MesDoTeste, DiaDaRetiradaPedida);
            DateOnly devolucao = new DateOnly(AnoDoTeste, MesDoTeste, DiaDaDevolucaoPedida);

            string sql = repositorio.ConsultaDisponiveis(retirada, devolucao, null, null, null).ToQueryString();

            Assert.Contains(DataDaDevolucaoPedidaNoSql, ValorDoOperando(sql, PredicadoRetiradaDaReservaMenorQueDevolucaoPedida));
            Assert.Contains(DataDaRetiradaPedidaNoSql, ValorDoOperando(sql, PredicadoDevolucaoDaReservaMaiorQueRetiradaPedida));
        }
    }
}
