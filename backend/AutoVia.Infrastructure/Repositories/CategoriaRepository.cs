using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public CategoriaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<List<Categoria>> FindAllAsync(int deslocamento, int quantidade)
        {
            try
            {
                List<Categoria> categorias = await _db.Categorias
                    .AsNoTracking()
                    .Where(x => x.DisabledAt == null)
                    .OrderBy(x => x.Nome)
                    .Skip(deslocamento)
                    .Take(quantidade)
                    .ToListAsync();

                return categorias;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCategorias, GetType().ToString());
                throw;
            }
        }

        public async Task<int> CountAsync()
        {
            try
            {
                int total = await _db.Categorias
                    .CountAsync(x => x.DisabledAt == null);

                return total;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarCategorias, GetType().ToString());
                throw;
            }
        }

        public async Task<Categoria?> FindByIdAsync(int idCategoria)
        {
            try
            {
                Categoria? categoria = await _db.Categorias
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdCategoria == idCategoria && x.DisabledAt == null);

                return categoria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCategoria, GetType().ToString());
                throw;
            }
        }

        public async Task<bool> ExistsByNomeAsync(string nome, int? idCategoriaIgnorada)
        {
            try
            {
                IQueryable<Categoria> consulta = _db.Categorias
                    .Where(x => x.DisabledAt == null);

                if (idCategoriaIgnorada.HasValue)
                {
                    consulta = consulta.Where(x => x.IdCategoria != idCategoriaIgnorada.Value);
                }

                bool existe = await consulta
                    .AnyAsync(x => x.Nome.Trim().ToLower() == nome.Trim().ToLower());

                return existe;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarCategoria, GetType().ToString());
                throw;
            }
        }

        public async Task<Categoria> InsertAsync(Categoria categoria)
        {
            try
            {
                DateTime agora = DateTime.Now;

                categoria.IdCategoria = 0;
                categoria.CreatedAt = agora;
                categoria.UpdatedAt = agora;
                categoria.DisabledAt = null;

                _db.Categorias.Add(categoria);
                await _db.SaveChangesAsync();

                return categoria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Categoria categoria)
        {
            try
            {
                Categoria? categoriaSalva = await _db.Categorias
                    .FirstOrDefaultAsync(x => x.IdCategoria == categoria.IdCategoria && x.DisabledAt == null);

                if (categoriaSalva == null)
                {
                    return false;
                }

                categoriaSalva.Nome = categoria.Nome;
                categoriaSalva.Descricao = categoria.Descricao;
                categoriaSalva.ValorDiariaBase = categoria.ValorDiariaBase;
                categoriaSalva.UpdatedAt = DateTime.Now;

                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarCategoria, GetType().ToString());
                throw;
            }
        }
    }
}
