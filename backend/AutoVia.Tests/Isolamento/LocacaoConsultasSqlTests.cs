using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Isolamento
{
    /// <summary>
    /// Consultas nomeadas do LocacaoRepository traduzidas pelo EF sem banco (locacao.contract.json, isolamento.consultas_nomeadas).
    /// Prova a FORMA do SQL: o filtro "Empresa" usa o id do contexto que executa a consulta, a guarda de contexto positivo,
    /// o disabled_at e o predicado de id (ou de ativo) de cada consulta. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class LocacaoConsultasSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const int IdDeTeste = 77;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string GuardaVerdadeiro = "='True'";
        private const string GuardaFalso = "='False'";
        private const string ContextoZero = "='0'";
        private const string ColunaIdReserva = "id_reserva";
        private const string ColunaIdFuncionario = "id_funcionario";
        private const string ColunaIdVeiculo = "id_veiculo";
        private const string ColunaIdLocacao = "id_locacao";
        private const string ColunaAtivo = "ativo";
        private const string ClausulaWhere = "WHERE";
        private const string ColunaDataRetirada = "data_retirada";
        private const string ColunaDataDevolucaoPrevista = "data_devolucao_prevista";
        private const string TravaDeLinha = "FOR UPDATE";
        private const string ClausulaOrderBy = "ORDER BY";
        private static readonly DateTime AgoraDeTeste = new DateTime(2026, 11, 26, 10, 0, 0);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDb(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            if (idEmpresa > 0)
            {
                contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);
            }

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>As cinco consultas nomeadas do repositorio, cada uma como o SQL que o EF gera.</summary>
        private static Dictionary<string, Func<LocacaoRepository, string>> ConsultasNomeadas()
        {
            return new Dictionary<string, Func<LocacaoRepository, string>>
            {
                ["ConsultaReservaAtiva"] = repositorio => repositorio.ConsultaReservaAtiva(IdDeTeste).ToQueryString(),
                ["ConsultaLocacoesAtivasDaReserva"] = repositorio => repositorio.ConsultaLocacoesAtivasDaReserva(IdDeTeste).ToQueryString(),
                ["ConsultaFuncionarioAtivo"] = repositorio => repositorio.ConsultaFuncionarioAtivo(IdFuncionarioDoTeste).ToQueryString(),
                ["ConsultaVeiculoAtivo"] = repositorio => repositorio.ConsultaVeiculoAtivo(IdDeTeste).ToQueryString(),
                ["ConsultaLocacaoAtiva"] = repositorio => repositorio.ConsultaLocacaoAtiva(IdDeTeste).ToQueryString(),
                ["ConsultaDevolucoesPendentes"] = repositorio => repositorio.ConsultaDevolucoesPendentes(false, AgoraDeTeste).ToQueryString(),
                ["ConsultaDevolucoesPendentesAtrasadas"] = repositorio => repositorio.ConsultaDevolucoesPendentes(true, AgoraDeTeste).ToQueryString()
            };
        }

        /// <summary>
        /// Prova que o WHERE filtra a coluna pelo valor esperado: acha "coluna = @parametro" depois do WHERE e confere, no
        /// cabecalho do ToQueryString, que esse parametro vale o id esperado. Um SELECT que apenas cite a coluna nao passa.
        /// </summary>
        private static void AfirmarPredicadoPorId(string sql, string coluna, int valorEsperado, string consulta)
        {
            Match predicado = Regex.Match(sql, $@"WHERE[\s\S]*?\b{Regex.Escape(coluna)}\s*=\s*@(\w+)");

            Assert.True(predicado.Success, $"{consulta}: o WHERE deveria filtrar {coluna} = @parametro. SQL: {sql}");
            Assert.Contains($"@{predicado.Groups[1].Value}='{valorEsperado}'", sql);
        }

        /// <summary>Prova que o WHERE exige a coluna ativo (sem NOT e sem FALSE logo depois).</summary>
        private static void AfirmarAtivoNoWhere(string sql, string consulta)
        {
            Match predicadoAtivo = Regex.Match(sql, $@"WHERE[\s\S]*?\b{ColunaAtivo}\b(?!\s*=\s*FALSE)");
            Assert.True(predicadoAtivo.Success, $"{consulta}: o WHERE deveria exigir ativo. SQL: {sql}");
            Assert.DoesNotMatch($@"WHERE[\s\S]*?NOT\s+[\w.]*\b{ColunaAtivo}\b", sql);
        }

        [Fact]
        public void ConsultasNomeadas_ComContextoDeFuncionario_TraduzemOFiltroEmpresaComOIdDoContexto()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            foreach (KeyValuePair<string, Func<LocacaoRepository, string>> caso in ConsultasNomeadas())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'"), $"{caso.Key}: o filtro deveria usar o id {EmpresaDoTeste} do contexto.");
                Assert.True(sql.Contains(GuardaVerdadeiro), $"{caso.Key}: o guarda de contexto positivo deveria estar ativo.");
                Assert.True(sql.Contains(DisabledAtNulo), $"{caso.Key}: deveria filtrar desativados.");
            }
        }

        [Fact]
        public void ConsultasNomeadas_SemContexto_FailClosedComGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDb(0);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            foreach (KeyValuePair<string, Func<LocacaoRepository, string>> caso in ConsultasNomeadas())
            {
                string sql = caso.Value(repositorio);

                Assert.True(sql.Contains($"{ParametroEmpresa}{ContextoZero}"), $"{caso.Key}: sem contexto o id deveria ser 0.");
                Assert.True(sql.Contains(GuardaFalso), $"{caso.Key}: sem contexto o guarda deveria ser falso (nenhuma linha).");
            }
        }

        [Fact]
        public void ConsultaReservaAtiva_FiltraPeloIdDaReserva()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservaAtiva(IdDeTeste).ToQueryString();

            AfirmarPredicadoPorId(sql, ColunaIdReserva, IdDeTeste, "ConsultaReservaAtiva");
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaLocacoesAtivasDaReserva_FiltraPeloIdDaReserva()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacoesAtivasDaReserva(IdDeTeste).ToQueryString();

            AfirmarPredicadoPorId(sql, ColunaIdReserva, IdDeTeste, "ConsultaLocacoesAtivasDaReserva");
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaFuncionarioAtivo_FiltraPeloIdEExigeAtivo()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFuncionarioAtivo(IdFuncionarioDoTeste).ToQueryString();

            AfirmarPredicadoPorId(sql, ColunaIdFuncionario, IdFuncionarioDoTeste, "ConsultaFuncionarioAtivo");
            AfirmarAtivoNoWhere(sql, "ConsultaFuncionarioAtivo");
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaVeiculoAtivo_FiltraPeloIdDoVeiculo()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVeiculoAtivo(IdDeTeste).ToQueryString();

            AfirmarPredicadoPorId(sql, ColunaIdVeiculo, IdDeTeste, "ConsultaVeiculoAtivo");
            Assert.Contains(DisabledAtNulo, sql);
        }

        [Fact]
        public void ConsultaLocacaoAtiva_FiltraPeloIdDaLocacao()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaLocacaoAtiva(IdDeTeste).ToQueryString();

            AfirmarPredicadoPorId(sql, ColunaIdLocacao, IdDeTeste, "ConsultaLocacaoAtiva");
            Assert.Contains(DisabledAtNulo, sql);
        }

        /// <summary>Trecho do WHERE do SQL gerado (a projecao do SELECT lista todas as colunas e atrapalharia a conferencia).</summary>
        private static string TrechoWhere(string sql)
        {
            int inicio = sql.IndexOf(ClausulaWhere, StringComparison.Ordinal);

            Assert.True(inicio >= 0, $"O SQL deveria ter WHERE. SQL: {sql}");

            return sql.Substring(inicio);
        }

        [Fact]
        public void ConsultaDevolucoesPendentes_SemAtrasadas_FiltraEmAndamentoEAtrasadaSemFiltroDeData()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDevolucoesPendentes(false, AgoraDeTeste).ToQueryString();

            Assert.Contains("'EM_ANDAMENTO'", sql);
            Assert.Contains("'ATRASADA'", sql);
            Assert.DoesNotContain(ColunaDataRetirada, TrechoWhere(sql));
            Assert.DoesNotContain(TravaDeLinha, sql);
        }

        [Fact]
        public void ConsultaDevolucoesPendentes_ComAtrasadas_TraduzAExpressaoDeDataSemFromSqlNemTrava()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDevolucoesPendentes(true, AgoraDeTeste).ToQueryString();

            string where = TrechoWhere(sql);
            Assert.Contains(ColunaDataRetirada, where);
            Assert.Contains(ColunaDataDevolucaoPrevista, where);
            Assert.Matches(@"status""?\s*=\s*'ATRASADA'", where);
            Assert.Matches(@"status""?\s*=\s*'EM_ANDAMENTO'", where);
            Assert.Matches(@"<\s*@\w+", where);
            Assert.DoesNotContain("2026-11-26", where);
            Assert.DoesNotContain(TravaDeLinha, sql);
            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaDevolucoesPendentesPagina_OrdenaPelaPrevistaEfetivaDepoisPeloIdEPaginaComLimitEOffset()
        {
            using AutoViaDbContext db = CriarDb(EmpresaDoTeste);
            LocacaoRepository repositorio = new LocacaoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaDevolucoesPendentesPagina(true, AgoraDeTeste, 2, 3).ToQueryString();

            int inicioOrdem = sql.IndexOf(ClausulaOrderBy, StringComparison.Ordinal);
            Assert.True(inicioOrdem >= 0, $"O SQL deveria ter ORDER BY. SQL: {sql}");
            string ordem = sql.Substring(inicioOrdem);
            int posicaoPrevista = ordem.IndexOf(ColunaDataDevolucaoPrevista, StringComparison.Ordinal);
            int posicaoRetirada = ordem.IndexOf(ColunaDataRetirada, StringComparison.Ordinal);
            int posicaoId = ordem.IndexOf(ColunaIdLocacao, StringComparison.Ordinal);
            Assert.True(posicaoPrevista >= 0 && posicaoRetirada >= 0 && posicaoId >= 0, $"O ORDER BY deveria citar as duas datas e o id. ORDER BY: {ordem}");
            Assert.True(posicaoPrevista < posicaoId && posicaoRetirada < posicaoId, $"O id deveria vir depois das datas no ORDER BY. ORDER BY: {ordem}");
            Assert.Matches(@"LIMIT\s+(3|@\w+)\b", sql);
            Assert.Matches(@"OFFSET\s+(2|@\w+)\b", sql);
            Assert.DoesNotContain(TravaDeLinha, sql);
        }
    }
}
