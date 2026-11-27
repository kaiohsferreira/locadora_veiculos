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
    /// Consulta real do PropostaCompraRepository traduzida pelo EF sem banco (ToQueryString), no molde do
    /// ReservaConsultasSqlTests. ConsultaPropostasDoClienteLogado desliga o filtro "Empresa"; a posse e a propria consulta:
    /// o WHERE traz o id do cliente do contexto e vira FALSE para outro cliente, para contexto zerado e para contexto de
    /// funcionario. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class PropostaCompraConsultasSqlTests
    {
        private const int IdClienteDoTeste = 1;
        private const int IdClienteAlheio = 2;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string PosseNegadaNoSql = "WHERE FALSE";
        private const string PredicadoDoDonoNoSql = @"WHERE p\.id_cliente = (@\S+) AND p\.disabled_at IS NULL";
        private const string ClienteNoSql = "p.id_cliente =";

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

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static string SqlDaConsulta(AutoViaDbContext db, int idClientePedido)
        {
            PropostaCompraRepository repositorio = new PropostaCompraRepository(db, new FakeLogService());

            return repositorio.ConsultaPropostasDoClienteLogado(idClientePedido).ToQueryString();
        }

        /// <summary>Le, no WHERE do SQL, o nome do parametro que o predicado usa (grupo 1 do padrao).</summary>
        private static string ExtrairParametro(string sql, string padrao)
        {
            Match casamento = Regex.Match(sql, padrao);

            Assert.True(casamento.Success, "Predicado esperado nao encontrado no SQL: " + sql);

            return casamento.Groups[1].Value;
        }

        [Fact]
        public void MesmoDono_WhereTrazOIdDoClienteDoContextoSemParametroDeEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);

            string sql = SqlDaConsulta(db, IdClienteDoTeste);

            string parametro = ExtrairParametro(sql, PredicadoDoDonoNoSql);
            Assert.Contains($"{parametro.TrimStart('@')}='{IdClienteDoTeste}'", sql);
            Assert.DoesNotContain(PosseNegadaNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.Contains("INNER JOIN veiculo", sql);
        }

        [Fact]
        public void OutroCliente_WhereFalseSemPredicadoDeCliente()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);

            string sql = SqlDaConsulta(db, IdClienteAlheio);

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
            Assert.DoesNotContain(ClienteNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void SemClienteNoContexto_WhereFalseMesmoComIdPedidoPositivo()
        {
            using AutoViaDbContext db = CriarDbSemContexto();

            string sql = SqlDaConsulta(db, IdClienteDoTeste);

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void IdPedidoZero_WhereFalse()
        {
            using AutoViaDbContext db = CriarDbSemContexto();

            string sql = SqlDaConsulta(db, 0);

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
        }

        [Fact]
        public void ContextoDeFuncionario_WhereFalseSemParametroDeEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);

            string sql = SqlDaConsulta(db, IdClienteDoTeste);

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }
    }
}
