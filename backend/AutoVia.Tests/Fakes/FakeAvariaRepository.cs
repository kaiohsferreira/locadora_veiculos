using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    public sealed class FakeAvariaRepository : IAvariaRepository
    {
        private readonly Dictionary<int, Avaria> _avarias = new Dictionary<int, Avaria>();
        public bool VistoriaExiste { get; set; } = true;
        public int CriacaoCount { get; private set; }

        public Task<bool> VistoriaAtivaExisteAsync(int idVistoria, CancellationToken cancellationToken = default) => Task.FromResult(VistoriaExiste);

        public Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            Avaria? avaria = _avarias.TryGetValue(idAvaria, out Avaria? encontrada) && encontrada.IdVistoria == idVistoria ? encontrada : null;
            return Task.FromResult(avaria);
        }

        public Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default)
        {
            avaria.IdAvaria = CriacaoCount + 1;
            _avarias.Add(avaria.IdAvaria, avaria);
            CriacaoCount++;
            return Task.FromResult(avaria);
        }
    }
}
