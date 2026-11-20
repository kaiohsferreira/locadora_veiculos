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
    /// Consulta de locacoes do dia do painel operacional (PainelOperacionalRepository.ConsultaLocacoesDoDia) traduzida pelo EF
    /// sem banco (ToQueryString). Prova: o SQL filtra disabled_at nulo, o conjunto de status em aberto (EM_ANDAMENTO e ATRASADA,
    /// como PARAMETRO de lista), sem FINALIZADA nem CANCELADA, e o filtro "Empresa" ligado com a locadora do teste.
    /// NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class PainelOperacionalConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string PredicadoDoStatusDeLocacaoNoSql = @"l\.status = ANY \((@\w+)\)";
        private const string ValorDoStatusDeLocacaoNoSql = @"'EM_ANDAMENTO', 'ATRASADA'";
        private const string StatusFinalizadaNoSql = "'FINALIZADA'";
        private const string StatusCanceladaNoSql = "'CANCELADA'";

        private static readonly DateTime InicioDoDia = new DateTime(2026, 11, 20, 0, 0, 0);
        private static readonly DateTime InicioDoProximoDia = new DateTime(2026, 11, 21, 0, 0, 0);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static PainelOperacionalRepository CriarRepositorio()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.GESTOR_FROTA, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new PainelOperacionalRepository(db, new FakeLogService());
        }

        /// <summary>
        /// Valor do parametro no cabecalho do ToQueryString. O filtro vem como "-- @nome='valor'" e os argumentos do SQL bruto
        /// como "-- nome='valor'", sem o arroba. Falha se o parametro nao aparecer.
        /// </summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @?" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        [Fact]
        public void ConsultaLocacoesDoDia_FiltraDisabledAtEOsDoisStatusEmAberto()
        {
            string sql = CriarRepositorio().ConsultaLocacoesDoDia(InicioDoDia, InicioDoProximoDia).ToQueryString();

            Assert.Matches(PredicadoDoStatusDeLocacaoNoSql, sql);
            Assert.Contains(ValorDoStatusDeLocacaoNoSql, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaLocacoesDoDia_NaoTrazFinalizadaNemCancelada()
        {
            string sql = CriarRepositorio().ConsultaLocacoesDoDia(InicioDoDia, InicioDoProximoDia).ToQueryString();

            Assert.DoesNotContain(StatusFinalizadaNoSql, sql);
            Assert.DoesNotContain(StatusCanceladaNoSql, sql);
        }

        [Fact]
        public void ConsultaLocacoesDoDia_FiltroEmpresaContinuaLigadoComALocadoraDoTeste()
        {
            string sql = CriarRepositorio().ConsultaLocacoesDoDia(InicioDoDia, InicioDoProximoDia).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }
    }
}
