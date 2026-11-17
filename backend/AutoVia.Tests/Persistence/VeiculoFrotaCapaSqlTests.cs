using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consulta da pagina da frota com a capa (VeiculoRepository.ConsultaFrotaPaginada) traduzida pelo EF sem banco
    /// (ToQueryString). Prova: o filtro "Empresa" com o id do contexto tambem na parte das fotos; a paginacao (LIMIT e
    /// OFFSET) fica na subconsulta do veiculo, antes do join das fotos; uma foto por veiculo (ROW_NUMBER, row &lt;= 1) e
    /// so fotos ativas; sem contexto, o id 0 e a guarda falsa. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class VeiculoFrotaCapaSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string GuardaFalso = "='False'";
        private const string Limite = "LIMIT";
        private const string JuncaoDasFotos = "LEFT JOIN";
        private const string TabelaDeFotos = "veiculo_foto";
        private const string PrimeiraFotoPorVeiculo = "row <= 1";
        private const string FotoAtivaNaJuncao = "disabled_at IS NULL";

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
        public void ConsultaFrotaPaginada_ComContextoDeFuncionario_TemOFiltroDaEmpresaTambemNaParteDasFotos()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            string parteDasFotos = sql.Substring(sql.IndexOf(TabelaDeFotos, StringComparison.Ordinal));
            Assert.Contains(ParametroEmpresa, parteDasFotos);
        }

        [Fact]
        public void ConsultaFrotaPaginada_PaginaAntesDoJoinDasFotos()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 20, 10).ToQueryString();

            int posicaoDoLimite = sql.IndexOf(Limite, StringComparison.Ordinal);
            int posicaoDaJuncao = sql.IndexOf(JuncaoDasFotos, StringComparison.Ordinal);
            Assert.True(posicaoDoLimite >= 0, "LIMIT esperado no SQL: " + sql);
            Assert.True(posicaoDoLimite < posicaoDaJuncao, "A paginacao precisa vir antes do join das fotos: " + sql);
        }

        [Fact]
        public void ConsultaFrotaPaginada_TrazSoAPrimeiraFotoAtivaDoVeiculo()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            Assert.Contains(TabelaDeFotos, sql);
            Assert.Contains(PrimeiraFotoPorVeiculo, sql);
            Assert.Contains("ROW_NUMBER() OVER(PARTITION BY", sql);
            string parteDasFotos = sql.Substring(sql.IndexOf(TabelaDeFotos, StringComparison.Ordinal));
            Assert.Contains(FotoAtivaNaJuncao, parteDasFotos);
        }

        [Fact]
        public void ConsultaFrotaPaginada_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='0'", sql);
            Assert.Contains(GuardaFalso, sql);
        }
    }
}
