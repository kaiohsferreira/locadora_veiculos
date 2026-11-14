using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public partial interface IVeiculoService
    {
        Task<PaginaVO<ReturnVeiculoDisponivelVO>> GetDisponiveisAsync(FiltroVeiculoDisponibilidadeVO filtro);
    }
}
