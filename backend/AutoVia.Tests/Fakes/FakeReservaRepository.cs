using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public class FakeReservaRepository : IReservaRepository
    {
        private int _proximoId = 1;
        public Reserva? Armazenada { get; private set; }
        public int ChamadasDeInsercao { get; private set; }

        public Task<Reserva> InsertAtomicallyAsync(Reserva reserva)
        {
            ChamadasDeInsercao++;
            reserva.IdReserva = _proximoId++;
            Armazenada = reserva;
            return Task.FromResult(reserva);
        }
    }
}
