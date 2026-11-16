using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Seguros
{
    /// <summary>
    /// Consultas reais do SeguroRepository traduzidas pelo EF sem banco (ToQueryString). Prova, para cada consulta
    /// publica do repositorio, que o filtro "Empresa" do contexto entra no SQL (apolice e veiculo de outra locadora nao
    /// aparecem), que as desativadas ficam de fora e que a unicidade do numero da apolice e por locadora (D-4). NAO prova
    /// o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class SeguroConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int OutraEmpresaDoTeste = 7373;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdSeguroIgnorado = 55;
        private const string ParametroEmpresa = "@ef_filter__IdEmpresaContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string TabelaVeiculo = "veiculo AS";
        private const string FiltroEmpresaDoVeiculo = "id_empresa = @ef_filter__IdEmpresaContexto";
        private const string ComparacaoIdIgnorado = "id_seguro <>";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static readonly DateOnly InicioJanela = new DateOnly(2026, 11, 1);
        private static readonly DateOnly FimJanela = new DateOnly(2026, 12, 1);

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ADMINISTRADOR, false);
            }

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static SeguroRepository CriarRepositorio(AutoViaDbContext db)
        {
            return new SeguroRepository(db, new FakeLogService());
        }

        [Fact]
        public void ConsultaSeguros_SemFiltros_TemOFiltroDaEmpresaEDesativadasDeFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(db);

            string sql = repositorio.ConsultaSeguros(null, null, null).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(FiltroEmpresaDoVeiculo, sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains(TabelaVeiculo, sql);
        }

        [Fact]
        public void ConsultaSeguros_ComFiltros_CombinaVeiculoEJanelaComAnd()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(db);

            string sql = repositorio.ConsultaSeguros(7, InicioJanela, FimJanela).ToQueryString();

            Assert.Contains("id_veiculo = @", sql);
            Assert.Contains("data_fim >= @", sql);
            Assert.Contains("data_fim <= @", sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains(" AND ", sql);
        }

        [Fact]
        public void ConsultaSeguroPorId_TrazOVeiculoComOFiltroDaEmpresaEDesativadasDeFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(db);

            string sql = repositorio.ConsultaSeguroPorId(3).ToQueryString();

            Assert.Contains(TabelaVeiculo, sql);
            Assert.Contains(FiltroEmpresaDoVeiculo, sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaSegurosComNumero_CompararComTrimEMaiusculasEFiltroDaEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(db);

            string sql = repositorio.ConsultaSegurosComNumero("AP-100", null).ToQueryString();

            Assert.Contains("upper(", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("trim", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(FiltroEmpresaDoVeiculo, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaSegurosComNumero_ComIdIgnorado_ExcluiEsseIdDaComparacao()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(db);

            string comIgnorado = repositorio.ConsultaSegurosComNumero("AP-100", IdSeguroIgnorado).ToQueryString();
            string semIgnorado = repositorio.ConsultaSegurosComNumero("AP-100", null).ToQueryString();

            Assert.Contains(ComparacaoIdIgnorado, comIgnorado);
            Assert.DoesNotContain(ComparacaoIdIgnorado, semIgnorado);
        }

        [Fact]
        public void Seguro_NumeroApoliceUnico_SoPorLocadora_ConsultaDeDuplicidadeNaoEnxergaOutraEmpresa()
        {
            using AutoViaDbContext dbDaEmpresa = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            using AutoViaDbContext dbDaOutra = CriarDbComContextoDeFuncionario(OutraEmpresaDoTeste);

            string sqlDaEmpresa = CriarRepositorio(dbDaEmpresa).ConsultaSegurosComNumero("AP-100", null).ToQueryString();
            string sqlDaOutra = CriarRepositorio(dbDaOutra).ConsultaSegurosComNumero("AP-100", null).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sqlDaEmpresa);
            Assert.DoesNotContain($"{ParametroEmpresa}='{OutraEmpresaDoTeste}'", sqlDaEmpresa);
            Assert.Contains($"{ParametroEmpresa}='{OutraEmpresaDoTeste}'", sqlDaOutra);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sqlDaOutra);
        }

        [Fact]
        public void ApoliceEVeiculoDeOutraLocadora_NaoEntramNaConsultaDaEmpresaAtual()
        {
            using AutoViaDbContext dbDaEmpresa = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            SeguroRepository repositorio = CriarRepositorio(dbDaEmpresa);

            string sqlLista = repositorio.ConsultaSeguros(null, null, null).ToQueryString();
            string sqlPorId = repositorio.ConsultaSeguroPorId(3).ToQueryString();
            string sqlNumero = repositorio.ConsultaSegurosComNumero("AP-100", null).ToQueryString();

            foreach (string sql in new List<string> { sqlLista, sqlPorId, sqlNumero })
            {
                Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
                Assert.DoesNotContain($"{ParametroEmpresa}='{OutraEmpresaDoTeste}'", sql);
            }
        }

        [Fact]
        public void SemContexto_FailClosed_OFiltroViraFalsoNasTresConsultas()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(0);
            SeguroRepository repositorio = CriarRepositorio(db);

            List<string> sqls = new List<string>
            {
                repositorio.ConsultaSeguros(null, null, null).ToQueryString(),
                repositorio.ConsultaSeguroPorId(3).ToQueryString(),
                repositorio.ConsultaSegurosComNumero("AP-100", null).ToQueryString()
            };

            foreach (string sql in sqls)
            {
                Assert.Contains($"{ParametroEmpresa}='0'", sql);
                Assert.Contains("='False'", sql);
            }
        }
    }
}
