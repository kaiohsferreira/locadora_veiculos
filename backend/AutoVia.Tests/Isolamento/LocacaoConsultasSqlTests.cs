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
                ["ConsultaLocacaoAtiva"] = repositorio => repositorio.ConsultaLocacaoAtiva(IdDeTeste).ToQueryString()
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
    }
}
