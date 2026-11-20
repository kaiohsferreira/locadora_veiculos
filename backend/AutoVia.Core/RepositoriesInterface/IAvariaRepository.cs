using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Persistencia das avarias de vistoria.</summary>
    public interface IAvariaRepository
    {
        /// <summary>Vistoria ativa da locadora do contexto, com a locacao carregada; nulo quando nao existe ou a locacao esta desativada.</summary>
        Task<Vistoria?> FindAtivaComLocacaoAsync(int idVistoria, CancellationToken cancellationToken = default);
        Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
        Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default);
    }
}
