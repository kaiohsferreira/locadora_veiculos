using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Isolamento
{
    /// <summary>
    /// Consulta nova do LocacaoRepository (ConsultaCaucaoGravadaDaReserva, a releitura sem trava da compensacao da caucao)
    /// traduzida pelo EF sem banco. Prova a FORMA do SQL: filtro "Empresa" do pagamento com o id do contexto que executa a
    /// consulta (guarda verdadeira; sem contexto, id 0 e guarda falsa), tipo CAUCAO, id_transacao e id_reserva por parametro, e o
    /// disabled_at nulo do pagamento e da locacao. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class LocacaoCaucaoConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdReservaDoTeste = 77;
        private const string IdTransacaoDoTeste = "FAKE-CAU-0123456789ab";
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string GuardaVerdadeiro = "='True'";
        private const string GuardaFalso = "='False'";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDb(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, 9, CargoFuncionario.ATENDENTE, false);
            }

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static string Sql(int idEmpresa)
        {
            using AutoViaDbContext db = CriarDb(idEmpresa);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaCaucaoGravadaDaReserva(IdReservaDoTeste, IdTransacaoDoTeste).ToQueryString();

            return sql;
        }

        /// <summary>Acha "coluna = @parametro" no WHERE principal e confere, no cabecalho do ToQueryString, que o parametro vale o esperado.</summary>
        private static void AfirmarPredicadoPorParametro(string sql, string colunaComAlias, string valorEsperado)
        {
            Match predicado = Regex.Match(sql, $@"WHERE[\s\S]*?{Regex.Escape(colunaComAlias)}\s*=\s*@(\w+)");

            Assert.True(predicado.Success, $"O WHERE deveria filtrar {colunaComAlias} = @parametro. SQL: {sql}");
            Assert.Contains($"@{predicado.Groups[1].Value}='{valorEsperado}'", sql);
        }

        [Fact]
        public void ComContexto_FiltroEmpresaComOIdDoContextoEGuardaVerdadeira()
        {
            string sql = Sql(EmpresaDoTeste);

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            Assert.Contains(GuardaVerdadeiro, sql);
        }

        [Fact]
        public void SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            string sql = Sql(0);

            Assert.Contains($"{ParametroEmpresa}='0'", sql);
            Assert.Contains(GuardaFalso, sql);
        }

        [Fact]
        public void FiltraCaucaoDoIdDaReservaEDaTransacao_PagamentoELocacaoAtivos()
        {
            string sql = Sql(EmpresaDoTeste);

            Assert.Contains("p.tipo = 'CAUCAO'", sql);
            AfirmarPredicadoPorParametro(sql, "p.id_transacao", IdTransacaoDoTeste);
            AfirmarPredicadoPorParametro(sql, "s.id_reserva", IdReservaDoTeste.ToString(CultureInfo.InvariantCulture));
            Assert.Contains("p.disabled_at IS NULL", sql);
            Assert.Contains("s.disabled_at IS NULL", sql);
            Assert.Contains("s.id_locacao IS NOT NULL", sql);

            // O id da transacao vai so como parametro: nao aparece no corpo do SELECT como texto fixo.
            string corpo = sql.Substring(sql.IndexOf("SELECT", StringComparison.Ordinal));
            Assert.DoesNotContain(IdTransacaoDoTeste, corpo);
        }
    }
}
