using AutoVia.Communication.ViewObjects.Vistorias;

namespace AutoVia.Core.ServicesInterface;

public interface IVistoriaSaidaService
{
    Task<ContextoDevolucaoVO> GetContextoAsync(int idLocacao);
    Task<ComparacaoSaidaVO> GetSaidaAsync(int idLocacao);
    Task<ComparacaoSaidaVO> RegistrarSaidaAsync(int idLocacao, VistoriaSaidaVO model, int idFuncionario);
}
