using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Consultas reais do ClienteLocadoraRepository, traduzidas pelo EF sem banco (T-BE-06, C5). ToQueryString traduz a
    /// consulta inteira; se alguma parte exigisse avaliacao no cliente, o EF lancaria aqui. Confere: o "minhas locadoras"
    /// com o Min correlacionado (a subconsulta tambem recebe o filtro "Empresa"), a linha que vale do par (menor id) e as
    /// linhas ativas do par usadas na decisao. NAO prova o resultado no PostgreSQL: so a forma do SQL.
    /// </summary>
    public class ClienteLocadoraConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdClienteDoTeste = 5;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string ParametroCliente = "ef_filter__IdClienteContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeCliente(int idCliente)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.DefinirCliente(idCliente);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static int CountOcorrencias(string texto, string trecho)
        {
            int total = 0;
            int indice = texto.IndexOf(trecho, StringComparison.Ordinal);

            while (indice >= 0)
            {
                total++;
                indice = texto.IndexOf(trecho, indice + trecho.Length, StringComparison.Ordinal);
            }

            return total;
        }

        [Fact]
        public void MinhasLocadoras_TraduzOMinCorrelacionadoComOFiltroNaSubconsulta()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ClienteLocadoraRepository repositorio = new ClienteLocadoraRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMinhasPaginada(IdClienteDoTeste, 0, 20).ToQueryString();

            // O Npgsql traduz o Min como subconsulta escalar "min(c0.id_cliente_locadora)" correlacionada ao vinculo externo "c".
            Assert.Contains("min(c0.id_cliente_locadora)", sql);
            string subconsulta = sql.Substring(sql.IndexOf("min(c0", StringComparison.Ordinal));
            Assert.Contains(ParametroCliente, subconsulta);
            Assert.Contains("c0.id_empresa = c.id_empresa", subconsulta);
            Assert.Contains("c0.id_cliente = c.id_cliente", subconsulta);
            Assert.Contains(DisabledAtNulo, subconsulta);
        }

        [Fact]
        public void MinhasLocadoras_TemJoinDaLocadoraOrdenacaoEPaginacaoNoSql()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ClienteLocadoraRepository repositorio = new ClienteLocadoraRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaMinhasPaginada(IdClienteDoTeste, 20, 10).ToQueryString();

            Assert.Contains("JOIN empresa", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("LIMIT", sql);
            Assert.Contains("OFFSET", sql);
        }

        [Fact]
        public void LinhaQueValeDoPar_EhAAtivaDeMenorId_TraduzidaParaSql()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            ClienteLocadoraRepository repositorio = new ClienteLocadoraRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVinculoAtivoMaisAntigo(IdClienteDoTeste, EmpresaDoTeste).ToQueryString();

            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains("ORDER BY", sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void LinhasAtivasDoPar_NaDecisao_TodasAsAtivasSemPaginacao()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            ClienteLocadoraRepository repositorio = new ClienteLocadoraRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVinculosAtivosDoPar(IdClienteDoTeste, EmpresaDoTeste).ToQueryString();

            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains("id_cliente", sql);
            Assert.Contains("id_empresa", sql);
            Assert.DoesNotContain("LIMIT", sql);
            Assert.Equal(1, CountOcorrencias(sql, "FROM cliente_locadora"));
        }
    }
}
