using AutoVia.Communication.ViewObjects.CobrancasExtras;

namespace AutoVia.Core.ServicesInterface;

public interface IApuracaoCobrancasExtrasService
{
    Task<ReturnSimulacaoCobrancasExtrasVO> SimularAsync(int idLocacao);
    Task<ReturnApuracaoCobrancasExtrasVO> BuscarAsync(int idLocacao);
    Task<ResultadoRegistroApuracaoVO> RegistrarAsync(int idLocacao, ApurarCobrancasExtrasVO model);
}
