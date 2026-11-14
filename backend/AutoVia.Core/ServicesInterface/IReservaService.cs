using AutoVia.Communication.ViewObjects.Reservas;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    public interface IReservaService
    {
        Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada);
    }
}
