using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes;

/// <summary>
/// Apuracao de cobrancas extras programavel para testar o encerramento e o GET da devolucao sem banco. Os resultados sao
/// configurados pelo teste; as chamadas sao contadas para provar que a pre-validacao nao grava e nao toca o provedor.
/// </summary>
public class FakeApuracaoCobrancasExtrasService : IApuracaoCobrancasExtrasService
{
    /// <summary>Pre-validacao devolvida por TotalParaEncerramentoAsync.</summary>
    public PreValidacaoEncerramentoRegistro Pre { get; set; } = new PreValidacaoEncerramentoRegistro();

    /// <summary>Quando verdadeiro, TotalParaEncerramentoAsync lanca a recusa configurada em ErroPre.</summary>
    public BusinessException? ErroPre { get; set; }

    /// <summary>Totais do GET devolucao devolvidos por TotaisDoContextoAsync.</summary>
    public TotaisContextoApuracaoRegistro Totais { get; set; } = new TotaisContextoApuracaoRegistro();

    /// <summary>Apuracao ja gravada, devolvida por BuscarAsync; nula = 404.</summary>
    public ReturnApuracaoCobrancasExtrasVO? Apuracao { get; set; }

    /// <summary>Resultado devolvido por RegistrarAsync (apuracao nova).</summary>
    public ResultadoRegistroApuracaoVO? Registro { get; set; }

    public int ChamadasTotal { get; private set; }

    public int ChamadasRegistrar { get; private set; }

    public Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao)
    {
        return Task.FromResult(new ReturnSimulacaoCobrancasExtrasVO());
    }

    public Task<ReturnApuracaoCobrancasExtrasVO> BuscarAsync(int idLocacao)
    {
        if (Apuracao == null)
        {
            throw new NotFoundException(ConstantsMessage.ErroApuracaoCobrancasExtrasNaoEncontrada);
        }

        return Task.FromResult(Apuracao);
    }

    public Task<ResultadoRegistroApuracaoVO> RegistrarAsync(int idLocacao, ApurarCobrancasExtrasVO model)
    {
        ChamadasRegistrar++;
        if (Registro == null)
        {
            throw new InvalidOperationException("Registro da apuracao nao configurado no teste.");
        }

        return Task.FromResult(Registro);
    }

    public Task<PreValidacaoEncerramentoRegistro> TotalParaEncerramentoAsync(int idLocacao, int idConfiguracao, int versaoEsperada)
    {
        ChamadasTotal++;
        if (ErroPre != null)
        {
            throw ErroPre;
        }

        return Task.FromResult(Pre);
    }

    public Task<TotaisContextoApuracaoRegistro> TotaisDoContextoAsync(int idLocacao)
    {
        return Task.FromResult(Totais);
    }
}
