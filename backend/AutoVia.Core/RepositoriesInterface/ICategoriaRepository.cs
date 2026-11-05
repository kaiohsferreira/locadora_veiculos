using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public interface ICategoriaRepository
    {
        Task<List<Categoria>> FindAllAsync(int deslocamento, int quantidade);
        Task<int> CountAsync();
        Task<Categoria?> FindByIdAsync(int idCategoria);
        Task<bool> ExistsByNomeAsync(string nome, int? idCategoriaIgnorada);
        Task<Categoria> InsertAsync(Categoria categoria);
        Task<bool> UpdateAsync(Categoria categoria);
    }
}
