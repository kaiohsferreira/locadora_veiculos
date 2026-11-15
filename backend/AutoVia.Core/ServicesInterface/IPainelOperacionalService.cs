using AutoVia.Communication.ViewObjects.PainelOperacional;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public interface IPainelOperacionalService
    {
        Task<ReturnPainelDiarioVO> GetPainelDiarioAsync();
    }
}
