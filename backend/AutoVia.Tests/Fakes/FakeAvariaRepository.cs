
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// Repositorio de avarias em memoria. As vistorias vem do fake de saida (mesma lista e mesma trava do IVistoriaRepository),
    /// para que a regra da vistoria e a gravacao da avaria enxerguem o mesmo estado.
    /// </summary>
    public sealed class FakeAvariaRepository : IAvariaRepository
    {
        private readonly Dictionary<int, Avaria> _avarias = new Dictionary<int, Avaria>();
        private readonly FakeVistoriaSaidaRepository _saidas;

        public FakeAvariaRepository(FakeVistoriaSaidaRepository saidas)
        {
            _saidas = saidas;
        }

        public int CriacaoCount { get; private set; }

        /// <summary>Verdadeiro quando a avaria foi gravada com a trava da locacao ativa (o que o servico precisa garantir).</summary>
        public bool CriadoSobTrava { get; private set; }

        public Task<Vistoria?> FindAtivaComLocacaoAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            Vistoria? vistoria = _saidas.Vistorias.Vistorias.FirstOrDefault(x => x.IdVistoria == idVistoria && x.DisabledAt == null);
            return Task.FromResult(vistoria);
        }

        public Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            Avaria? avaria = _avarias.TryGetValue(idAvaria, out Avaria? encontrada) && encontrada.IdVistoria == idVistoria ? encontrada : null;
            return Task.FromResult(avaria);
        }

        public Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default)
        {
            CriadoSobTrava = _saidas.Vistorias.SobTrava;
            avaria.IdAvaria = CriacaoCount + 1;
            _avarias.Add(avaria.IdAvaria, avaria);
            CriacaoCount++;
            return Task.FromResult(avaria);
        }
    }
}
