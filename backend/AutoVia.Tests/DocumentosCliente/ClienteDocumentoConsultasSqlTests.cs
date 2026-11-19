using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// Consultas dos documentos do cliente traduzidas pelo EF sem banco (ToQueryString). Prova a FORMA do SQL: filtro pelo cliente,
    /// "disabled_at IS NULL" nas consultas de ativos, a contagem do teto SEM filtro de desativados, e que a tabela nao tem filtro
    /// "Empresa" (e global). NAO prova o resultado no PostgreSQL.
    /// </summary>
    public sealed class ClienteDocumentoConsultasSqlTests
    {
        private const int IdClienteDoTeste = 7;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string TabelaDocumentos = "cliente_documento";
        private const string SeparadorDeComando = ";";
        private const string GroupBy = "GROUP BY";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private const string CabecalhoValorIdCliente = "-- @idCliente='7'";
        private const string FiltroIdCliente = "id_cliente = @idCliente";
        private const string CabecalhoValorTipoComprovante = "-- @tipo='COMPROVANTE_RESIDENCIA'";
        private const string FiltroTipo = "tipo = @tipo";
        private const string FiltroDesde = "created_at >= @";
        private const string PadraoValorDesde = @"-- @\w+='2026-11-18";
        private const string SelectSemWhere = "WHERE";

        private static ClienteDocumentoRepository CriarRepositorio()
        {
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, ContextoEmpresaDeTeste.Criar(4242));

            return new ClienteDocumentoRepository(db, new FakeLogService());
        }

        /// <summary>Trecho do SQL depois do WHERE. Sem WHERE o teste falha: a consulta teria de filtrar pelo cliente.</summary>
        private static string ClausulaWhere(string sql)
        {
            int indice = sql.IndexOf(SelectSemWhere, StringComparison.Ordinal);
            Assert.True(indice >= 0, "O SQL nao tem WHERE: " + sql);

            return sql.Substring(indice);
        }

        [Fact]
        public void ConsultaDeEnviosDesde_FiltraClienteEDataSemFiltrarDesativados()
        {
            ClienteDocumentoRepository repositorio = CriarRepositorio();

            string sql = repositorio.ConsultaEnviosDesdeWithDisableds(IdClienteDoTeste, new DateTime(2026, 11, 18, 10, 0, 0)).ToQueryString();
            string where = ClausulaWhere(sql);

            Assert.Contains(TabelaDocumentos, sql);
            Assert.Contains(CabecalhoValorIdCliente, sql);
            Assert.Contains(FiltroIdCliente, where);
            Assert.Contains(FiltroDesde, where);
            Assert.Matches(PadraoValorDesde, sql);
            Assert.DoesNotContain(DisabledAtNulo, where);
            Assert.DoesNotContain(FiltroEmpresa, sql);
            Assert.DoesNotContain(SeparadorDeComando, sql);
            Assert.DoesNotContain(GroupBy, sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConsultaDeAtivosDoCliente_FiltraClienteEAtivosSemFiltroDeEmpresa()
        {
            ClienteDocumentoRepository repositorio = CriarRepositorio();

            string sql = repositorio.ConsultaAtivosDoCliente(IdClienteDoTeste).ToQueryString();
            string where = ClausulaWhere(sql);

            Assert.Contains(TabelaDocumentos, sql);
            Assert.Contains(CabecalhoValorIdCliente, sql);
            Assert.Contains(FiltroIdCliente, where);
            Assert.Contains(DisabledAtNulo, where);
            Assert.DoesNotContain(FiltroTipo, where);
            Assert.DoesNotContain(FiltroEmpresa, sql);
            Assert.DoesNotContain(SeparadorDeComando, sql);
        }

        [Fact]
        public void ConsultaDeAtivosDoClienteETipo_FiltraClienteTipoEAtivos()
        {
            ClienteDocumentoRepository repositorio = CriarRepositorio();

            string sql = repositorio.ConsultaAtivosDoClienteETipo(IdClienteDoTeste, TipoDocumentoCliente.COMPROVANTE_RESIDENCIA).ToQueryString();
            string where = ClausulaWhere(sql);

            Assert.Contains(TabelaDocumentos, sql);
            Assert.Contains(CabecalhoValorIdCliente, sql);
            Assert.Contains(CabecalhoValorTipoComprovante, sql);
            Assert.Contains(FiltroIdCliente, where);
            Assert.Contains(FiltroTipo, where);
            Assert.Contains(DisabledAtNulo, where);
            Assert.DoesNotContain(FiltroEmpresa, sql);
            Assert.DoesNotContain(SeparadorDeComando, sql);
        }
    }
}
