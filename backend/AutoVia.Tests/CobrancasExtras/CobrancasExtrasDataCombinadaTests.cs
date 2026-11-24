using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.CobrancasExtras;

/// <summary>
/// Caso real da mudanca B1: a reserva guarda so a data, entao a retirada e as 10h e a previsao gravada e o dia 20/11 a meia-noite.
/// A devolucao prevista efetiva e 20/11 as 10h; o atraso so conta depois da tolerancia dessa hora. Sem PostgreSQL: repositorio de mentira.
/// </summary>
public sealed class CobrancasExtrasDataCombinadaTests
{
    private static readonly DateTime Retirada = new DateTime(2026, 11, 18, 10, 0, 0);
    private static readonly DateTime PrevistaGravada = new DateTime(2026, 11, 20, 0, 0, 0);

    private static ApuracaoCobrancasExtrasService Montar(DateTime retirada, DateTime saida, DateTime previstaGravada,
        int toleranciaMinutos, string unidade, string fracionamento, out FakeCobrancasExtrasRepository repositorio)
    {
        FakeLogService log = new FakeLogService();
        ContextoEmpresa contexto = new ContextoEmpresa(log);
        contexto.Definir(1, 20, CargoFuncionario.ADMINISTRADOR, false);
        repositorio = new FakeCobrancasExtrasRepository(contexto);
        FakeLocacaoRepository locacoes = new FakeLocacaoRepository();
        locacoes.Adicionar(new Funcionario { IdFuncionario = 20, IdEmpresa = 1, Ativo = true });
        Locacao locacao = new Locacao
        {
            IdLocacao = 10, DataRetirada = retirada, DataDevolucaoPrevista = previstaGravada, KmSaida = 100,
            Status = StatusLocacao.EM_ANDAMENTO, Veiculo = new Veiculo { IdVeiculo = 1, IdEmpresa = 1, IdCategoria = 1 }
        };
        locacao.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DataHora = retirada, KmRegistrado = 100, NivelCombustivel = 1m });
        locacao.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, DataHora = saida, KmRegistrado = 200, NivelCombustivel = 0.5m, ComparacaoRealizada = true });
        repositorio.Locacoes.Add(locacao);
        repositorio.Configuracoes.Add(new ConfiguracaoCobrancaExtra
        {
            IdConfiguracao = 1, IdCategoria = 1, IdEmpresa = 1, Versao = 1, Categoria = new Categoria { IdCategoria = 1, Nome = "Compacto" },
            BaseFranquia = ConstantsMessage.CobrancasExtrasTotal, FranquiaKm = 50, ValorKmExcedente = 2m, ValorTanqueCompleto = 200m,
            ToleranciaAtrasoMinutos = toleranciaMinutos, UnidadeAtraso = unidade, FracionamentoAtraso = fracionamento,
            ValorUnidadeAtraso = 20m, BaseAvarias = ConstantsMessage.CobrancasExtrasEstimativaVistoria
        });
        MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<CobrancasExtrasProfile>(), NullLoggerFactory.Instance);
        CobrancasExtrasService calculadora = new CobrancasExtrasService(locacoes, new FakeVistoriaRepository(), new FakeVistoriaSaidaRepository(), log);
        FakeRelogio relogio = new FakeRelogio(saida.AddHours(1));

        return new ApuracaoCobrancasExtrasService(repositorio, locacoes, calculadora, contexto, relogio, configuracao.CreateMapper(), log, new FakePagamentoService());
    }

    [Theory]
    [InlineData(20, 9, 0, 30, 0)]
    [InlineData(20, 10, 0, 30, 0)]
    [InlineData(20, 10, 30, 30, 0)]
    [InlineData(20, 11, 0, 30, 10)]
    [InlineData(20, 11, 0, 60, 0)]
    [InlineData(22, 10, 0, 30, 950)]
    public async Task AtrasoContaAPartirDaDataCombinadaNaHoraDaRetirada(int diaSaida, int horaSaida, int minutoSaida, int tolerancia, int valorEsperado)
    {
        DateTime saida = new DateTime(2026, 11, diaSaida, horaSaida, minutoSaida, 0);
        ApuracaoCobrancasExtrasService servico = Montar(Retirada, saida, PrevistaGravada, tolerancia, ConstantsMessage.CobrancasExtrasHora,
            ConstantsMessage.CobrancasExtrasProporcional, out FakeCobrancasExtrasRepository _);

        ReturnSimulacaoCobrancasExtrasVO resultado = await servico.SimularAsync(10);

        Assert.Equal((decimal)valorEsperado, resultado.Atraso.Valor);
        Assert.Equal(new DateTime(2026, 11, 20, 10, 0, 0), resultado.Atraso.DataPrevista);
        Assert.Equal("20/11/2026 10:00", resultado.Atraso.DataPrevistaStr);
    }

    [Fact]
    public async Task UmaHoraDepoisForaDaToleranciaEIniciadaCobraUmaUnidadeInteira()
    {
        DateTime saida = new DateTime(2026, 11, 20, 11, 0, 0);
        ApuracaoCobrancasExtrasService servico = Montar(Retirada, saida, PrevistaGravada, 30, ConstantsMessage.CobrancasExtrasHora,
            ConstantsMessage.CobrancasExtrasIniciada, out FakeCobrancasExtrasRepository _);

        ReturnSimulacaoCobrancasExtrasVO resultado = await servico.SimularAsync(10);

        Assert.Equal(20m, resultado.Atraso.Valor);
    }

    [Fact]
    public async Task DiariasPrevistasSaemDaDataCombinadaEBatemComAReserva()
    {
        ApuracaoCobrancasExtrasService servico = Montar(Retirada, new DateTime(2026, 11, 20, 10, 0, 0), PrevistaGravada, 30,
            ConstantsMessage.CobrancasExtrasHora, ConstantsMessage.CobrancasExtrasProporcional, out FakeCobrancasExtrasRepository _);

        ReturnSimulacaoCobrancasExtrasVO resultado = await servico.SimularAsync(10);

        Assert.Equal(2, resultado.DiariasPrevistas);
    }

    [Theory]
    [InlineData(20, 10)]
    [InlineData(21, 10)]
    public async Task LocacaoAbertaNoDiaPrevistoOuDepoisNaoTemSimulacao(int diaRetirada, int horaRetirada)
    {
        DateTime retirada = new DateTime(2026, 11, diaRetirada, horaRetirada, 0, 0);
        ApuracaoCobrancasExtrasService servico = Montar(retirada, retirada.AddHours(1), PrevistaGravada, 30,
            ConstantsMessage.CobrancasExtrasHora, ConstantsMessage.CobrancasExtrasProporcional, out FakeCobrancasExtrasRepository _);

        await Assert.ThrowsAsync<BusinessException>(() => servico.SimularAsync(10));
    }

    [Fact]
    public async Task SnapshotGuardaAPrevistaBrutaEVersaoUm()
    {
        ApuracaoCobrancasExtrasService servico = Montar(Retirada, new DateTime(2026, 11, 20, 11, 0, 0), PrevistaGravada, 30,
            ConstantsMessage.CobrancasExtrasHora, ConstantsMessage.CobrancasExtrasProporcional, out FakeCobrancasExtrasRepository repositorio);

        await servico.RegistrarAsync(10, new ApurarCobrancasExtrasVO { IdConfiguracao = 1, VersaoEsperada = 1 });

        ApuracaoCobrancaExtra apuracao = repositorio.Apuracoes.Single();
        Assert.Equal(1, apuracao.VersaoSnapshot);
        Assert.Contains("\"DataPrevista\":\"2026-11-20T00:00:00\"", apuracao.SnapshotJson);
    }
}
