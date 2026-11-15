using AutoVia.Core.Entities.PgSql;
using AutoVia.Communication.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface IReservaRepository
    {
        Task<Reserva> InsertAtomicallyAsync(Reserva reserva);
        Task<Reserva?> FindByIdAsync(int idReserva, int idCliente);
        Task<int> CountAsync(int idCliente, StatusReserva? status);
        Task<List<Reserva>> FindAllAsync(int idCliente, StatusReserva? status, int deslocamento, int quantidade);
        Task CancelAtomicallyAsync(int idReserva, int idCliente);
    }
}
