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
    /// Consultas da troca manual de status (P1-BE-04) traduzidas pelo EF sem banco (ToQueryString). Prova: a trava tem FOR UPDATE,
    /// disabled_at nulo, e id do veiculo e id da locadora como PARAMETROS (nao como texto), com o VALOR de cada parametro
    /// conferido pelo cabecalho do ToQueryString; o filtro "Empresa" continua ligado e com a locadora do teste; as leituras de
    /// reserva e de locacao em aberto filtram veiculo (com o valor certo), disabled_at, a locadora e o conjunto de status certo.
    /// NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class VeiculoStatusConsultasSqlTests
    {
        private const int IdVeiculoDoTeste = 8;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string DisabledAtNulo = "disabled_at IS NULL";
        private const string PredicadoDaTravaNoSql = @"id_veiculo = @(\w+) AND id_empresa = @(\w+) AND disabled_at IS NULL FOR UPDATE";
        private const string PredicadoDoIdVeiculoDeReservaNoSql = @"r\.id_veiculo = @(\w+)";
        private const string PredicadoDoIdVeiculoDeLocacaoNoSql = @"l\.id_veiculo = @(\w+)";
        private const string PredicadoDoStatusDeReservaNoSql = @"r\.status = ANY \((@\w+)\)";
        private const string PredicadoDoStatusDeLocacaoNoSql = @"l\.status = ANY \((@\w+)\)";
        private const string ValorDoStatusDeReservaNoSql = @"'ATIVA', 'CONFIRMADA'";
        private const string ValorDoStatusDeLocacaoNoSql = @"'EM_ANDAMENTO', 'ATRASADA'";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static VeiculoRepository CriarRepositorio()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.GESTOR_FROTA, false);
            AutoViaDbContext db = new AutoViaDbContext(Opcoes, contexto);

            return new VeiculoRepository(db, new FakeLogService());
        }

        /// <summary>
        /// Valor do parametro no cabecalho do ToQueryString. O filtro vem como "-- @nome='valor'" e os argumentos do SQL bruto
        /// (FromSqlRaw) como "-- nome='valor'", sem o arroba. Falha se o parametro nao aparecer.
        /// </summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @?" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        /// <summary>Nome do parametro ligado a uma coluna no SQL (sem o @). Falha se a coluna nao tiver parametro.</summary>
        private static string NomeDoParametro(string sql, string predicado)
        {
            Match casamento = Regex.Match(sql, predicado);

            Assert.True(casamento.Success, $"O predicado {predicado} deveria aparecer no SQL.");

            return casamento.Groups[1].Value;
        }

        [Fact]
        public void Trava_TemForUpdateEDisabledAtNuloEIdVeiculoEIdEmpresaComoParametros()
        {
            string sql = CriarRepositorio().ConsultaTravaStatusVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Matches(PredicadoDaTravaNoSql, sql);
        }

        [Fact]
        public void Trava_ValoresDosParametrosSaoIdVeiculoEIdEmpresaDoContexto()
        {
            string sql = CriarRepositorio().ConsultaTravaStatusVeiculo(IdVeiculoDoTeste).ToQueryString();
            Match casamento = Regex.Match(sql, PredicadoDaTravaNoSql);

            Assert.True(casamento.Success);
            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, casamento.Groups[1].Value));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, casamento.Groups[2].Value));
        }

        [Fact]
        public void Trava_FiltroEmpresaContinuaLigado()
        {
            string sql = CriarRepositorio().ConsultaTravaStatusVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void LeituraDeReservasEmAberto_FiltraVeiculoDisabledAtEmpresaEOsDoisStatus()
        {
            string sql = CriarRepositorio().ConsultaReservasEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Matches(PredicadoDoStatusDeReservaNoSql, sql);
            Assert.Contains(ValorDoStatusDeReservaNoSql, sql);
            Assert.Contains("r.id_veiculo = @", sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains(FiltroEmpresa, sql);
        }

        [Fact]
        public void LeituraDeReservasEmAberto_ValoresDoVeiculoEDaLocadoraSaoOsDoTeste()
        {
            string sql = CriarRepositorio().ConsultaReservasEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, NomeDoParametro(sql, PredicadoDoIdVeiculoDeReservaNoSql)));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void LeituraDeLocacoesEmAberto_FiltraVeiculoDisabledAtEmpresaEOsDoisStatus()
        {
            string sql = CriarRepositorio().ConsultaLocacoesEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Matches(PredicadoDoStatusDeLocacaoNoSql, sql);
            Assert.Contains(ValorDoStatusDeLocacaoNoSql, sql);
            Assert.Contains("l.id_veiculo = @", sql);
            Assert.Contains(DisabledAtNulo, sql);
            Assert.Contains(FiltroEmpresa, sql);
        }

        [Fact]
        public void LeituraDeLocacoesEmAberto_ValoresDoVeiculoEDaLocadoraSaoOsDoTeste()
        {
            string sql = CriarRepositorio().ConsultaLocacoesEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, NomeDoParametro(sql, PredicadoDoIdVeiculoDeLocacaoNoSql)));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }
    }
}
