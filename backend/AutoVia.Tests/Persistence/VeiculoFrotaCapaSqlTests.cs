using AutoVia.Communication.Enums;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consulta da pagina da frota com a capa (VeiculoRepository.ConsultaFrotaPaginada) traduzida pelo EF sem banco
    /// (ToQueryString). Prova: o filtro "Empresa" com o id do contexto tambem dentro da subconsulta de fotos; a paginacao
    /// (LIMIT e OFFSET) fica na subconsulta do veiculo, antes do join das fotos; na subconsulta de fotos, uma foto por
    /// veiculo (ROW_NUMBER, row &lt;= 1), so fotos ativas (disabled_at IS NULL) e ordem por ordem e id da foto; sem
    /// contexto, o id 0 e a guarda falsa. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class VeiculoFrotaCapaSqlTests
    {
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string GuardaFalso = "='False'";
        private const string Limite = "LIMIT";
        private const string JuncaoDasFotos = "LEFT JOIN";
        private const string TabelaDeFotos = "veiculo_foto";
        private const string FromDaTabelaDeFotos = "FROM veiculo_foto";
        private const string PrimeiraFotoPorVeiculo = "row <= 1";
        private const string ParticaoDaPrimeiraFoto = "ROW_NUMBER() OVER(PARTITION BY";
        private const string OrdemDaPrimeiraFotoNaJanela = @"ROW_NUMBER\(\) OVER\(PARTITION BY \w+\.id_veiculo ORDER BY \w+\.ordem, \w+\.id_veiculo_foto\)";

        /// <summary>Alias da tabela veiculo_foto na subconsulta da capa (ex.: "FROM veiculo_foto AS v1").</summary>
        private static readonly Regex AliasDaTabelaDeFotos = new Regex(@"FROM veiculo_foto AS (\w+)", RegexOptions.CultureInvariant);

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarDbComContextoDeFuncionario(int idEmpresa)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(idEmpresa, IdFuncionarioDoTeste, CargoFuncionario.ATENDENTE, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static AutoViaDbContext CriarDbSemContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            return new AutoViaDbContext(Opcoes, contexto);
        }

        /// <summary>
        /// Subconsulta da capa: do parentese que abre a subconsulta do LEFT JOIN (o SELECT das fotos, com a janela
        /// ROW_NUMBER, o FROM veiculo_foto e o filtro de empresa) ate o parentese que a fecha. Fica de fora o WHERE do
        /// veiculo (antes do LEFT JOIN) e o SELECT da consulta principal (que traz id_veiculo_foto).
        /// </summary>
        private static string TrechoDaSubconsultaDeFotos(string sql)
        {
            int inicioDaJuncao = sql.IndexOf(JuncaoDasFotos, StringComparison.Ordinal);

            Assert.True(inicioDaJuncao >= 0, "A consulta deveria ter o LEFT JOIN das fotos: " + sql);

            int aberturaDaSubconsulta = sql.IndexOf('(', inicioDaJuncao);
            int fechamentoDaSubconsulta = PosicaoDoParentesePorFechar(sql, aberturaDaSubconsulta);
            string subconsulta = sql.Substring(aberturaDaSubconsulta, fechamentoDaSubconsulta - aberturaDaSubconsulta + 1);

            Assert.Contains(FromDaTabelaDeFotos, subconsulta);

            return subconsulta;
        }

        /// <summary>Posicao do parentese que fecha o parentese aberto na posicao "abertura" (conta a profundidade).</summary>
        private static int PosicaoDoParentesePorFechar(string sql, int abertura)
        {
            int profundidade = 0;
            int posicao = -1;

            for (int indice = abertura; indice < sql.Length && posicao < 0; indice++)
            {
                if (sql[indice] == '(')
                {
                    profundidade++;
                }
                else if (sql[indice] == ')')
                {
                    profundidade--;

                    if (profundidade == 0)
                    {
                        posicao = indice;
                    }
                }
            }

            Assert.True(posicao > abertura, "Parenteses da subconsulta de fotos sem fechamento: " + sql);

            return posicao;
        }

        /// <summary>Prova que a subconsulta de fotos so traz fotos ativas: o alias de veiculo_foto tem disabled_at IS NULL dentro dela.</summary>
        private static void AssertFotoAtivaNaSubconsulta(string subconsulta)
        {
            Match tabela = AliasDaTabelaDeFotos.Match(subconsulta);

            Assert.True(tabela.Success, $"A subconsulta deveria ler veiculo_foto com alias. Trecho: {subconsulta}");
            Assert.True(
                Regex.IsMatch(subconsulta, @"\b" + Regex.Escape(tabela.Groups[1].Value) + @"\.disabled_at IS NULL\b"),
                $"A foto de capa deveria filtrar disabled_at IS NULL dentro da subconsulta de fotos. Trecho: {subconsulta}");
        }

        [Fact]
        public void ConsultaFrotaPaginada_ComContextoDeFuncionario_TemOFiltroDaEmpresaTambemNaParteDasFotos()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='{EmpresaDoTeste}'", sql);
            string subconsulta = TrechoDaSubconsultaDeFotos(sql);
            Assert.Contains(ParametroEmpresa, subconsulta);
        }

        [Fact]
        public void ConsultaFrotaPaginada_PaginaAntesDoJoinDasFotos()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 20, 10).ToQueryString();

            int posicaoDoLimite = sql.IndexOf(Limite, StringComparison.Ordinal);
            int posicaoDaJuncao = sql.IndexOf(JuncaoDasFotos, StringComparison.Ordinal);
            Assert.True(posicaoDoLimite >= 0, "LIMIT esperado no SQL: " + sql);
            Assert.True(posicaoDoLimite < posicaoDaJuncao, "A paginacao precisa vir antes do join das fotos: " + sql);
        }

        [Fact]
        public void ConsultaFrotaPaginada_TrazSoAPrimeiraFotoAtivaDoVeiculo()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            string subconsulta = TrechoDaSubconsultaDeFotos(sql);
            Assert.Contains(TabelaDeFotos, subconsulta);
            AssertFotoAtivaNaSubconsulta(subconsulta);
            Assert.Contains(ParticaoDaPrimeiraFoto, subconsulta);
            Assert.Matches(OrdemDaPrimeiraFotoNaJanela, subconsulta);
            Assert.Contains(PrimeiraFotoPorVeiculo, subconsulta);
        }

        [Fact]
        public void ConsultaFrotaPaginada_SemContexto_FailClosedComIdZeroEGuardaFalsa()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            VeiculoRepository repositorio = new VeiculoRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaFrotaPaginada(null, null, null, 0, 20).ToQueryString();

            Assert.Contains($"{ParametroEmpresa}='0'", sql);
            Assert.Contains(GuardaFalso, sql);
        }
    }
}
