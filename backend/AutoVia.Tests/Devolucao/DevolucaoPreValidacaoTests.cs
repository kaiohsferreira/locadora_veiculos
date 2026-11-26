using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Devolucao;

/// <summary>
/// Pre-validacao do encerramento (TotalParaEncerramentoAsync e TotaisDoContextoAsync) com o servico real de apuracao e
/// repositorio fake: recusas sem gravacao e sem chamar o pagamento, e leitura do snapshot quando ha apuracao existente (O-5).
/// </summary>
public sealed class DevolucaoPreValidacaoTests
{
    private static readonly DateTime Inicio = new DateTime(2026, 11, 18, 8, 0, 0);
    private readonly FakeLogService _log = new FakeLogService();
    private readonly ContextoEmpresa _contexto;
    private readonly FakeCobrancasExtrasRepository _repository;
    private readonly FakeLocacaoRepository _locacoes = new FakeLocacaoRepository();
    private readonly ApuracaoCobrancasExtrasService _service;
    private readonly ConfiguracaoCobrancaExtra _config;
    private readonly FakePagamentoService _pagamentos = new FakePagamentoService();

    public DevolucaoPreValidacaoTests()
    {
        _contexto = new ContextoEmpresa(_log);
        _contexto.Definir(1, 20, CargoFuncionario.ADMINISTRADOR, false);
        _repository = new FakeCobrancasExtrasRepository(_contexto);
        _locacoes.Adicionar(new Funcionario { IdFuncionario = 20, IdEmpresa = 1, Ativo = true });

        Locacao locacao = new Locacao
        {
            IdLocacao = 10, DataRetirada = Inicio, DataDevolucaoPrevista = Inicio.Date.AddDays(1), KmSaida = 100,
            Status = StatusLocacao.EM_ANDAMENTO, Veiculo = new Veiculo { IdVeiculo = 1, IdEmpresa = 1, IdCategoria = 1 }
        };
        locacao.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DataHora = Inicio, KmRegistrado = 100, NivelCombustivel = 1m });
        locacao.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, DataHora = Inicio.Date.AddDays(1).AddHours(10), KmRegistrado = 200, NivelCombustivel = 0.5m, ComparacaoRealizada = true });
        _repository.Locacoes.Add(locacao);

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
        FakeRelogio relogio = new FakeRelogio(Inicio.AddDays(1).AddHours(5));
        _service = new ApuracaoCobrancasExtrasService(_repository, _locacoes, calculadora, _contexto, relogio, configuracao.CreateMapper(), _log, _pagamentos);
    }

    private static ApurarCobrancasExtrasVO Pedido() => new ApurarCobrancasExtrasVO { IdConfiguracao = 1, VersaoEsperada = 1 };

    [Fact]
    public async Task TotalParaEncerramento_VersaoDivergente_Recusa409SemGravarNemLancarPagamento()
    {
        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => _service.TotalParaEncerramentoAsync(10, 1, 99));

        Assert.Equal(ConstantsMessage.ErroConfiguracaoCobrancasExtrasVersao, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task TotalParaEncerramento_ApuracaoExistente_DevolveOTotalDoSnapshotSemGravarNemLancar()
    {
        ResultadoRegistroApuracaoVO registro = await _service.RegistrarAsync(10, Pedido());
        int chamadasAntes = _pagamentos.Chamadas;
        int apuracoesAntes = _repository.Apuracoes.Count;
        _config.ValorKmExcedente = 999m;

        PreValidacaoEncerramentoRegistro pre = await _service.TotalParaEncerramentoAsync(10, 1, 1);

        Assert.True(pre.ApuracaoExistente);
        Assert.Equal(registro.Apuracao.Total, pre.Total);
        Assert.Equal(registro.Apuracao.IdPagamentoLancado, pre.IdPagamentoLancado);
        Assert.Equal(chamadasAntes, _pagamentos.Chamadas);
        Assert.Equal(apuracoesAntes, _repository.Apuracoes.Count);
    }

    [Fact]
    public async Task TotaisDoContexto_SemPoliticaComApuracaoExistente_DevolveOTotalDoSnapshot()
    {
        ResultadoRegistroApuracaoVO registro = await _service.RegistrarAsync(10, Pedido());
        _config.Disable();

        TotaisContextoApuracaoRegistro totais = await _service.TotaisDoContextoAsync(10);

        Assert.Equal(registro.Apuracao.Total, totais.TotalExtras);
    }

    /// <summary>O GET da devolucao nunca falha por causa do calculo (O-9): sem vistoria de saida, os totais das extras ficam nulos.</summary>
    [Fact]
    public async Task TotaisDoContexto_SemVistoriaDeSaida_DevolveTotaisExtrasNulosSem409()
    {
        _repository.Locacoes.Single().Vistorias.RemoveAll(x => x.Tipo == TipoVistoria.SAIDA);

        TotaisContextoApuracaoRegistro totais = await _service.TotaisDoContextoAsync(10);

        Assert.Null(totais.TotalExtras);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task TotalParaEncerramento_LocacaoInexistente_Recusa404SemGravarNemLancarPagamento()
    {
        NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => _service.TotalParaEncerramentoAsync(999, 1, 1));

        Assert.Equal(ConstantsMessage.ErroLocacaoNaoEncontrada, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task TotalParaEncerramento_SemPoliticaEApuracaoNova_Recusa409SemGravarNemLancarPagamento()
    {
        _config.Disable();

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => _service.TotalParaEncerramentoAsync(10, 1, 1));

        Assert.Equal(ConstantsMessage.ErroConfiguracaoCobrancasExtrasObrigatoria, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task RegistrarApuracao_LocacaoInexistente_Recusa404SemGravarNemLancarPagamento()
    {
        NotFoundException erro = await Assert.ThrowsAsync<NotFoundException>(() => _service.RegistrarAsync(999, Pedido()));

        Assert.Equal(ConstantsMessage.ErroLocacaoNaoEncontrada, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task RegistrarApuracao_VersaoDivergente_Recusa409SemGravarNemLancarPagamento()
    {
        ApurarCobrancasExtrasVO pedido = new ApurarCobrancasExtrasVO { IdConfiguracao = 1, VersaoEsperada = 99 };

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, pedido));

        Assert.Equal(ConstantsMessage.ErroConfiguracaoCobrancasExtrasVersao, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }

    [Fact]
    public async Task RegistrarApuracao_SemPoliticaEApuracaoNova_Recusa409SemGravarNemLancarPagamento()
    {
        _config.Disable();

        BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => _service.RegistrarAsync(10, Pedido()));

        Assert.Equal(ConstantsMessage.ErroConfiguracaoCobrancasExtrasObrigatoria, erro.Message);
        Assert.Empty(_repository.Apuracoes);
        Assert.Equal(0, _pagamentos.Chamadas);
    }
}
