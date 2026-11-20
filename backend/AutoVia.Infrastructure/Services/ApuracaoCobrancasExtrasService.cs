using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.CobrancasExtras;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using System.Text.Json;

namespace AutoVia.Infrastructure.Services;

public sealed class ApuracaoCobrancasExtrasService : IApuracaoCobrancasExtrasService
{
    private const int VersaoSnapshot = 1;
    private readonly ICobrancasExtrasRepository _repository;
    private readonly ILocacaoRepository _locacoes;
    private readonly ICobrancasExtrasService _calculadora;
    private readonly IContextoEmpresa _contexto;
    private readonly IRelogio _relogio;
    private readonly IMapper _mapper;
    private readonly ILogService _logService;

    public ApuracaoCobrancasExtrasService(ICobrancasExtrasRepository repository, ILocacaoRepository locacoes,
        ICobrancasExtrasService calculadora, IContextoEmpresa contexto, IRelogio relogio, IMapper mapper, ILogService logService)
    {
        _repository = repository; _locacoes = locacoes; _calculadora = calculadora;
        _contexto = contexto; _relogio = relogio; _mapper = mapper; _logService = logService;
    }

    public async Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao)
    {
        try
        {
            return await _repository.ComLocacaoBloqueadaAsync(idLocacao, async () =>
            {
                Locacao locacao = await ObterLocacaoAsync(idLocacao);
                ConfiguracaoCobrancaExtra configuracao = await ObterConfiguracaoAsync(locacao);
                return Calcular(locacao, configuracao);
            });
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroSimularCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ReturnApuracaoCobrancasExtrasVO> BuscarAsync(int idLocacao)
    {
        try
        {
            await ObterLocacaoAsync(idLocacao);
            ApuracaoCobrancaExtra apuracao = await _repository.BuscarApuracaoAsync(idLocacao)
                ?? throw new NotFoundException(ConstantsMessage.ErroApuracaoCobrancasExtrasNaoEncontrada);
            return LerSnapshot(apuracao);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public async Task<ResultadoRegistroApuracaoVO> RegistrarAsync(int idLocacao, ApurarCobrancasExtrasVO model)
    {
        try
        {
            ApurarCobrancasExtrasVOValidator validator = new ApurarCobrancasExtrasVOValidator();
            await validator.ValidateAndThrowAsync(model);
            return await _repository.ComLocacaoBloqueadaAsync(idLocacao, async () =>
            {
                ApuracaoCobrancaExtra? anterior = await _repository.BuscarApuracaoAsync(idLocacao);
                if (anterior != null)
                    return new ResultadoRegistroApuracaoVO { Criada = false, Apuracao = LerSnapshot(anterior) };
                Locacao locacao = await ObterLocacaoAsync(idLocacao);
                Funcionario? funcionario = await _locacoes.FindActiveFuncionarioByIdAsync(_contexto.IdFuncionario);
                if (funcionario == null || _contexto.IdFuncionario <= 0 || _contexto.IdEmpresa <= 0)
                    throw new BusinessException(ConstantsMessage.ErroVistoriaFuncionarioInativo);
                if (!RegrasVistoria.LocacaoAceitaVistoria(locacao.Status))
                    throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasStatus);
                ConfiguracaoCobrancaExtra configuracao = await ObterConfiguracaoAsync(locacao);
                if (configuracao.IdConfiguracao != model.IdConfiguracao || configuracao.Versao != model.VersaoEsperada)
                    throw new BusinessException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasVersao);
                if (await _repository.ExistemCobrancasConflitantesAsync(idLocacao))
                    throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtrasConflito);
                ReturnSimulacaoCobrancasExtrasVO calculo = Calcular(locacao, configuracao);
                calculo.Simulacao = false; calculo.Provisorio = false; calculo.Aviso = ConstantsMessage.AvisoCobrancasExtrasApuradas;
                DateTime agora = _relogio.Agora;
                ApuracaoCobrancaExtra apuracao = new ApuracaoCobrancaExtra
                {
                    IdLocacao = idLocacao, IdConfiguracao = configuracao.IdConfiguracao, VersaoConfiguracao = configuracao.Versao,
                    IdVistoriaEntrada = calculo.IdVistoriaEntrada, IdVistoriaSaida = calculo.IdVistoriaSaida,
                    IdFuncionario = _contexto.IdFuncionario, DataHora = agora, VersaoSnapshot = VersaoSnapshot,
                    Total = calculo.Total, CreatedAt = agora, UpdatedAt = agora
                };
                AdicionarCobranca(apuracao, TipoCobrancaExtra.KM_EXCEDENTE, calculo.Quilometragem.Valor, ConstantsMessage.DescricaoCobrancaKm, agora);
                AdicionarCobranca(apuracao, TipoCobrancaExtra.COMBUSTIVEL, calculo.Combustivel.Valor, ConstantsMessage.DescricaoCobrancaCombustivel, agora);
                AdicionarCobranca(apuracao, TipoCobrancaExtra.ATRASO, calculo.Atraso.Valor, ConstantsMessage.DescricaoCobrancaAtraso, agora);
                AdicionarCobranca(apuracao, TipoCobrancaExtra.AVARIA, calculo.Avarias.Valor, ConstantsMessage.DescricaoCobrancaAvaria, agora);
                SnapshotApuracao snapshot = CriarSnapshot(locacao, calculo);
                apuracao.SnapshotJson = JsonSerializer.Serialize(snapshot);
                await _repository.InserirApuracaoAsync(apuracao);
                ReturnApuracaoCobrancasExtrasVO resultado = snapshot.Resultado;
                resultado.IdApuracao = apuracao.IdApuracao; resultado.IdLocacao = idLocacao;
                resultado.IdConfiguracao = configuracao.IdConfiguracao; resultado.VersaoConfiguracao = configuracao.Versao;
                resultado.DataApuracaoStr = StaticMethods.FormateDateTimeToPTBR(agora);
                resultado.Configuracao = calculo.Configuracao; resultado.Total = calculo.Total;
                resultado.Cobrancas = apuracao.Cobrancas.OrderBy(x => x.Tipo).Select(x => new ReturnCobrancaApuradaVO
                {
                    IdCobranca = x.IdCobranca, Tipo = x.Tipo.ToString(), Descricao = x.Descricao ?? string.Empty,
                    Valor = x.Valor, DataLancamentoStr = StaticMethods.FormateDateToPTBR(x.DataLancamento)
                }).ToList();
                apuracao.SnapshotJson = JsonSerializer.Serialize(snapshot);
                await _repository.SalvarSnapshotAsync(apuracao);
                return new ResultadoRegistroApuracaoVO { Criada = true, Apuracao = resultado };
            });
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private async Task<Locacao> ObterLocacaoAsync(int idLocacao)
    {
        try { return await _repository.BuscarLocacaoAsync(idLocacao) ?? throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada); }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private async Task<ConfiguracaoCobrancaExtra> ObterConfiguracaoAsync(Locacao locacao)
    {
        try
        {
            ConfiguracaoCobrancaExtra? configuracao = await _repository.BuscarConfiguracaoCategoriaAsync(locacao.Veiculo.IdCategoria, true);
            if (configuracao == null || configuracao.IdEmpresa != _contexto.IdEmpresa || configuracao.DisabledAt != null)
                throw new BusinessException(ConstantsMessage.ErroConfiguracaoCobrancasExtrasObrigatoria);
            return configuracao;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private ReturnSimulacaoCobrancasExtrasVO Calcular(Locacao locacao, ConfiguracaoCobrancaExtra configuracao)
    {
        try
        {
            List<Vistoria> entradas = locacao.Vistorias.Where(x => x.DisabledAt == null && x.Tipo == TipoVistoria.ENTRADA).ToList();
            List<Vistoria> saidas = locacao.Vistorias.Where(x => x.DisabledAt == null && x.Tipo == TipoVistoria.SAIDA).ToList();
            if (entradas.Count != 1 || saidas.Count != 1 || locacao.DataDevolucaoPrevista <= locacao.DataRetirada)
                throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasVistorias);
            int diarias = checked((int)decimal.Ceiling((locacao.DataDevolucaoPrevista - locacao.DataRetirada).Ticks / (decimal)TimeSpan.TicksPerDay));
            SimularCobrancasExtrasVO parametros = new SimularCobrancasExtrasVO
            {
                PacoteKmTotal = configuracao.BaseFranquia == ConstantsMessage.CobrancasExtrasPorDiaria ? (long)configuracao.FranquiaKm * diarias : configuracao.FranquiaKm,
                ValorKmExcedente = configuracao.ValorKmExcedente, ValorTanqueCompleto = configuracao.ValorTanqueCompleto,
                ToleranciaAtrasoMinutos = configuracao.ToleranciaAtrasoMinutos, UnidadeAtraso = configuracao.UnidadeAtraso,
                FracionamentoAtraso = configuracao.FracionamentoAtraso, ValorUnidadeAtraso = configuracao.ValorUnidadeAtraso
            };
            ReturnSimulacaoCobrancasExtrasVO resultado = _calculadora.Calcular(locacao, entradas[0], saidas[0],
                saidas[0].Avarias.Where(x => x.DisabledAt == null).ToList(), parametros);
            resultado.Configuracao = _mapper.Map<ReturnConfiguracaoCobrancaExtraVO>(configuracao);
            resultado.DiariasPrevistas = diarias;
            return resultado;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroSimularCobrancasExtras, GetType().ToString()); throw; }
    }

    private void AdicionarCobranca(ApuracaoCobrancaExtra apuracao, TipoCobrancaExtra tipo, decimal valor, string descricao, DateTime agora)
    {
        try
        {
            if (valor <= 0m) return;
            apuracao.Cobrancas.Add(new CobrancaExtra
            {
                IdLocacao = apuracao.IdLocacao, Tipo = tipo, Valor = valor, Descricao = descricao,
                DataLancamento = DateOnly.FromDateTime(agora), CreatedAt = agora, UpdatedAt = agora
            });
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private ReturnApuracaoCobrancasExtrasVO LerSnapshot(ApuracaoCobrancaExtra apuracao)
    {
        try
        {
            if (apuracao.VersaoSnapshot != VersaoSnapshot) throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtras);
            SnapshotApuracao snapshot = JsonSerializer.Deserialize<SnapshotApuracao>(apuracao.SnapshotJson)
                ?? throw new BusinessException(ConstantsMessage.ErroApuracaoCobrancasExtras);
            return snapshot.Resultado;
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    private SnapshotApuracao CriarSnapshot(Locacao locacao, ReturnSimulacaoCobrancasExtrasVO calculo)
    {
        try
        {
            return new SnapshotApuracao
            {
                Resultado = new ReturnApuracaoCobrancasExtrasVO { Calculo = calculo },
                DataRetirada = locacao.DataRetirada, DataPrevista = locacao.DataDevolucaoPrevista,
                Vistorias = locacao.Vistorias.Where(x => x.DisabledAt == null).Select(x => new FatoVistoria
                {
                    IdVistoria = x.IdVistoria, DataHora = x.DataHora, Km = x.KmRegistrado, Nivel = x.NivelCombustivel,
                    ComparacaoRealizada = x.ComparacaoRealizada,
                    Avarias = x.Avarias.Where(a => a.DisabledAt == null).Select(a => new FatoAvaria
                    { IdAvaria = a.IdAvaria, IdAvariaEntrada = a.IdAvariaEntrada, Descricao = a.Descricao, ValorEstimado = a.ValorEstimado }).ToList()
                }).ToList()
            };
        }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    public sealed class SnapshotApuracao
    {
        public ReturnApuracaoCobrancasExtrasVO Resultado { get; set; } = new ReturnApuracaoCobrancasExtrasVO();
        public DateTime DataRetirada { get; set; }
        public DateTime DataPrevista { get; set; }
        public List<FatoVistoria> Vistorias { get; set; } = new List<FatoVistoria>();
    }
    public sealed class FatoVistoria
    {
        public int IdVistoria { get; set; }
        public DateTime DataHora { get; set; }
        public int Km { get; set; }
        public decimal? Nivel { get; set; }
        public bool ComparacaoRealizada { get; set; }
        public List<FatoAvaria> Avarias { get; set; } = new List<FatoAvaria>();
    }
    public sealed class FatoAvaria
    {
        public int IdAvaria { get; set; }
        public int? IdAvariaEntrada { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal? ValorEstimado { get; set; }
    }
}
