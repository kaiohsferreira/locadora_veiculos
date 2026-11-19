using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Consulta da fila de verificacao (ClienteLocadoraRepository.ConsultaFila e ConsultaFilaPaginada) traduzida pelo EF sem
    /// banco (ToQueryString). Prova: o filtro "Empresa" entra com o id do contexto; a linha que vale do par usa o minimo do id
    /// ativo (MIN); o status filtra o vinculo; a busca casa nome e e-mail com LIKE escapado (% e _ e \ do termo saem como
    /// texto); o CPF so entra com digitos; o vinculo e o cliente filtram disabled_at nulo; a pagina ordena por data do pedido e
    /// id do cliente e usa deslocamento e quantidade. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class ClienteVerificacaoConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string TermoComCuringa = "a%_b";
        private const string TermoComCuringaEscapado = @"%a\%\_b%";
        private const string TermoComCuringaSemEscape = "%a%_b%";
        private const string TermoComBarra = @"a\b";
        private const string TermoComBarraEscapado = @"%a\\b%";
        private const string TermoComBarraSemEscape = @"%a\b%";
        private const string Digitos = "12345";
        private const string CpfNaConsulta = "cpf";
        private const string StatusAprovado = "APROVADO";
        private const string FuncaoMinimo = "MIN(";
        private const string ClausulaDeOrdemNaFila = @"ORDER BY[^\r\n]*";
        private const string ClausulaDeLimiteNaFila = @"LIMIT (@(\w+)|(\d+))";
        private const string ClausulaDeDeslocamentoNaFila = @"OFFSET (@(\w+)|(\d+))";
        private const string FiltroDisabledAtNulo = @"\.disabled_at IS NULL";
        private const string ColunaDataSolicitacao = "data_solicitacao";
        private const string ColunaIdCliente = "id_cliente";
        private const int DeslocamentoDoTeste = 20;
        private const int QuantidadeDoTeste = 10;
        private const int VinculoClienteESubconsultaComDisabledAtNulo = 3;

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>Valor de LIMIT ou OFFSET no SQL: parametro (resolvido no cabecalho do ToQueryString) ou literal.</summary>
        private static string ValorDaClausula(string sql, Match clausula)
        {
            if (!clausula.Groups[2].Success)
            {
                return clausula.Groups[3].Value;
            }

            Match valor = Regex.Match(sql, @"-- @?" + Regex.Escape(clausula.Groups[2].Value) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {clausula.Groups[2].Value} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        private static ClienteLocadoraRepository CriarRepositorioDoFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new ClienteLocadoraRepository(db, new FakeLogService());
        }

        [Fact]
        public void ConsultaFila_SemFiltros_TemFiltroDeEmpresaEVinculoQueVale()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFila(null, null, null).ToQueryString();

            Assert.Contains(ParametroEmpresa + "='" + EmpresaDoTeste.ToString(System.Globalization.CultureInfo.InvariantCulture) + "'", sql);
            Assert.Contains(FuncaoMinimo, sql.ToUpperInvariant());
            Assert.Contains("JOIN cliente", sql);
        }

        [Fact]
        public void ConsultaFila_BuscaSaiComOsCuringasEscapados()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFila(null, TermoComCuringa, null).ToQueryString();

            Assert.Contains(TermoComCuringaEscapado, sql);
            Assert.DoesNotContain(TermoComCuringaSemEscape, sql);
        }

        [Fact]
        public void ConsultaFila_CpfSoEntraComDigitos()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string semDigitos = repositorio.ConsultaFila(null, "maria", null).ToQueryString();
            string comDigitos = repositorio.ConsultaFila(null, "maria", Digitos).ToQueryString();

            Assert.DoesNotContain(CpfNaConsulta, semDigitos);
            Assert.Contains(CpfNaConsulta, comDigitos);
        }

        [Fact]
        public void ConsultaFila_StatusFiltraOVinculo()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFila(StatusCadastroCliente.APROVADO, null, null).ToQueryString();

            Assert.Contains(StatusAprovado, sql);
        }

        [Fact]
        public void ConsultaFila_BarraInvertidaDoTermoSaiEscapada()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFila(null, TermoComBarra, null).ToQueryString();

            Assert.Contains(TermoComBarraEscapado, sql);
            Assert.DoesNotContain(TermoComBarraSemEscape, sql);
        }

        [Fact]
        public void ConsultaFila_VinculoECLienteFiltramDisabledAtNulo()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFila(null, null, null).ToQueryString();

            Assert.True(Regex.Matches(sql, FiltroDisabledAtNulo).Count >= VinculoClienteESubconsultaComDisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaFilaPaginada_OrdenaPorDataDoPedidoEIdDoClienteEPagina()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFilaPaginada(null, null, null, DeslocamentoDoTeste, QuantidadeDoTeste).ToQueryString();
            Match ordem = Regex.Match(sql, ClausulaDeOrdemNaFila);
            Match limite = Regex.Match(sql, ClausulaDeLimiteNaFila);
            Match deslocamento = Regex.Match(sql, ClausulaDeDeslocamentoNaFila);

            Assert.True(ordem.Success, sql);
            Assert.True(ordem.Value.IndexOf(ColunaDataSolicitacao, StringComparison.Ordinal) < ordem.Value.IndexOf(ColunaIdCliente, StringComparison.Ordinal), ordem.Value);
            Assert.True(limite.Success, sql);
            Assert.Equal(QuantidadeDoTeste.ToString(CultureInfo.InvariantCulture), ValorDaClausula(sql, limite));
            Assert.True(deslocamento.Success, sql);
            Assert.Equal(DeslocamentoDoTeste.ToString(CultureInfo.InvariantCulture), ValorDaClausula(sql, deslocamento));
        }

        [Fact]
        public void ConsultaFilaPaginada_TemFiltroDeEmpresaEIncluiOCliente()
        {
            ClienteLocadoraRepository repositorio = CriarRepositorioDoFuncionario(EmpresaDoTeste);

            string sql = repositorio.ConsultaFilaPaginada(StatusCadastroCliente.PENDENTE, "maria", Digitos, 0, QuantidadeDoTeste).ToQueryString();

            Assert.Contains(ParametroEmpresa + "='" + EmpresaDoTeste.ToString(CultureInfo.InvariantCulture) + "'", sql);
            Assert.Contains("JOIN cliente", sql);
            Assert.Contains(CpfNaConsulta, sql);
        }
    }
}
