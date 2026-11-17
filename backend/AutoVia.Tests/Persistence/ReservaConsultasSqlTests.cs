using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas reais do ReservaRepository traduzidas pelo EF sem banco (ToQueryString). Prova o predicado de cada ponto
    /// que atravessa o filtro "Empresa": posse do cliente (WHERE com o id do dono, ou FALSE para outro cliente e para
    /// contexto zerado), conflito de periodo (veiculo e status, sem empresa e sem posse), locadora ativa do veiculo e a
    /// trava do veiculo (FOR UPDATE, sem parametro de empresa). NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class ReservaConsultasSqlTests
    {
        private const int IdClienteDoTeste = 1;
        private const int IdClienteAlheio = 2;
        private const int IdVeiculoDoTeste = 8;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string ParametroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string ParametroCliente = "ef_filter__IdClienteContexto";
        private const string PosseNegadaNoSql = "WHERE FALSE";
        private const string PredicadoDoDonoNoSql = @"WHERE r\.id_cliente = (@\S+) AND r\.disabled_at IS NULL";
        private const string PredicadoDoVeiculoNoConflito = @"WHERE r\.id_veiculo = (@\S+) AND r\.disabled_at IS NULL";
        private const string StatusDoConflito = "r.status IN ('ATIVA', 'CONFIRMADA')";
        private const string PredicadoDaLocadoraNoSql = @"WHERE v\.id_veiculo = (@\S+) AND v\.disabled_at IS NULL AND e\.ativo AND e\.disabled_at IS NULL";
        private const string PredicadoDaTravaNoSql = @"WHERE id_veiculo = (@\S+) AND disabled_at IS NULL FOR UPDATE";
        private const string ClienteEmConflito = "r.id_cliente =";
        private const string TabelaDeFotos = "veiculo_foto";
        private const string FromDaTabelaDeFotos = "FROM veiculo_foto";
        private const string JuncaoEsquerdaDasFotos = "LEFT JOIN";
        private const string ParticaoDaPrimeiraFoto = "ROW_NUMBER() OVER(PARTITION BY";
        private const string OrdemDaPrimeiraFotoNaJanela = @"ROW_NUMBER\(\) OVER\(PARTITION BY \w+\.id_veiculo ORDER BY \w+\.ordem, \w+\.id_veiculo_foto\)";
        private const string LimiteDaPrimeiraFoto = "row <= 1";

        /// <summary>Alias da tabela veiculo_foto na subconsulta da capa (ex.: "FROM veiculo_foto AS v0").</summary>
        private static readonly Regex AliasDaTabelaDeFotos = new Regex(@"FROM veiculo_foto AS (\w+)", RegexOptions.CultureInvariant);

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

        /// <summary>Le, no WHERE do SQL, o nome do parametro que o predicado usa (grupo 1 do padrao).</summary>
        private static string ExtrairParametro(string sql, string padrao)
        {
            Match casamento = Regex.Match(sql, padrao);

            Assert.True(casamento.Success, "Predicado esperado nao encontrado no SQL: " + sql);

            return casamento.Groups[1].Value;
        }

        /// <summary>
        /// Subconsulta da capa: do parentese que abre a subconsulta do LEFT JOIN (o SELECT das fotos, com a janela
        /// ROW_NUMBER, o FROM veiculo_foto e o filtro row &lt;= 1) ate o parentese que a fecha. Fica de fora o WHERE de
        /// reserva e veiculo (antes do LEFT JOIN) e o WHERE final do cliente (depois dele), que tambem tem disabled_at IS NULL.
        /// </summary>
        private static string TrechoDaSubconsultaDeFotos(string sql)
        {
            int inicioDaJuncao = sql.IndexOf(JuncaoEsquerdaDasFotos, StringComparison.Ordinal);

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
        public void ConsultaDoClienteLogado_NaoTemParametroDeEmpresa_MesmoComOsIncludesDeVeiculoCategoriaELocadora()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.Contains("INNER JOIN veiculo", sql);
            Assert.Contains("INNER JOIN categoria", sql);
            Assert.Contains("INNER JOIN empresa", sql);
        }

        [Fact]
        public void ConsultaDoClienteLogado_NaoTrazAFotoDoVeiculo()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain("veiculo_foto", sql);
        }

        [Fact]
        public void ConsultaDoClienteLogadoComCapa_TrazAPrimeiraFotoSemParametroDeEmpresaEComPosseDoCliente()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogadoComCapa(IdClienteDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            string subconsulta = TrechoDaSubconsultaDeFotos(sql);
            Assert.Contains(TabelaDeFotos, subconsulta);
            AssertFotoAtivaNaSubconsulta(subconsulta);
            Assert.Contains(ParticaoDaPrimeiraFoto, subconsulta);
            Assert.Matches(OrdemDaPrimeiraFotoNaJanela, subconsulta);
            Assert.Contains(LimiteDaPrimeiraFoto, subconsulta);
            string parametro = ExtrairParametro(sql, PredicadoDoDonoNoSql);
            Assert.Contains($"{parametro.TrimStart('@')}='{IdClienteDoTeste}'", sql);
        }

        [Fact]
        public void ConsultaDoClienteLogadoComCapa_OutroCliente_WhereFalseSemParametroDeEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogadoComCapa(IdClienteAlheio).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void ConsultaDoClienteLogado_MesmoDono_ContextoPositivoIgualAoIdPedido_WhereTrazOIdDoDono()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogado(IdClienteDoTeste).ToQueryString();

            string parametro = ExtrairParametro(sql, PredicadoDoDonoNoSql);
            Assert.Contains($"{parametro.TrimStart('@')}='{IdClienteDoTeste}'", sql);
            Assert.DoesNotContain(PosseNegadaNoSql, sql);
        }

        [Fact]
        public void ConsultaDoClienteLogado_OutroCliente_ContextoDiferenteDoIdPedido_NaoTemPredicadoDeClienteEWhereFalse()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogado(IdClienteAlheio).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
            Assert.DoesNotContain(ClienteEmConflito, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void ConsultaDoClienteLogado_SemClienteNoContexto_WhereFalseMesmoComIdPedidoPositivo()
        {
            using AutoViaDbContext db = CriarDbSemContexto();
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoClienteLogado(IdClienteDoTeste).ToQueryString();

            Assert.Contains(PosseNegadaNoSql, sql);
            Assert.DoesNotMatch(PredicadoDoDonoNoSql, sql);
            Assert.DoesNotContain(ParametroEmpresa, sql);
        }

        [Fact]
        public void ConflitoDePeriodo_SemParametroDeEmpresaNemDePosse_FiltraVeiculoEStatus()
        {
            using AutoViaDbContext db = CriarDbComContextoDeCliente(IdClienteDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaReservasDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.DoesNotContain(ParametroCliente, sql);
            Assert.DoesNotContain(ClienteEmConflito, sql);
            string parametroVeiculo = ExtrairParametro(sql, PredicadoDoVeiculoNoConflito);
            Assert.Contains($"{parametroVeiculo.TrimStart('@')}='{IdVeiculoDoTeste}'", sql);
            Assert.Contains(StatusDoConflito, sql);
        }

        [Fact]
        public void LocadoraAtivaDoVeiculo_SemParametroDeEmpresa_ExigeEmpresaAtivaENaoDesativada()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaVeiculoParaReserva(IdVeiculoDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.Contains("INNER JOIN empresa", sql);
            string parametroVeiculo = ExtrairParametro(sql, PredicadoDaLocadoraNoSql);
            Assert.Contains($"{parametroVeiculo.TrimStart('@')}='{IdVeiculoDoTeste}'", sql);
        }

        [Fact]
        public void TravaDoVeiculo_UsaForUpdateComOIdDoVeiculoESemParametroDeEmpresa()
        {
            using AutoViaDbContext db = CriarDbComContextoDeFuncionario(EmpresaDoTeste);
            ReservaRepository repositorio = new ReservaRepository(db, new FakeLogService());

            string sql = repositorio.ConsultaTravaDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.DoesNotContain(ParametroEmpresa, sql);
            Assert.DoesNotContain("id_empresa", sql);
            string parametroTrava = ExtrairParametro(sql, PredicadoDaTravaNoSql);
            Assert.Contains($"{parametroTrava.TrimStart('@')}='{IdVeiculoDoTeste}'", sql);
        }
    }
}
