using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas de vistoria e de avaria do AvariaRepository traduzidas pelo EF sem banco (ToQueryString). Prova a FORMA do SQL:
    /// o filtro "Empresa" com o id do contexto, a guarda de contexto positivo, "disabled_at IS NULL" e, sem contexto, o id 0 com
    /// a guarda falsa (fail-closed). NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class AvariaConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdVistoriaDoTeste = 14;
        private const int IdAvariaDoTeste = 3;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string GuardaVerdadeiro = "='True'";
        private const string GuardaFalso = "='False'";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string ContextoSemEmpresa = "0";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        [Fact]
        public void ConsultaVistoriaAtiva_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVistoriaAtiva(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaVistoriaAtiva_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVistoriaAtiva(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaVistoriaAtivaComLocacao_ComContextoDaEmpresa_TraduzFiltroEJuncaoComALocacao()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVistoriaAtivaComLocacao(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains("JOIN", sql);
        }

        [Fact]
        public void ConsultaVistoriaAtivaComLocacao_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVistoriaAtivaComLocacao(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaAvariaAtiva_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAvariaAtiva(IdVistoriaDoTeste, IdAvariaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaAvariasAtivasDaVistoria_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAvariasAtivasDaVistoria(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaAvariasAtivasDaVistoria_OrdenaPorIdAvariaDentroDaPropriaConsulta()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAvariasAtivasDaVistoria(IdVistoriaDoTeste).ToQueryString();

            int inicioOrdenacao = sql.IndexOf("ORDER BY", StringComparison.Ordinal);
            Assert.True(inicioOrdenacao >= 0);
            Assert.Contains("id_avaria", sql.Substring(inicioOrdenacao));
        }

        [Fact]
        public void ConsultaAvariasAtivasDaVistoria_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAvariasAtivasDaVistoria(IdVistoriaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaAvariaAtiva_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            AvariaRepository repositorio = new AvariaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAvariaAtiva(IdVistoriaDoTeste, IdAvariaDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }
    }
}
