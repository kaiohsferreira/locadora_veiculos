using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;

namespace AutoVia.Tests.CobrancasExtras;

public sealed class CobrancasExtrasServiceTests
{
    private static readonly DateTime Inicio = new DateTime(2026, 11, 18, 8, 0, 0);
    private readonly FakeLocacaoRepository _locacoes = new FakeLocacaoRepository();
    private readonly FakeVistoriaSaidaRepository _saidas = new FakeVistoriaSaidaRepository();
    private readonly FakeLogService _log = new FakeLogService();
    private readonly CobrancasExtrasService _service;

    public CobrancasExtrasServiceTests()
    {
        _locacoes.Adicionar(new Locacao { IdLocacao = 10, KmSaida = 100, DataRetirada = Inicio, DataDevolucaoPrevista = Inicio.AddHours(2) });
        _saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DataHora = Inicio, KmRegistrado = 100, NivelCombustivel = 1m });
        _saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, DataHora = Inicio.AddHours(4), KmRegistrado = 200, NivelCombustivel = 0.5m, ComparacaoRealizada = true });
        _saidas.Avarias.Add(new Avaria { IdAvaria = 1, IdVistoria = 2, IdAvariaEntrada = 99, ValorEstimado = null });
        _saidas.Avarias.Add(new Avaria { IdAvaria = 2, IdVistoria = 2, Descricao = "Dano novo", ValorEstimado = 30m });
        _service = new CobrancasExtrasService(_locacoes, _saidas.Vistorias, _saidas, _log);
    }

    public static SimularCobrancasExtrasVO Parametros() => new SimularCobrancasExtrasVO
    {
        PacoteKmTotal = 50, ValorKmExcedente = 2m, ValorTanqueCompleto = 200m,
        ToleranciaAtrasoMinutos = 30, UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora,
        FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional, ValorUnidadeAtraso = 20m
    };

    [Fact]
    public async Task CalculaQuatroCategoriasSemMutacaoERepeteResultado()
    {
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, Parametros());
        Assert.Equal(100m, resultado.Quilometragem.Valor);
        Assert.Equal(100m, resultado.Combustivel.Valor);
        Assert.Equal(1.5m, resultado.Atraso.UnidadesAposTolerancia);
        Assert.Equal(30m, resultado.Atraso.Valor);
        Assert.Equal(30m, resultado.Avarias.Valor);
        Assert.Single(resultado.Avarias.Itens);
        Assert.Equal(260m, resultado.Total);
        Assert.True(resultado.Provisorio);
        Assert.True(resultado.Simulacao);
        Assert.Equal(ConstantsMessage.AvisoCobrancasExtrasSimulacao, resultado.Aviso);
        Assert.Equal("18/11/2026 10:00", resultado.Atraso.DataPrevistaStr);
        ReturnSimulacaoCobrancasExtrasVO repetida = await _service.SimularAsync(10, Parametros());
        Assert.Equal(resultado.Total, repetida.Total);
        Locacao locacao = (await _locacoes.FindByIdAsync(10))!;
        Assert.Null(locacao.ValorTotal);
        Assert.Null(locacao.KmChegada);
        Assert.Null(locacao.DataDevolucaoEfetiva);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, locacao.Status);
        Assert.Equal(2, _saidas.Vistorias.Vistorias.Count);
        Assert.Equal(2, _saidas.Avarias.Count);
        Assert.Equal(0, _locacoes.ChamadasStartFromReserva);
        Assert.Equal(200, _saidas.Vistorias.Vistorias[1].KmRegistrado);
    }

    [Theory]
    [InlineData("HORA", "INICIADA", 2, 40)]
    [InlineData("DIARIA", "INICIADA", 1, 20)]
    [InlineData("DIARIA", "PROPORCIONAL", 0.0625, 1.25)]
    public async Task UnidadeExataEDescontoAntesDoFracionamento(string unidade, string fracionamento, double unidades, double valor)
    {
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.UnidadeAtraso = unidade;
        parametros.FracionamentoAtraso = fracionamento;
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal((decimal)unidades, resultado.Atraso.UnidadesCobradas);
        Assert.Equal((decimal)valor, resultado.Atraso.Valor);
    }

    [Fact]
    public async Task ZerosLegitimosEToleranciaMaximaSemOverflow()
    {
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.PacoteKmTotal = int.MaxValue;
        parametros.ToleranciaAtrasoMinutos = int.MaxValue;
        _saidas.Vistorias.Vistorias[1].NivelCombustivel = 1m;
        _saidas.Avarias.Clear();
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal(0m, resultado.Total);
        Assert.Equal(0m, resultado.Atraso.UnidadesCobradas);
        Assert.Empty(resultado.Avarias.Itens);
        Assert.True(resultado.Avarias.ValoresEstimados);
    }

    [Fact]
    public async Task ArredondaCadaCategoriaAwayFromZeroETotalSomaMemorias()
    {
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.ValorTanqueCompleto = 0.01m;
        parametros.ValorUnidadeAtraso = 0.01m;
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal(0.01m, resultado.Combustivel.Valor);
        Assert.Equal(0.02m, resultado.Atraso.Valor);
        Assert.Equal(130.03m, resultado.Total);
    }

    [Fact]
    public async Task LimitePorCategoriaPermiteTotalSuperior()
    {
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.PacoteKmTotal = 99;
        parametros.ValorKmExcedente = 99999999.99m;
        parametros.ValorTanqueCompleto = 99999999.99m;
        parametros.ToleranciaAtrasoMinutos = 60;
        parametros.ValorUnidadeAtraso = 99999999.99m;
        _saidas.Vistorias.Vistorias[1].NivelCombustivel = 0m;
        _saidas.Avarias[1].ValorEstimado = 99999999.99m;
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal(399999999.96m, resultado.Total);
        parametros.PacoteKmTotal = 98;
        await Assert.ThrowsAsync<BusinessException>(() => _service.SimularAsync(10, parametros));
    }

    [Theory]
    [InlineData("HORA", 50, 0, 303, 253)]
    [InlineData("HORA", 50, -1, 303, 252)]
    [InlineData("HORA", 50, 1, 303, 253)]
    [InlineData("HORA", 10, 0, 303, 51)]
    [InlineData("HORA", 70, 0, 303, 354)]
    [InlineData("HORA", 50, 0, 309, 258)]
    [InlineData("DIARIA", 1200, 0, 303, 253)]
    [InlineData("DIARIA", 1200, -1, 303, 252)]
    [InlineData("DIARIA", 1200, 1, 303, 253)]
    public async Task AtrasoProporcionalPreservaMeioCentavoESuaVizinhanca(string unidade, int minutos, int ajusteTicks, int tarifaCentavos, int esperadoCentavos)
    {
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.ToleranciaAtrasoMinutos = 0;
        parametros.UnidadeAtraso = unidade;
        parametros.FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional;
        parametros.ValorUnidadeAtraso = tarifaCentavos / 100m;
        _saidas.Vistorias.Vistorias[1].DataHora = Inicio.AddHours(2).AddMinutes(minutos).AddTicks(ajusteTicks);
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal(esperadoCentavos / 100m, resultado.Atraso.Valor);
        Assert.Equal(resultado.Quilometragem.Valor + resultado.Combustivel.Valor + resultado.Avarias.Valor + resultado.Atraso.Valor, resultado.Total);
    }

    [Fact]
    public async Task AtrasoUmTickNaoUsaDoubleEUnidadeIniciadaCobraUma()
    {
        _saidas.Vistorias.Vistorias[1].DataHora = Inicio.AddHours(2).AddTicks(1);
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.ToleranciaAtrasoMinutos = 0;
        parametros.FracionamentoAtraso = ConstantsMessage.CobrancasExtrasIniciada;
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, parametros);
        Assert.Equal(1m / TimeSpan.TicksPerHour, resultado.Atraso.UnidadesAposTolerancia);
        Assert.Equal(1m, resultado.Atraso.UnidadesCobradas);
        Assert.Equal(20m, resultado.Atraso.Valor);
    }

    [Fact]
    public async Task SomaAvariasExcedeLimiteMesmoComItensValidos()
    {
        _saidas.Avarias[1].ValorEstimado = 99999999.99m;
        _saidas.Avarias.Add(new Avaria { IdAvaria = 3, IdVistoria = 2, ValorEstimado = 0.01m });
        await Assert.ThrowsAsync<BusinessException>(() => _service.SimularAsync(10, Parametros()));
    }

    [Fact]
    public async Task KmMaximoETarifaMaximaBloqueiamSemOverflow()
    {
        _saidas.Vistorias.Vistorias[1].KmRegistrado = int.MaxValue;
        SimularCobrancasExtrasVO parametros = Parametros();
        parametros.ValorKmExcedente = 99999999.99m;
        await Assert.ThrowsAsync<BusinessException>(() => _service.SimularAsync(10, parametros));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public async Task BloqueiaLeiturasInconsistentes(int caso)
    {
        Vistoria entrada = _saidas.Vistorias.Vistorias[0];
        Vistoria saida = _saidas.Vistorias.Vistorias[1];
        switch (caso)
        {
            case 0: saida.ComparacaoRealizada = false; break;
            case 1: entrada.NivelCombustivel = null; break;
            case 2: saida.NivelCombustivel = -0.1m; break;
            case 3: saida.NivelCombustivel = 1.1m; break;
            case 4: saida.NivelCombustivel = 0.001m; break;
            case 5: entrada.KmRegistrado = -1; break;
            case 6: saida.KmRegistrado = 99; break;
            case 7: saida.DataHora = Inicio.AddTicks(-1); break;
            case 8: entrada.DataHora = Inicio.AddTicks(-1); break;
            case 9: _saidas.Avarias[1].ValorEstimado = null; break;
            case 10: _saidas.Avarias[1].ValorEstimado = -1; break;
            case 11: _saidas.Avarias[1].ValorEstimado = 0.001m; break;
            case 12: _saidas.Avarias[1].ValorEstimado = 100000000m; break;
            case 13: saida.DisabledAt = Inicio; break;
            case 14: entrada.DisabledAt = Inicio; break;
        }
        await Assert.ThrowsAsync<BusinessException>(() => _service.SimularAsync(10, Parametros()));
    }

    [Fact]
    public async Task AvariaNovaComValorZeroNaoRecusaENaoSomaNaCobranca()
    {
        _saidas.Avarias[1].ValorEstimado = 0m;
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10, Parametros());
        Assert.Equal(0m, resultado.Avarias.Valor);
        Assert.Equal(0m, Assert.Single(resultado.Avarias.Itens).ValorEstimado);
        Assert.Equal(230m, resultado.Total);
    }

    [Fact]
    public async Task EntradaInvalidaNoServico404SemLocacao()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.SimularAsync(10, new SimularCobrancasExtrasVO()));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SimularAsync(999, Parametros()));
    }
}
