using AutoVia.Communication.ViewObjects.CobrancasExtras;

namespace AutoVia.Core.ServicesInterface;

public interface IApuracaoCobrancasExtrasService
{
    Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao);
    Task<ReturnApuracaoCobrancasExtrasVO> BuscarAsync(int idLocacao);
    Task<ResultadoRegistroApuracaoVO> RegistrarAsync(int idLocacao, ApurarCobrancasExtrasVO model);

    /// <summary>
    /// Pre-validacao do encerramento (passo 7): devolve T e o estado da apuracao SEM gravar e SEM provedor. Lanca 409 ou 404 nas
    /// mesmas recusas de RegistrarAsync quando a apuracao ainda nao existe.
    /// </summary>
    Task<PreValidacaoEncerramentoRegistro> TotalParaEncerramentoAsync(int idLocacao, int idConfiguracao, int versaoEsperada);

    /// <summary>
    /// Totais para o GET da devolucao, SEM gravar e SEM provedor: nulos (nunca 409) quando nao ha politica ou vistoria de saida.
    /// </summary>
    Task<TotaisContextoApuracaoRegistro> TotaisDoContextoAsync(int idLocacao);
}

/// <summary>Totais do contexto da devolucao (GET devolucao): politica da categoria e total das extras, nulos quando nao ha.</summary>
public sealed class TotaisContextoApuracaoRegistro
{
    /// <summary>Politica de cobranca da categoria; nulo sem politica ativa.</summary>
    public int? IdConfiguracao { get; set; }

    /// <summary>Versao da politica; nulo sem politica ativa.</summary>
    public int? VersaoEsperada { get; set; }

    /// <summary>Total das extras (T); nulo sem vistoria de saida ou sem politica.</summary>
    public decimal? TotalExtras { get; set; }
}
