using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// SQL da consulta da rotina de atraso (contrato pagamento-atraso v1) por ToQueryString, sem banco: a consulta base e
    /// PENDENTE, sem caucao, com vencimento anterior a hoje e sem desativados, com o filtro de locadora ligado; a versao da
    /// rotina geral tira o filtro de locadora e nao leva o parametro da empresa. O resultado no PostgreSQL nao e provado aqui.
    /// </summary>
    public class PagamentoAtrasoConsultasSqlTests
    {
        private const int IdFuncionarioDoTeste = 9;
        private const int EmpresaDoTeste = 4242;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string StatusPendenteNoSql = "status = 'PENDENTE'";
        private const string TipoSemCaucaoNoSql = "tipo <> 'CAUCAO'";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string VencimentoAnteriorNoSql = @"data_vencimento < @\w+";
        private static readonly DateOnly HojeDoTeste = new DateOnly(2026, 11, 19);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.FINANCEIRO, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        [Fact]
        public void ConsultaAtrasosVencidos_FiltraPendenteSemCaucaoVencidoEAntesDeHojeENaoDesativado()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAtrasosVencidos(HojeDoTeste).ToQueryString();

            Assert.Contains(StatusPendenteNoSql, sql);
            Assert.Contains(TipoSemCaucaoNoSql, sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Matches(VencimentoAnteriorNoSql, sql);
        }

        [Fact]
        public void ConsultaAtrasosVencidos_ComFiltroDeLocadoraLigado_LevaOParametroDaEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAtrasosVencidos(HojeDoTeste).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
        }

        [Fact]
        public void ConsultaAtrasosVencidos_SemEscopoDaRotina_NaoLevaOParametroDaEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            PagamentoRepository repositorio = new PagamentoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaAtrasosVencidos(HojeDoTeste).SemEscopoEmpresa().ToQueryString();

            Assert.DoesNotContain(FiltroEmpresa, sql);
            Assert.Contains(StatusPendenteNoSql, sql);
            Assert.Contains(TipoSemCaucaoNoSql, sql);
        }
    }
}
