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

namespace AutoVia.Infrastructure.Services;

/// <summary>Simulação somente de leitura. Cada resultado é provisório, sem lançamento ou fechamento.</summary>
public sealed class CobrancasExtrasService : ICobrancasExtrasService
{
    private const decimal LimiteCategoria = 99999999.99m;
    private readonly ILocacaoRepository _locacoes;
    private readonly IVistoriaRepository _entradas;
    private readonly IVistoriaSaidaRepository _saidas;
    private readonly ILogService _logService;

    public CobrancasExtrasService(ILocacaoRepository locacoes, IVistoriaRepository entradas,
        IVistoriaSaidaRepository saidas, ILogService logService)
    {
        _locacoes = locacoes;
        _entradas = entradas;
        _saidas = saidas;
        _logService = logService;
    }

    public async Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao, SimularCobrancasExtrasVO model)
    {
        try
        {
            SimularCobrancasExtrasVOValidator validator = new SimularCobrancasExtrasVOValidator();
            await validator.ValidateAndThrowAsync(model);
            // O repositório aplica o filtro de empresa da sessão antes das demais leituras.
            Locacao? locacao = await _locacoes.FindByIdAsync(idLocacao);
            if (locacao == null || locacao.DisabledAt != null || locacao.IdLocacao != idLocacao)
                throw new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada);
            Vistoria? entrada = await _entradas.FindEntradaByLocacaoAsync(idLocacao);
            Vistoria? saida = await _saidas.FindSaidaByLocacaoAsync(idLocacao);
            List<Avaria> snapshots = saida == null ? new List<Avaria>() : await _saidas.FindAvariasAtivasAsync(saida.IdVistoria);
            return Calcular(locacao, entrada, saida, snapshots, model);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSimularCobrancasExtras, GetType().ToString());
            throw;
        }
    }

    public ReturnSimulacaoCobrancasExtrasVO Calcular(Locacao locacao, Vistoria? entrada, Vistoria? saida,
        List<Avaria> snapshots, SimularCobrancasExtrasVO model)
    {
        try
        {
            SimularCobrancasExtrasVOValidator validator = new SimularCobrancasExtrasVOValidator();
            validator.ValidateAndThrow(model);
            int idLocacao = locacao.IdLocacao;
            DateTime previsaoEfetiva = RegrasCobrancasExtras.DevolucaoPrevistaEfetiva(locacao.DataRetirada, locacao.DataDevolucaoPrevista);
            if (entrada == null || saida == null || entrada.DisabledAt != null || saida.DisabledAt != null ||
                entrada.IdLocacao != idLocacao || saida.IdLocacao != idLocacao ||
                entrada.IdVistoria <= 0 || saida.IdVistoria <= 0 || entrada.IdVistoria == saida.IdVistoria ||
                entrada.Tipo != TipoVistoria.ENTRADA || saida.Tipo != TipoVistoria.SAIDA ||
                locacao.KmSaida < 0 || entrada.KmRegistrado < 0 || saida.KmRegistrado < entrada.KmRegistrado ||
                saida.KmRegistrado < locacao.KmSaida || locacao.DataRetirada == default ||
                previsaoEfetiva <= locacao.DataRetirada || entrada.DataHora < locacao.DataRetirada ||
                saida.DataHora < entrada.DataHora ||
                !entrada.NivelCombustivel.HasValue || !saida.NivelCombustivel.HasValue ||
                entrada.NivelCombustivel.Value < 0m || entrada.NivelCombustivel.Value > 1m ||
                saida.NivelCombustivel.Value < 0m || saida.NivelCombustivel.Value > 1m ||
                decimal.Round(entrada.NivelCombustivel.Value, 2) != entrada.NivelCombustivel.Value ||
                decimal.Round(saida.NivelCombustivel.Value, 2) != saida.NivelCombustivel.Value)
                throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasVistorias);
            if (!saida.ComparacaoRealizada)
                throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasAvarias);

            if (snapshots.Any(x => x.DisabledAt != null || x.IdVistoria != saida.IdVistoria || x.IdAvaria <= 0 ||
                    (x.IdAvariaEntrada.HasValue && x.IdAvariaEntrada.Value <= 0)) ||
                snapshots.Select(x => x.IdAvaria).Distinct().Count() != snapshots.Count)
                throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasAvarias);
            List<ItemAvariaSimulacaoVO> itens = new List<ItemAvariaSimulacaoVO>();
            decimal valorAvarias = 0m;
            foreach (Avaria nova in snapshots.Where(x => !x.IdAvariaEntrada.HasValue).OrderBy(x => x.IdAvaria))
            {
                if (!nova.ValorEstimado.HasValue || nova.ValorEstimado.Value < 0m ||
                    nova.ValorEstimado.Value > LimiteCategoria || decimal.Round(nova.ValorEstimado.Value, 2) != nova.ValorEstimado.Value)
                    throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasAvarias);
                valorAvarias += nova.ValorEstimado.Value;
                if (valorAvarias > LimiteCategoria) throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasLimite);
                itens.Add(new ItemAvariaSimulacaoVO
                {
                    IdAvaria = nova.IdAvaria, Descricao = nova.Descricao, ValorEstimado = nova.ValorEstimado.Value
                });
            }

            long kmExcedentes = Math.Max((long)saida.KmRegistrado - entrada.KmRegistrado - model.PacoteKmTotal!.Value, 0L);
            decimal fracaoFaltante = Math.Max(entrada.NivelCombustivel.Value - saida.NivelCombustivel.Value, 0m);
            decimal ticksAtraso = Math.Max((decimal)(saida.DataHora - previsaoEfetiva).Ticks -
                model.ToleranciaAtrasoMinutos!.Value * (decimal)TimeSpan.TicksPerMinute, 0m);
            decimal ticksUnidade = model.UnidadeAtraso == ConstantsMessage.CobrancasExtrasHora ? TimeSpan.TicksPerHour : TimeSpan.TicksPerDay;
            decimal unidades = ticksAtraso / ticksUnidade;
            decimal cobradas = model.FracionamentoAtraso == ConstantsMessage.CobrancasExtrasIniciada ? decimal.Ceiling(unidades) : unidades;
            decimal valorKm = decimal.Round(kmExcedentes * model.ValorKmExcedente!.Value, 2, MidpointRounding.AwayFromZero);
            decimal valorCombustivel = decimal.Round(fracaoFaltante * model.ValorTanqueCompleto!.Value, 2, MidpointRounding.AwayFromZero);
            decimal valorAtrasoExato = model.FracionamentoAtraso == ConstantsMessage.CobrancasExtrasIniciada
                ? cobradas * model.ValorUnidadeAtraso!.Value
                : (ticksAtraso * model.ValorUnidadeAtraso!.Value) / ticksUnidade;
            decimal valorAtraso = decimal.Round(valorAtrasoExato, 2, MidpointRounding.AwayFromZero);
            valorAvarias = decimal.Round(valorAvarias, 2, MidpointRounding.AwayFromZero);
            if (valorKm > LimiteCategoria || valorCombustivel > LimiteCategoria || valorAtraso > LimiteCategoria)
                throw new BusinessException(ConstantsMessage.ErroCobrancasExtrasLimite);
            return new ReturnSimulacaoCobrancasExtrasVO
            {
                IdLocacao = idLocacao, IdVistoriaEntrada = entrada.IdVistoria, IdVistoriaSaida = saida.IdVistoria,
                Simulacao = true, Provisorio = true, Aviso = ConstantsMessage.AvisoCobrancasExtrasSimulacao,
                Quilometragem = new MemoriaQuilometragemVO
                {
                    KmEntrada = entrada.KmRegistrado, KmSaida = saida.KmRegistrado,
                    PacoteKmTotal = model.PacoteKmTotal.Value, KmExcedentes = kmExcedentes,
                    ValorKmExcedente = model.ValorKmExcedente.Value, Valor = valorKm
                },
                Combustivel = new MemoriaCombustivelVO
                {
                    NivelEntrada = entrada.NivelCombustivel.Value, NivelSaida = saida.NivelCombustivel.Value,
                    FracaoFaltante = fracaoFaltante, ValorTanqueCompleto = model.ValorTanqueCompleto.Value, Valor = valorCombustivel
                },
                Atraso = new MemoriaAtrasoVO
                {
                    DataPrevistaStr = StaticMethods.FormateDateTimeToPTBR(previsaoEfetiva), DataPrevista = previsaoEfetiva,
                    DataSaidaStr = StaticMethods.FormateDateTimeToPTBR(saida.DataHora),
                    ToleranciaAtrasoMinutos = model.ToleranciaAtrasoMinutos.Value, UnidadeAtraso = model.UnidadeAtraso!,
                    FracionamentoAtraso = model.FracionamentoAtraso!, UnidadesAposTolerancia = unidades,
                    UnidadesCobradas = cobradas, ValorUnidadeAtraso = model.ValorUnidadeAtraso.Value, Valor = valorAtraso
                },
                Avarias = new MemoriaAvariasVO { ValoresEstimados = true, Itens = itens, Valor = valorAvarias },
                Total = valorKm + valorCombustivel + valorAtraso + valorAvarias
            };
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSimularCobrancasExtras, GetType().ToString());
            throw;
        }
    }
}
