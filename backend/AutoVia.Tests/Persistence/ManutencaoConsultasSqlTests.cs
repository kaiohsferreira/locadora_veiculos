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
        private const string ValorDosStatusDeLocacaoEmAbertoNoSql = "'EM_ANDAMENTO', 'ATRASADA'";
        private const string StatusFinalizadaNoSql = "FINALIZADA";
        private const string StatusCanceladaNoSql = "CANCELADA";
        private const string TabelaDeFotosNoSql = "veiculo_foto";
        private const string PrimeiraFotoPorVeiculoNoSql = "row <= 1";

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
        public void LocacoesEmAberto_TraduzOsDoisStatusAbertosENaoOsFechados()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaLocacoesEmAbertoDoVeiculo(IdVeiculoDoTeste).ToQueryString();

            Assert.Contains(ValorDosStatusDeLocacaoEmAbertoNoSql, sql);
            Assert.DoesNotContain(StatusFinalizadaNoSql, sql);
            Assert.DoesNotContain(StatusCanceladaNoSql, sql);
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

        private static string ConsultaDaListaSemFiltros(int deslocamento, int quantidade)
        {
            return CriarRepositorioDeManutencao()
                .ConsultaManutencoesFiltradas(null, null, null, null, null, null, deslocamento, quantidade)
                .ToQueryString();
        }

        [Fact]
        public void Listagem_FiltraLocadoraEDisabledAtNaOrdem()
        {
            string sql = ConsultaDaListaSemFiltros(0, 20);

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));

            Match tabelaDaOrdem = Regex.Match(sql, @"FROM manutencao AS (\w+)");

            Assert.True(tabelaDaOrdem.Success, sql);
            string aliasDaOrdem = tabelaDaOrdem.Groups[1].Value;
            Assert.True(Regex.IsMatch(sql, @"\b" + Regex.Escape(aliasDaOrdem) + @"\.disabled_at IS NULL\b"), "A ordem precisa filtrar disabled_at pelo proprio alias: " + sql);
        }

        [Fact]
        public void Listagem_OrdenaPorEntradaDepoisIdDecrescentes()
        {
            string sql = ConsultaDaListaSemFiltros(0, 20);

            Assert.Matches(@"ORDER BY \w+\.data_entrada DESC, \w+\.id_manutencao DESC", sql);
        }

        [Fact]
        public void Listagem_PaginaPorDeslocamentoEQuantidadeComoParametros()
        {
            string sql = ConsultaDaListaSemFiltros(20, 10);

            Match deslocamento = Regex.Match(sql, @"OFFSET (@(\w+)|(\d+))");
            Match quantidade = Regex.Match(sql, @"LIMIT (@(\w+)|(\d+))");

            Assert.True(deslocamento.Success, sql);
            Assert.True(quantidade.Success, sql);
            string valorDoDeslocamento = deslocamento.Groups[2].Success ? ValorDoParametro(sql, deslocamento.Groups[2].Value) : deslocamento.Groups[3].Value;
            string valorDaQuantidade = quantidade.Groups[2].Success ? ValorDoParametro(sql, quantidade.Groups[2].Value) : quantidade.Groups[3].Value;
            Assert.Equal("20", valorDoDeslocamento);
            Assert.Equal("10", valorDaQuantidade);
        }

        [Fact]
        public void Listagem_FiltroDeTipoSituacaoEVeiculoViramPredicados()
        {
            string sql = CriarRepositorioDeManutencao()
                .ConsultaManutencoesFiltradas(null, null, null, TipoManutencao.PREVENTIVA, true, IdVeiculoDoTeste, 0, 20)
                .ToQueryString();

            Assert.Contains(ValorDoTipoPreventivaNoSql, sql);
            Assert.Contains(FiltroDataSaidaNula, sql);
            Assert.Matches(PredicadoDoIdVeiculoNoSql, sql);
            Assert.Equal(IdVeiculoDoTeste.ToString(), ValorDoParametro(sql, Regex.Match(sql, PredicadoDoIdVeiculoNoSql).Groups[1].Value));
        }

        [Fact]
        public void Listagem_SituacaoEncerradaEhDataSaidaPreenchida()
        {
            string sql = CriarRepositorioDeManutencao()
                .ConsultaManutencoesFiltradas(null, null, null, null, false, null, 0, 20)
                .ToQueryString();

            Assert.Contains("data_saida IS NOT NULL", sql);
            Assert.DoesNotContain(FiltroDataSaidaNula, sql);
        }

        [Fact]
        public void Listagem_BuscaCasaPlacaMarcaModeloECodigoDaOrdem()
        {
            string sql = CriarRepositorioDeManutencao()
                .ConsultaManutencoesFiltradas("fiat", "ABC1D23", 5, null, null, null, 0, 20)
                .ToQueryString();

            Assert.Matches(@"(?i)lower\(\w+\.marca\)\s+LIKE", sql);
            Assert.Matches(@"(?i)lower\(\w+\.modelo\)\s+LIKE", sql);
            Assert.Matches(@"(?i)\w+\.placa\s+LIKE", sql);
            Assert.Contains("fiat", sql);
            Assert.Contains("ABC1D23", sql);
            Assert.Matches(@"\.id_manutencao = @(\w+)", sql);
            Assert.Equal("5", ValorDoParametro(sql, Regex.Match(sql, @"\.id_manutencao = @(\w+)").Groups[1].Value));
        }

        [Fact]
        public void Listagem_TrazAFotoDeCapaSoAtivaComTeto()
        {
            string sql = ConsultaDaListaSemFiltros(0, 20);

            Assert.Contains(TabelaDeFotosNoSql, sql);
            Assert.Contains(PrimeiraFotoPorVeiculoNoSql, sql);
            Assert.Matches(@"veiculo_foto AS (\w+)", sql);
            string alias = Regex.Match(sql, @"veiculo_foto AS (\w+)").Groups[1].Value;
            Assert.True(Regex.IsMatch(sql, @"\b" + Regex.Escape(alias) + @"\.disabled_at IS NULL\b"), sql);
        }

        [Fact]
        public void Contagem_FiltraLocadoraEDisabledAtSemPaginar()
        {
            string sql = CriarRepositorioDeManutencao()
                .ConsultaManutencoesFiltradasSemPaginar(null, null, null, null, null, null)
                .ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
            Assert.Contains(FiltroDisabledAtNulo, sql);
            Assert.DoesNotContain(TabelaDeFotosNoSql, sql);
            Assert.DoesNotContain("LIMIT", sql);
        }

        [Fact]
        public void OrdensAbertasDaLocadora_FiltraDataSaidaNulaDisabledAtEEmpresa()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaOrdensAbertas().ToQueryString();

            Assert.Contains(FiltroDataSaidaNula, sql);
            Assert.Contains(FiltroDisabledAtNulo, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void CustoDoPeriodo_LeOCustoDasEncerradasDentroDoPeriodoDaLocadora()
        {
            DateOnly inicio = new DateOnly(2026, 11, 1);
            DateOnly fim = new DateOnly(2026, 11, 30);

            string sql = CriarRepositorioDeManutencao().ConsultaCustoEncerradasNoPeriodo(inicio, fim).ToQueryString();

            Assert.Contains("m.custo", sql);
            Assert.Contains(ColunaDataSaida, sql);
            Assert.Contains("'11/01/2026'", sql);
            Assert.Contains("'11/30/2026'", sql);
            Assert.Contains(FiltroDisabledAtNulo, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
        }

        [Fact]
        public void AlertasDaCapa_FiltraLocadoraDisabledAtPelosIdsEComFoto()
        {
            string sql = CriarRepositorioDeManutencao().ConsultaVeiculosDosAlertas(new List<int> { IdVeiculoDoTeste, 9 }).ToQueryString();

            Assert.Contains(FiltroEmpresa, sql);
            Assert.Equal(EmpresaDoTeste.ToString(), ValorDoParametro(sql, FiltroEmpresa));
            Assert.Contains(FiltroDisabledAtNulo, sql);
            Assert.Matches(@"\.id_veiculo = ANY ?\(", sql);
            Assert.Contains(TabelaDeFotosNoSql, sql);
        }
    }
}
