using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Conferencia, pelo SQL gerado (ToQueryString, sem banco), da consulta de clientes por vinculo com a empresa
    /// (RF-59, P10-BE-04). Confere: um EXISTS para cada caminho (reserva, locacao e venda), o id da empresa em cada
    /// um, a condicao de cliente ativo e a condicao de id nao positivo (fail-closed).
    /// </summary>
    public class ConsultaClientesDaEmpresaTests
    {
        private const string ParametroDoContexto = "@ef_filter__IdEmpresaContexto";
        private const int EmpresaConsultada = 4242;
        private const int OutraEmpresa = 7373;
        private const string ExistsSql = "EXISTS";
        private const string TabelaReserva = "reserva";
        private const string TabelaLocacao = "locacao";
        private const string TabelaVenda = "venda";
        private const string TabelaClienteLocadora = "cliente_locadora";
        private const string ClienteAtivoSql = "disabled_at";
        private const string DisabledAtNulo = "disabled_at IS NULL";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        /// <summary>SQL da consulta do repositorio para a empresa informada, com o contexto de empresa informado (0 = sem contexto).</summary>
        private static string SqlDaConsulta(int idEmpresaDoContexto, int idEmpresaConsultada)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresaDoContexto > 0)
            {
                contexto.Definir(idEmpresaDoContexto, 1, CargoFuncionario.ADMINISTRADOR, false);
            }

            using AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);
            ClienteRepository repositorio = new ClienteRepository(db, new FakeLogService());

            return repositorio.ConsultaDeClientesDaEmpresa(idEmpresaConsultada).ToQueryString();
        }

        [Fact]
        public void Consulta_TemUmExistsPorCaminhoDeVinculo()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, EmpresaConsultada);

            Assert.Equal(4, CountOcorrencias(sql, ExistsSql));
            Assert.Contains(TabelaReserva, sql);
            Assert.Contains(TabelaLocacao, sql);
            Assert.Contains(TabelaVenda, sql);
            Assert.Contains(TabelaClienteLocadora, sql);
        }

        /// <summary>O vinculo cliente x locadora conta em qualquer status (PENDENTE, APROVADO ou RECUSADO): a subconsulta nao filtra status.</summary>
        [Fact]
        public void Consulta_VinculoClienteLocadoraContaEmQualquerStatus()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, EmpresaConsultada);

            string subconsultaDoVinculo = sql.Split(ExistsSql).Skip(1).Single(trecho => trecho.Contains(TabelaClienteLocadora));

            Assert.DoesNotContain("status", subconsultaDoVinculo, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(DisabledAtNulo, subconsultaDoVinculo);
        }

        [Fact]
        public void Consulta_UsaOIdDaEmpresaConsultadaEmCadaCaminho()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, EmpresaConsultada);

            Assert.Contains($"='{EmpresaConsultada}'", sql);
        }

        [Fact]
        public void Consulta_MantemOClienteAtivoComoCondicao()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, EmpresaConsultada);

            Assert.Contains(ClienteAtivoSql, sql);
        }

        [Fact]
        public void Consulta_IdDeEmpresaDiferenteDoContexto_ContinuaUsandoOFiltroDoContexto()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, OutraEmpresa);

            Assert.Contains($"='{OutraEmpresa}'", sql);
            Assert.Contains($"{ParametroDoContexto}='{EmpresaConsultada}'", sql);
        }

        /// <summary>Cada EXISTS (reserva, locacao e venda) so conta registro ativo: DisabledAt nulo dentro da propria subconsulta.</summary>
        [Fact]
        public void Consulta_CadaVinculoSoContaRegistroAtivo()
        {
            string sql = SqlDaConsulta(EmpresaConsultada, EmpresaConsultada);

            List<string> subconsultas = sql.Split(ExistsSql).Skip(1).ToList();

            Assert.Equal(4, subconsultas.Count);
            foreach (string subconsulta in subconsultas)
            {
                Assert.Contains(DisabledAtNulo, subconsulta);
            }
        }

        /// <summary>Sem contexto (ou id nao positivo) o EF simplifica a consulta para WHERE FALSE: nenhum cliente e nenhum EXISTS.</summary>
        [Fact]
        public void Consulta_SemContexto_EFSimplificaParaWhereFalseSemNenhumExists()
        {
            string sql = SqlDaConsulta(0, 0);

            Assert.Contains("WHERE FALSE", sql);
            Assert.DoesNotContain(ExistsSql, sql);
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
    }
}
