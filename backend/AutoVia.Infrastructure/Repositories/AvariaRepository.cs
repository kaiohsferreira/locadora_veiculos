using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>Persistencia das avarias com filtro de exclusao logica.</summary>
    public sealed class AvariaRepository : IAvariaRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public AvariaRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<bool> VistoriaAtivaExisteAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Vistorias.AsNoTracking().AnyAsync(x => x.IdVistoria == idVistoria && x.DisabledAt == null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Avarias.AsNoTracking().FirstOrDefaultAsync(x => x.IdVistoria == idVistoria && x.IdAvaria == idAvaria && x.DisabledAt == null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        public async Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default)
        {
            try
            {
                DateTime agora = DateTime.Now;
                avaria.CreatedAt = agora;
                avaria.UpdatedAt = agora;
                avaria.DisabledAt = null;
                _db.Avarias.Add(avaria);
                await _db.SaveChangesAsync(cancellationToken);
                return avaria;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }
    }
}
