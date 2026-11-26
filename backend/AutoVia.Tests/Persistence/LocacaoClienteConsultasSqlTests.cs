using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas das locacoes do cliente (tela 27) traduzidas pelo EF sem banco (ToQueryString). Prova a posse do cliente
    /// dentro do WHERE (dono, outro cliente e contexto zerado), que a consulta nao leva o filtro por locadora (ef_filter da
    /// Empresa, tambem dentro das subconsultas), a locadora ativa, e que tolerancia, combustivel e capa entram na mesma
    /// instrucao (joins e subconsultas). NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class LocacaoClienteConsultasSqlTests
    {
        private const int IdClienteDoTeste = 1;
        private const int IdClienteAlheio = 2;
        private const int IdEmpresaDoTeste = 4242;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string PosseNegadaNoSql = "WHERE FALSE";
        private const string PredicadoDoDonoNoSql = @"WHERE l\.id_cliente = @\S+ AND l\.disabled_at IS NULL";
        private const string LocadoraAtivaNoSql = @"e\.ativo AND e\.disabled_at IS NULL";
        private const string TabelaDaConfiguracao = "configuracao_cobranca_extra";
        private const string TabelaDaVistoria = "vistoria";
        private const string TabelaDasFotos = "veiculo_foto";
        private const string CampoDaTolerancia = "tolerancia_atraso_minutos";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeCliente(int idCliente)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.DefinirCliente(idCliente);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbComContextoDeEmpresa(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, 9, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static readonly IReadOnlyCollection<StatusLocacao> StatusDeAndamento = new StatusLocacao[] { StatusLocacao.EM_ANDAMENTO, StatusLocacao.ATRASADA };

        [Fact]
        public void ConsultaDoCliente_DonoDoContexto_TemAPosseNoWhereELocadoraAtiva()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(PosseNegadaNoSql, sql);
            Assert.Matches(PredicadoDoDonoNoSql, sql);
            Assert.Matches(LocadoraAtivaNoSql, sql);
        }

        [Fact]
        public void ConsultaDoCliente_NaoTemParametroDeEmpresa_MesmoComOsJoinsDaLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.Contains("INNER JOIN veiculo", sql);
            Assert.Contains("INNER JOIN empresa", sql);
        }

        [Fact]
        public void ConsultaDoCliente_OutroClienteQueOContextoNaoTem_NaoTrazNada()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesDoClienteLogado(IdClienteAlheio).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
        }

        [Fact]
        public void ConsultaDoCliente_SemContextoDeCliente_NaoTrazNada()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
        }

        [Fact]
        public void ConsultaDoClienteComContextoDeEmpresa_AindaNaoTemFiltroDeLocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeEmpresa(IdEmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void ConsultaPaginada_ToleranciaCombustivelECapaEntramNaMesmaInstrucao_SemParametroDeEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPaginaMinhasLocacoes(IdClienteDoTeste, StatusDeAndamento, 0, 20).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.Contains($"FROM {TabelaDaConfiguracao}", sql);
            Assert.Contains(CampoDaTolerancia, sql);
            Assert.Contains($"FROM {TabelaDaVistoria}", sql);
            Assert.Contains($"FROM {TabelaDasFotos}", sql);
            Assert.DoesNotContain(PosseNegadaNoSql, sql);
        }

        [Fact]
        public void ConsultaPaginada_FiltraPorPosseEPaginaNoBanco()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPaginaMinhasLocacoes(IdClienteDoTeste, StatusDeAndamento, 20, 10).ToQueryString();

            Assert.Matches(PredicadoDoDonoNoSql, sql);
            Assert.Matches(@"ORDER BY \S+\.data_retirada DESC", sql);
            Assert.Contains("LIMIT", sql);
        }

        [Fact]
        public void ConsultaPaginada_DeOutroCliente_NaoTrazNada()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaPaginaMinhasLocacoes(IdClienteAlheio, StatusDeAndamento, 0, 20).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
        }
    }
}
