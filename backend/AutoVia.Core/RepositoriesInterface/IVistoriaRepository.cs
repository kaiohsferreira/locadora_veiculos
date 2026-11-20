using AutoVia.Core.Entities.PgSql;
using System;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface IVistoriaRepository
    {
        Task<Vistoria?> FindEntradaByLocacaoAsync(int idLocacao);
        Task<Vistoria> InsertAsync(Vistoria vistoria);
        Task<T> ExecutarComLocacaoBloqueadaAsync<T>(int idLocacao, Func<Task<T>> operacao);
    }
}
