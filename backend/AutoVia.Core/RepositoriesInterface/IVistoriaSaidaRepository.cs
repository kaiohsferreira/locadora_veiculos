using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface;

public interface IVistoriaSaidaRepository
{
    Task<Vistoria?> FindSaidaByLocacaoAsync(int idLocacao);
    Task<List<Avaria>> FindAvariasAtivasAsync(int idVistoria);
    Task<Vistoria> InsertAsync(Vistoria vistoria);
}
