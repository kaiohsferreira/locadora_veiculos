using AutoVia.Core.ServicesInterface;
using System.Threading.Tasks;

namespace AutoVia.Tests.Fakes
{
    public sealed class FakeNotificacaoTransacionalService : INotificacaoTransacionalService
    {
        public Task ConfirmarReservaAsync(ReservaCriadaEmail evento) => Task.CompletedTask;

        public Task EmitirReciboDevolucaoAsync(LocacaoFinalizadaEmail evento) => Task.CompletedTask;

        public Task AvisarCobrancaAsync(CobrancaLancadaEmail evento) => Task.CompletedTask;
    }
}
