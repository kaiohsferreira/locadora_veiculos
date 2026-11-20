using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Vistorias;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Vistorias;

public class VistoriaSaidaServiceTests
{
    private static readonly DateTime Agora = new DateTime(2026, 11, 18, 12, 0, 0);

    private static VistoriaSaidaService Criar(FakeVistoriaSaidaRepository saidas, StatusLocacao status = StatusLocacao.EM_ANDAMENTO)
    {
        FakeLocacaoRepository locacoes = new FakeLocacaoRepository();
        locacoes.Adicionar(new Locacao { IdLocacao = 10, KmSaida = 100, Status = status, DataRetirada = Agora.AddDays(-1) });
        locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
        MapperConfiguration configuracao = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<VistoriaProfile>();
            cfg.AddProfile<VistoriaSaidaProfile>();
            cfg.AddProfile<LocacaoProfile>();
        }, NullLoggerFactory.Instance);
        return new VistoriaSaidaService(saidas, saidas.Vistorias, locacoes,
            new FakeRelogio(Agora), configuracao.CreateMapper(), new FakeLogService());
    }

    private static FakeVistoriaSaidaRepository Preparar()
    {
        FakeVistoriaSaidaRepository saidas = new FakeVistoriaSaidaRepository();
        saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, KmRegistrado = 110, DataHora = Agora.AddHours(-2) });
        saidas.Avarias.Add(new Avaria { IdAvaria = 1, IdVistoria = 1, Descricao = "Risco", Gravidade = GravidadeAvaria.LEVE, ValorEstimado = 20m });
        return saidas;
    }

    private static VistoriaSaidaVO Payload()
    {
        return new VistoriaSaidaVO
        {
            KmRegistrado = 120, NivelCombustivel = 0.50m, IdsAvariasEntrada = new List<int> { 1 },
            NovasAvarias = new List<NovaAvariaSaidaVO>
            {
                new NovaAvariaSaidaVO { Descricao = "Risco", Gravidade = GravidadeAvaria.LEVE, ValorEstimado = 30m }
            }
        };
    }

    [Fact]
    public async Task SnapshotComVinculoPreexistenteEMesmoTextoSemVinculoNova()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        VistoriaSaidaService service = Criar(saidas);
        ComparacaoSaidaVO retorno = await service.RegistrarSaidaAsync(10, Payload(), 20);
        Assert.True(retorno.ComparacaoRealizada);
        Assert.Equal(1, Assert.Single(retorno.AvariasPreexistentes).IdAvariaEntrada);
        Assert.Null(Assert.Single(retorno.AvariasNovas).IdAvariaEntrada);
        Assert.Empty(retorno.AvariasIndeterminadas);
        saidas.Avarias[0].Descricao = "Alterada";
        saidas.Avarias[0].ValorEstimado = 900m;
        saidas.Avarias[0].DisabledAt = Agora;
        ComparacaoSaidaVO historico = await service.GetSaidaAsync(10);
        Assert.Equal("Risco", Assert.Single(historico.AvariasPreexistentes).Descricao);
        Assert.Equal(20m, historico.AvariasPreexistentes[0].ValorEstimado);
        Assert.Single(historico.AvariasNovas);
    }

    [Fact]
    public async Task LegadoMesmoComFkNuncaInferido()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA });
        saidas.Avarias.Add(new Avaria { IdAvaria = 2, IdVistoria = 2, IdAvariaEntrada = 1, Descricao = "Risco" });
        ComparacaoSaidaVO retorno = await Criar(saidas).GetSaidaAsync(10);
        Assert.False(retorno.ComparacaoRealizada);
        Assert.Single(retorno.AvariasIndeterminadas);
        Assert.Empty(retorno.AvariasNovas);
        Assert.Empty(retorno.AvariasPreexistentes);
    }

    [Fact]
    public async Task ReferenciaDeOutraEntradaRetorna404SemPersistir()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        VistoriaSaidaVO model = Payload();
        model.IdsAvariasEntrada = new List<int> { 99 };
        await Assert.ThrowsAsync<NotFoundException>(() => Criar(saidas).RegistrarSaidaAsync(10, model, 20));
        Assert.Single(saidas.Vistorias.Vistorias);
        Assert.Single(saidas.Avarias);
    }

    [Fact]
    public async Task DuplicidadeConcorrenteRevalidadaSobTrava()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        VistoriaSaidaService service = Criar(saidas);
        Task<ComparacaoSaidaVO> primeira = service.RegistrarSaidaAsync(10, Payload(), 20);
        Task<ComparacaoSaidaVO> segunda = service.RegistrarSaidaAsync(10, Payload(), 20);
        await Assert.ThrowsAsync<BusinessException>(() => Task.WhenAll(primeira, segunda));
        Assert.Equal(1, new[] { primeira, segunda }.Count(x => x.IsCompletedSuccessfully));
        Assert.Single(saidas.Vistorias.Vistorias, x => x.Tipo == TipoVistoria.SAIDA);
    }

    [Theory]
    [InlineData(StatusLocacao.FINALIZADA)]
    [InlineData(StatusLocacao.CANCELADA)]
    public async Task StatusFechadoImpedeSaida(StatusLocacao status)
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        await Assert.ThrowsAsync<BusinessException>(() => Criar(saidas, status).RegistrarSaidaAsync(10, Payload(), 20));
        Assert.Single(saidas.Vistorias.Vistorias);
    }

    [Fact]
    public async Task SemEntradaOuKmInferiorRejeita409()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        VistoriaSaidaVO model = Payload();
        model.KmRegistrado = 109;
        await Assert.ThrowsAsync<BusinessException>(() => Criar(saidas).RegistrarSaidaAsync(10, model, 20));
        saidas.Vistorias.Vistorias.Clear();
        await Assert.ThrowsAsync<BusinessException>(() => Criar(saidas).RegistrarSaidaAsync(10, Payload(), 20));
    }

    [Fact]
    public async Task ListaDuplicadaRejeitadaAntesDaPersistencia()
    {
        FakeVistoriaSaidaRepository saidas = Preparar();
        VistoriaSaidaVO model = Payload();
        model.IdsAvariasEntrada!.Add(1);
        await Assert.ThrowsAsync<ValidationException>(() => Criar(saidas).RegistrarSaidaAsync(10, model, 20));
        Assert.Single(saidas.Vistorias.Vistorias);
    }

    [Fact]
    public void ListasAusentesENullNosFilhosSaoInvalidos()
    {
        VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
        Assert.False(validator.Validate(new VistoriaSaidaVO { KmRegistrado = 120, NivelCombustivel = 1m }).IsValid);
        VistoriaSaidaVO model = Payload();
        model.NovasAvarias!.Add(null!);
        Assert.False(validator.Validate(model).IsValid);
    }

    [Fact]
    public void AvariaNovaSemValorEstimadoEInvalidaComMensagemPropria()
    {
        VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
        VistoriaSaidaVO model = Payload();
        model.NovasAvarias![0].ValorEstimado = null;

        FluentValidation.Results.ValidationResult resultado = validator.Validate(model);

        Assert.False(resultado.IsValid);
        Assert.Equal(ConstantsMessage.ErroVistoriaSaidaAvariaValorObrigatorio, resultado.Errors[0].ErrorMessage);
    }

    [Fact]
    public void AvariaNovaComValorZeroEValida()
    {
        VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
        VistoriaSaidaVO model = Payload();
        model.NovasAvarias![0].ValorEstimado = 0m;

        Assert.True(validator.Validate(model).IsValid);
    }

    [Fact]
    public void SomaDasNovasAvariasAcimaDoTetoEInvalidaComMensagemPropria()
    {
        VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
        VistoriaSaidaVO model = Payload();
        model.NovasAvarias = new List<NovaAvariaSaidaVO>
        {
            new NovaAvariaSaidaVO { Descricao = "Dano A", Gravidade = GravidadeAvaria.GRAVE, ValorEstimado = 99999999.99m },
            new NovaAvariaSaidaVO { Descricao = "Dano B", Gravidade = GravidadeAvaria.GRAVE, ValorEstimado = 99999999.99m }
        };

        FluentValidation.Results.ValidationResult resultado = validator.Validate(model);

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroVistoriaSaidaAvariasSomaExcedida);
    }

    [Fact]
    public void UmaNovaAvariaNoTetoDaSomaEValida()
    {
        VistoriaSaidaVOValidator validator = new VistoriaSaidaVOValidator();
        VistoriaSaidaVO model = Payload();
        model.NovasAvarias = new List<NovaAvariaSaidaVO>
        {
            new NovaAvariaSaidaVO { Descricao = "Dano A", Gravidade = GravidadeAvaria.GRAVE, ValorEstimado = 99999999.99m }
        };

        Assert.True(validator.Validate(model).IsValid);
    }
}
