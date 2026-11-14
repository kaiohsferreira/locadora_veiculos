using AutoVia.Core.Entities.PgSql;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface IReservaRepository
    {
        Task<Reserva> InsertAtomicallyAsync(Reserva reserva);
    }
}
