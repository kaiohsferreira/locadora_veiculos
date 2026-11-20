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
using System.Text.Json;

namespace AutoVia.Tests.CobrancasExtras;

public sealed class ApuracaoCobrancasExtrasTests
{
    private static readonly DateTime Inicio = new DateTime(2026, 11, 18, 8, 0, 0);
    private readonly FakeLogService _log = new FakeLogService();
    private readonly ContextoEmpresa _contexto;
    private readonly FakeCobrancasExtrasRepository _repository;
    private readonly FakeLocacaoRepository _locacoes = new FakeLocacaoRepository();
    private readonly ApuracaoCobrancasExtrasService _service;
    private readonly ConfiguracoesCobrancasExtrasService _configService;
    private readonly Locacao _locacao;
    private readonly ConfiguracaoCobrancaExtra _config;
    public ApuracaoCobrancasExtrasTests()
    {
        _contexto = new ContextoEmpresa(_log); _contexto.Definir(1, 20, CargoFuncionario.ADMINISTRADOR, false);
        _repository = new FakeCobrancasExtrasRepository(_contexto);
        _locacoes.Adicionar(new Funcionario { IdFuncionario = 20, IdEmpresa = 1, Ativo = true });
        _locacao = new Locacao { IdLocacao = 10, DataRetirada = Inicio, DataDevolucaoPrevista = Inicio.AddHours(2), KmSaida = 100,
            Status = StatusLocacao.EM_ANDAMENTO, Veiculo = new Veiculo { IdVeiculo = 1, IdEmpresa = 1, IdCategoria = 1 } };
        _locacao.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DataHora = Inicio, KmRegistrado = 100, NivelCombustivel = 1m });
        _locacao.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, DataHora = Inicio.AddHours(4), KmRegistrado = 200, NivelCombustivel = 0.5m, ComparacaoRealizada = true });
        _repository.Locacoes.Add(_locacao);
        _config = new ConfiguracaoCobrancaExtra
        {
            IdConfiguracao = 1, IdCategoria = 1, IdEmpresa = 1, Versao = 1, Categoria = new Categoria { IdCategoria = 1, Nome = "Compacto" },
            BaseFranquia = ConstantsMessage.CobrancasExtrasTotal, FranquiaKm = 50, ValorKmExcedente = 2m,
            ValorTanqueCompleto = 200m, ToleranciaAtrasoMinutos = 30, UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora,
            FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional, ValorUnidadeAtraso = 20m,
            BaseAvarias = ConstantsMessage.CobrancasExtrasEstimativaVistoria, UpdatedAt = Inicio
        };
        _repository.Configuracoes.Add(_config);
        MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<CobrancasExtrasProfile>(), NullLoggerFactory.Instance);
        CobrancasExtrasService calculadora = new CobrancasExtrasService(_locacoes, new FakeVistoriaRepository(), new FakeVistoriaSaidaRepository(), _log);
        FakeRelogio relogio = new FakeRelogio(Inicio.AddHours(5));
        _service = new ApuracaoCobrancasExtrasService(_repository, _locacoes, calculadora, _contexto, relogio, configuracao.CreateMapper(), _log);
        FakeCategoriaRepository categorias = new FakeCategoriaRepository(); categorias.Adicionar(_config.Categoria);
        _configService = new ConfiguracoesCobrancasExtrasService(_repository, categorias, _contexto, relogio, configuracao.CreateMapper(), _log);
    }
    private static ApurarCobrancasExtrasVO Pedido() => new ApurarCobrancasExtrasVO { IdConfiguracao = 1, VersaoEsperada = 1 };
    private static ConfiguracaoCobrancaExtraVO Valores() => new ConfiguracaoCobrancaExtraVO
    {
        IdCategoria = 1, BaseFranquia = ConstantsMessage.CobrancasExtrasTotal, FranquiaKm = 50, ValorKmExcedente = 2m,
        ValorTanqueCompleto = 200m, ToleranciaAtrasoMinutos = 30, UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora,
        FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional, ValorUnidadeAtraso = 20m,
        BaseAvarias = ConstantsMessage.CobrancasExtrasEstimativaVistoria, VersaoEsperada = 1
    };

    [Fact]
    public async Task AtrasoCombustivelEKmGeramExatamenteTresLancamentosSemTotalManual()
    {
        ReturnSimulacaoCobrancasExtrasVO previa = await _service.SimularAsync(10);
        Assert.Empty(_repository.Apuracoes);
        ResultadoRegistroApuracaoVO registro = await _service.RegistrarAsync(10, Pedido());
        Assert.True(registro.Criada); Assert.Equal(230m, registro.Apuracao.Total);
        Assert.Equal(previa.Total, registro.Apuracao.Total); Assert.Equal(3, registro.Apuracao.Cobrancas.Count);
        Assert.Equal(new[] { "KM_EXCEDENTE", "COMBUSTIVEL", "ATRASO" }.Order(), registro.Apuracao.Cobrancas.Select(x => x.Tipo).Order());
        Assert.All(registro.Apuracao.Cobrancas, x => { Assert.True(x.IdCobranca > 0); Assert.True(x.Valor > 0); });
        Assert.Equal(registro.Apuracao.Total, registro.Apuracao.Cobrancas.Sum(x => x.Valor));
        Assert.False(registro.Apuracao.Calculo.Provisorio); Assert.False(registro.Apuracao.Simulacao);
        Assert.Null(_locacao.ValorTotal); Assert.Null(_locacao.DataDevolucaoEfetiva); Assert.Null(_locacao.KmChegada);
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, _locacao.Status);
        Assert.Equal(0, _locacoes.ChamadasStartFromReserva);
    }

    [Fact]
    public async Task ComAvariaNovaGeraQuatroEReplayPermaneceImutavelAposAlteracoes()
    {
        _locacao.Vistorias[1].Avarias.Add(new Avaria { IdAvaria = 4, IdVistoria = 2, Descricao = "Nova avaria", ValorEstimado = 30m });
        ResultadoRegistroApuracaoVO primeira = await _service.RegistrarAsync(10, Pedido());
        Assert.Equal(4, primeira.Apuracao.Cobrancas.Count); Assert.Equal(260m, primeira.Apuracao.Total);
        string esperado = JsonSerializer.Serialize(primeira.Apuracao);
        _config.DisabledAt = Inicio; _config.ValorKmExcedente = 99m;
        _locacao.Status = StatusLocacao.FINALIZADA; _locacao.Vistorias[1].Avarias[0].ValorEstimado = 800m;
        ResultadoRegistroApuracaoVO repetida = await _service.RegistrarAsync(10, Pedido());
        Assert.False(repetida.Criada); Assert.Equal(esperado, JsonSerializer.Serialize(repetida.Apuracao));
        Assert.Equal(esperado, JsonSerializer.Serialize(await _service.BuscarAsync(10)));
        Assert.Equal(1, _repository.Insercoes);
        Assert.Contains("DataRetirada", _repository.Apuracoes[0].SnapshotJson);
        Assert.Contains("ComparacaoRealizada", _repository.Apuracoes[0].SnapshotJson);
    }

    [Fact]
    public async Task ZeroPersisteApuracaoSemCobrancasEReplayNaoRecalcula()
    {
        _config.FranquiaKm = 1000; _config.ToleranciaAtrasoMinutos = 999; _locacao.Vistorias[1].NivelCombustivel = 1m;
        ResultadoRegistroApuracaoVO primeira = await _service.RegistrarAsync(10, Pedido());
        Assert.Equal(0m, primeira.Apuracao.Total); Assert.Empty(primeira.Apuracao.Cobrancas); Assert.Single(_repository.Apuracoes);
        _locacao.Vistorias.Clear(); _config.DisabledAt = Inicio;
        ResultadoRegistroApuracaoVO repetida = await _service.RegistrarAsync(10, Pedido());
        Assert.False(repetida.Criada); Assert.Equal(0m, repetida.Apuracao.Total); Assert.Empty(repetida.Apuracao.Cobrancas);
    }

    [Fact]
    public async Task VersaoObsoletaEConflitoManualNaoPersistem()
    {
        _config.Versao = 2;
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, Pedido()));
        _config.Versao = 1; _repository.ConflitoManual = true;
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, Pedido()));
        Assert.Empty(_repository.Apuracoes); Assert.Equal(0, _repository.Insercoes);
    }

    [Fact]
    public async Task SemConfiguracaoOuFuncionarioOuStatusValidoNaoPersiste()
    {
        _config.DisabledAt = Inicio;
        await Assert.ThrowsAsync<BusinessException>(() => _service.SimularAsync(10));
        _config.DisabledAt = null; _locacao.Status = StatusLocacao.FINALIZADA;
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, Pedido()));
        _locacao.Status = StatusLocacao.EM_ANDAMENTO; _contexto.Definir(1, 999, CargoFuncionario.ATENDENTE, false);
        await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, Pedido()));
        Assert.Empty(_repository.Apuracoes);
    }

    [Fact]
    public async Task StatusAtrasadaAceitaApuracao()
    {
        _locacao.Status = StatusLocacao.ATRASADA;
        ResultadoRegistroApuracaoVO registro = await _service.RegistrarAsync(10, Pedido());
        Assert.True(registro.Criada);
        Assert.Equal(StatusLocacao.ATRASADA, _locacao.Status);
    }

    [Fact]
    public async Task FalhaNaSegundaGravacaoReverteApuracaoECobrancasNoContratoDoFake()
    {
        _repository.FalharSnapshot = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.RegistrarAsync(10, Pedido()));
        Assert.Empty(_repository.Apuracoes); Assert.NotEmpty(_log.Entries);
        Assert.Null(_locacao.ValorTotal); Assert.Equal(StatusLocacao.EM_ANDAMENTO, _locacao.Status);
    }

    [Fact]
    public async Task RequisicoesConcorrentesGeramUmRegistro()
    {
        ResultadoRegistroApuracaoVO[] resultados = await Task.WhenAll(_service.RegistrarAsync(10, Pedido()), _service.RegistrarAsync(10, Pedido()));
        Assert.Single(resultados, x => x.Criada); Assert.Single(_repository.Apuracoes); Assert.Equal(1, _repository.Insercoes);
    }

    [Fact]
    public async Task EmpresaDiferenteNaoConsultaNemRegistraHistorico()
    {
        await _service.RegistrarAsync(10, Pedido()); _contexto.Definir(2, 20, CargoFuncionario.ADMINISTRADOR, false);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.BuscarAsync(10));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.RegistrarAsync(10, Pedido()));
        await Assert.ThrowsAsync<NotFoundException>(() => _configService.BuscarAsync(1));
        Assert.Equal(1, _repository.Insercoes);
    }

    [Fact]
    public async Task FranquiaPorDiariaUsaSomentePrazoPrevistoESuportaLong()
    {
        _config.BaseFranquia = ConstantsMessage.CobrancasExtrasPorDiaria; _config.FranquiaKm = int.MaxValue;
        _locacao.DataDevolucaoPrevista = Inicio.AddHours(25); _locacao.Vistorias[1].DataHora = Inicio.AddHours(80);
        ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(10);
        Assert.Equal(2, resultado.DiariasPrevistas); Assert.Equal((long)int.MaxValue * 2, resultado.Quilometragem.PacoteKmTotal);
    }

    [Fact]
    public async Task CadastroDuplicadoVersaoEExclusaoLogica()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _configService.InserirAsync(Valores()));
        ConfiguracaoCobrancaExtraVO valores = Valores(); valores.VersaoEsperada = 99;
        await Assert.ThrowsAsync<BusinessException>(() => _configService.AtualizarAsync(1, valores));
        Assert.Equal(1, _config.Versao); valores.VersaoEsperada = 1; valores.ValorKmExcedente = 3m;
        await _configService.AtualizarAsync(1, valores); Assert.Equal(2, _config.Versao); Assert.Equal(3m, _config.ValorKmExcedente);
        await _configService.DesativarAsync(1); Assert.NotNull(_config.DisabledAt); Assert.Single(_repository.Configuracoes);
        ReturnConfiguracaoCobrancaExtraVO nova = await _configService.InserirAsync(Valores());
        Assert.Equal(1, nova.Versao); Assert.Equal(2, _repository.Configuracoes.Count);
    }
}
