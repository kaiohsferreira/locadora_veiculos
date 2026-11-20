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
    /// Ordem da lista de alertas de revisao (item 6 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026),
    /// traduzida pelo EF sem banco (ToQueryString). Prova o sentido da ordenacao: a falta de km (teto de revisao menos o
    /// km atual, com o maximo de km das preventivas) entra CRESCENTE, sem DESC, e o km atual e subtraido; o desempate e o
    /// id do veiculo. A consulta e a mesma que ListarAlertasRevisaoAsync executa. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class ManutencaoAlertasOrdemSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ClausulaDeOrdem = @"ORDER BY[\s\S]*?LIMIT";
        private const string OrdemDescendente = "DESC";
        private const string SubtracaoDoKmAtual = @"-\s*\w+\.km_atual";
        private const string SomaDoIntervaloDeRevisao = @"\+\s*\d+";
        private const string ColunaDoIdDoVeiculo = @"\w+\.id_veiculo";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static ManutencaoRepository CriarRepositorioDeManutencao()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.GESTOR_FROTA, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new ManutencaoRepository(db, new FakeLogService());
        }

        private static string ClausulaDeOrdemDoSql(string sql)
        {
            Match ordem = Regex.Match(sql, ClausulaDeOrdem);

            Assert.True(ordem.Success, "A consulta de alertas ordenados deveria ter ORDER BY antes do LIMIT: " + sql);

            return ordem.Value;
        }

        /// <summary>A ordem dos alertas nao tem DESC: a falta de revisao cresce e o mais atrasado (menor falta) vem primeiro.</summary>
        [Fact]
        public void AlertasOrdenados_OrdemCrescenteSemDesc()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            string ordem = ClausulaDeOrdemDoSql(sql);

            Assert.DoesNotContain(OrdemDescendente, ordem);
        }

        /// <summary>A falta e o intervalo de revisao mais o maximo de km preventivo, menos o km atual: o km atual entra com sinal de menos.</summary>
        [Fact]
        public void AlertasOrdenados_KmAtualEntraSubtraido_EIntervaloSomado()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            string ordem = ClausulaDeOrdemDoSql(sql);

            Assert.Matches(SubtracaoDoKmAtual, ordem);
            Assert.Matches(SomaDoIntervaloDeRevisao, ordem);
        }

        /// <summary>
        /// O id do veiculo e o desempate, logo depois da falta (que termina com o km atual) e antes do LIMIT. A sequencia
        /// "- v.km_atual, v.id_veiculo" e a da ordenacao da consulta principal; as colunas da subconsulta de manutencao vem antes.
        /// </summary>
        [Fact]
        public void AlertasOrdenados_DesempataPeloIdDoVeiculoDepoisDaFalta()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            string ordem = ClausulaDeOrdemDoSql(sql);

            Assert.Matches(SubtracaoDoKmAtual + @",\s*" + ColunaDoIdDoVeiculo + @"\s+LIMIT", ordem);
        }
    }
}
