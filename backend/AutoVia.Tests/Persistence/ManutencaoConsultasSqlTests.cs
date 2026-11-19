using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Persistence
{
    /// <summary>
    /// Consultas da manutencao traduzidas pelo EF sem banco (ToQueryString). Prova: a trava da abertura e do encerramento
    /// tem FOR UPDATE, disabled_at nulo, e id do veiculo e id da locadora como PARAMETROS, e e o MESMO texto da trava da
    /// troca de status; as leituras de reserva e de locacao em aberto sao iguais as da troca de status; a consulta de ordem
    /// aberta filtra veiculo, disabled_at e data_saida nula; e a lista de alertas filtra a locadora, fora VENDIDO e BAIXADO,
    /// e usa a base por subconsulta (preventivas, abertas e encerradas) com o limite de km. NAO prova o resultado no PostgreSQL.
    /// </summary>
    public class ManutencaoConsultasSqlTests
    {
        private const int IdVeiculoDoTeste = 8;
        private const int EmpresaDoTeste = 4242;
        private const int IdFuncionarioDoTeste = 9;
        private const string FiltroEmpresa = "ef_filter__IdEmpresaContexto";
        private const string PredicadoDaTravaNoSql = @"id_veiculo = @(\w+) AND id_empresa = @(\w+) AND disabled_at IS NULL FOR UPDATE";
        private const string PredicadoDoIdVeiculoNoSql = @"\.id_veiculo = @(\w+)";
        private const string TextoDaTravaNoSql = @"SELECT \* FROM veiculo WHERE[^\r\n]*";
        private const string ValorDoTipoPreventivaNoSql = "'PREVENTIVA'";
        private const string ValorDoStatusVendidoNoSql = "'VENDIDO'";
        private const string ValorDoStatusBaixadoNoSql = "'BAIXADO'";
        private const string ValorDoKmAtualNoSql = "km_atual >=";
        private const string ClausulaDeOrdemNoSql = @"ORDER BY[\s\S]*?LIMIT";
        private const string ClausulaDeLimiteNoSql = @"LIMIT (@(\w+)|(\d+))";
        private const string FuncaoDeMaximoSemRegistroNoSql = "COALESCE(";
        private const string ColunaKmAtualNoSql = "km_atual";
        private const string ColunaIdVeiculoNoSql = "id_veiculo";
        private const string SubconsultaDaBaseNoSql = "MAX(";
        private const string FiltroDisabledAtNulo = "disabled_at IS NULL";
        private const string FiltroDataSaidaNula = "data_saida IS NULL";
        private const string ColunaDataSaida = "data_saida";

        private static readonly DbContextOptions<AutoViaDbContext> Opcoes = new DbContextOptionsBuilder<AutoViaDbContext>()
            .UseNpgsql("Host=localhost;Database=autovia_modelo_teste")
            .Options;

        private static AutoViaDbContext CriarContexto()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(EmpresaDoTeste, IdFuncionarioDoTeste, CargoFuncionario.GESTOR_FROTA, false);

            return new AutoViaDbContext(Opcoes, contexto);
        }

        private static ManutencaoRepository CriarRepositorioDeManutencao()
        {
            return new ManutencaoRepository(CriarContexto(), new FakeLogService());
        }

        private static VeiculoRepository CriarRepositorioDeVeiculo()
        {
            return new VeiculoRepository(CriarContexto(), new FakeLogService());
        }

        /// <summary>Valor do parametro no cabecalho do ToQueryString (o filtro e os argumentos aparecem como "-- @nome='valor'").</summary>
        private static string ValorDoParametro(string sql, string nome)
        {
            Match valor = Regex.Match(sql, @"-- @?" + Regex.Escape(nome) + @"='([^']*)'");

            Assert.True(valor.Success, $"O parametro {nome} deveria aparecer no ToQueryString.");

            return valor.Groups[1].Value;
        }

        [Fact]
        public void Trava_TemForUpdateDisabledAtEIdsComoParametros()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaTravaVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Matches(PredicadoDaTravaNoSql, sql);
        }

        [Fact]
        public void Trava_ValoresDosParametrosSaoIdVeiculoEIdEmpresaDoContexto()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaTravaVeiculo(IdVeiculoDoTeste).ToQueryString();
            Match casamento = Regex.Match(sql, PredicadoDaTravaNoSql);

            Assert.True(casamento.Success);
            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, casamento.Groups[1].Value));
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, casamento.Groups[2].Value));
        }

        [Fact]
        public void Trava_EOMesmoTextoDaTravaDaTrocaDeStatus()
        {
            string sqlManutencao = CriarRepositorioDeManutencao().ConsultaTravaVeiculo(IdVeiculoDoTeste).ToQueryString();
            string sqlTroca = CriarRepositorioDeVeiculo().ConsultaTravaStatusVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(Regex.Match(sqlTroca, TextoDaTravaNoSql).Value, Regex.Match(sqlManutencao, TextoDaTravaNoSql).Value);
        }

        [Fact]
        public void Trava_FiltroEmpresaContinuaLigado()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaTravaVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void OrdemAbertaDoVeiculo_FiltraVeiculoDisabledAtEDataSaidaNula()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaManutencoesAbertasDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Matches(PredicadoDoIdVeiculoNoSql, sql);
            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, Regex.Match(sql, PredicadoDoIdVeiculoNoSql).Groups[1].Value));
            Assert.Contains(FiltroDisabledAtNulo, sql);
            Assert.Contains(FiltroDataSaidaNula, sql);
            Assert.Contains(FiltroEmpresa, sql);
        }

        [Fact]
        public void OrdemAbertaDoVeiculo_IgualAConsultaDaTrocaDeStatus()
        {
            string sqlManutencao = CriarRepositorioDeManutencao().ConsultaManutencoesAbertasDoVeiculo(IdVeiculoDoTeste).ToQueryString();
            string sqlTroca = CriarRepositorioDeVeiculo().ConsultaManutencoesAbertasDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(sqlTroca, sqlManutencao);
        }

        [Fact]
        public void ReservasEmAberto_IgualAsDaTrocaDeStatus()
        {
            string sqlManutencao = CriarRepositorioDeManutencao().ConsultaReservasEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();
            string sqlTroca = CriarRepositorioDeVeiculo().ConsultaReservasEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(sqlTroca, sqlManutencao);
        }

        [Fact]
        public void LocacoesEmAberto_IgualAsDaTrocaDeStatus()
        {
            string sqlManutencao = CriarRepositorioDeManutencao().ConsultaLocacoesEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();
            string sqlTroca = CriarRepositorioDeVeiculo().ConsultaLocacoesEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Equal(sqlTroca, sqlManutencao);
        }

        [Fact]
        public void Alertas_FiltraLocadoraEExcluiVendidoEBaixado()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisao().ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
            Assert.Contains(ValorDoStatusVendidoNoSql, sql);
            Assert.Contains(ValorDoStatusBaixadoNoSql, sql);
            Assert.Contains(FiltroDisabledAtNulo, sql);
        }

        [Fact]
        public void Alertas_BaseEhSubconsultaDasPreventivasComMaximoDeKm()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisao().ToQueryString();

            Assert.True(sql.Contains(SubconsultaDaBaseNoSql, StringComparison.OrdinalIgnoreCase), sql);
            Assert.Contains(ValorDoTipoPreventivaNoSql, sql);
            Assert.Contains(ValorDoKmAtualNoSql, sql);
        }

        [Fact]
        public void AlertasOrdenados_TraduzemSemExcecaoEOrdenamPorFaltamEDepoisIdDoVeiculo()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            Match ordem = Regex.Match(sql, ClausulaDeOrdemNoSql);

            Assert.True(ordem.Success, sql);
            Assert.Contains(FuncaoDeMaximoSemRegistroNoSql, ordem.Value);
            Assert.Contains(ColunaKmAtualNoSql, ordem.Value);
            Assert.True(ordem.Value.IndexOf(FuncaoDeMaximoSemRegistroNoSql, StringComparison.Ordinal) < ordem.Value.IndexOf(ColunaIdVeiculoNoSql, StringComparison.Ordinal), ordem.Value);
        }

        [Fact]
        public void AlertasOrdenados_LimiteEhOTetoDaListaParametrizado()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            Match limite = Regex.Match(sql, ClausulaDeLimiteNoSql);

            Assert.True(limite.Success, sql);
            string valorDoLimite = limite.Groups[2].Success
                ? ValorDoParametro(sql, limite.Groups[2].Value)
                : limite.Groups[3].Value;
            Assert.Equal(ConstantsValues.TetoAlertasRevisao.ToString(CultureInfo.InvariantCulture), valorDoLimite);
        }

        [Fact]
        public void AlertasOrdenados_MaximoSemPreventivaVaiParaZeroNoSql()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisaoOrdenada().ToQueryString();

            Assert.Contains(FuncaoDeMaximoSemRegistroNoSql, sql);
            Assert.Matches(@"COALESCE\([\s\S]*?,\s*0\)", sql);
        }

        [Fact]
        public void Alertas_BaseConsideraOrdensAbertasEEncerradas()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaAlertasRevisao().ToQueryString();

            Assert.DoesNotContain(ColunaDataSaida, sql);
        }
    }
}
