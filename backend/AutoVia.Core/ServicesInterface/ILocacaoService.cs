using AutoVia.Communication.ViewObjects.Locacoes;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Regras de negocio para iniciar e consultar locacoes.</summary>
    public interface ILocacaoService
    {
        Task<ReturnLocacaoVO> StartFromReservaAsync(LocacaoVO locacaoVO, int idFuncionario);
        Task<ReturnLocacaoVO> GetByIdAsync(int idLocacao);
    }
}
