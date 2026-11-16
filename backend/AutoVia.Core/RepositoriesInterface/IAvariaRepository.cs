using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Persistencia das avarias de vistoria.</summary>
    public interface IAvariaRepository
    {
        Task<bool> VistoriaAtivaExisteAsync(int idVistoria, CancellationToken cancellationToken = default);
        Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
        Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default);
    }
}
