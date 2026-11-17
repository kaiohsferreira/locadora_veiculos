using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas das fotos do veiculo do VeiculoFotoRepository traduzidas pelo EF sem banco (ToQueryString). Prova a FORMA do SQL:
    /// o filtro "Empresa" com o id do contexto, a guarda de contexto positivo, "disabled_at IS NULL" e, sem contexto, o id 0 com
    /// a guarda falsa (fail-closed). NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class VeiculoFotoConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdVeiculoDoTeste = 14;
        private const int IdVeiculoFotoDoTeste = 3;
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
        public void ConsultaVeiculoAtivo_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVeiculoAtivo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaVeiculoAtivo_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVeiculoAtivo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaFotosAtivasDoVeiculo_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFotosAtivasDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains("veiculo_foto", sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaFotosAtivasDoVeiculo_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFotosAtivasDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaFotoAtiva_ComContextoDaEmpresa_TraduzOFiltroComOIdDaEmpresaEDesativadasFora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFotoAtiva(IdVeiculoDoTeste, IdVeiculoFotoDoTeste).ToQueryString();

            Assert.Contains("veiculo_foto", sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaFotoAtiva_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoFotoRepository repositorio = new VeiculoFotoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFotoAtiva(IdVeiculoDoTeste, IdVeiculoFotoDoTeste).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{ContextoSemEmpresa}'", sql);
            Assert.Contains(GuardaFalso, sql);
            Assert.DoesNotContain($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }
    }
}
