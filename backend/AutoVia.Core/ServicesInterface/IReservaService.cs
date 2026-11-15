using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public interface IReservaService
    {
        Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada);
        Task<ReturnReservaVO> GetByIdAsync(int idReserva, int idCliente);
        Task<PaginaVO<ReturnReservaVO>> GetAllAsync(int idCliente, FiltroReservaVO filtro);
        Task CancelarAsync(int idReserva, int idCliente);
    }
}
