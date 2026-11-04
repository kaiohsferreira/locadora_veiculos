using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Persistence;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Leitura das tabelas integration/integration_setting para o
    /// IIntegrationResolver. Somente leitura (AsNoTracking): a configuracao das
    /// integracoes e mantida direto no banco.
    /// </summary>
    public class IntegrationRepository : IIntegrationRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public IntegrationRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }

        public async Task<Integration?> FindByCodigoWithSettingsAsync(string codigo, CancellationToken cancellationToken = default)
        {
            try
            {
                Integration? integration = await _db.Integrations
                    .AsNoTracking()
                    .Where(x => x.Codigo == codigo && x.DisabledAt == null)
                    .Include(x => x.Settings)
                    .FirstOrDefaultAsync(cancellationToken);

                return integration;
            }
            catch (Exception ex)
            {
                _logService.Write(
                    ex,
                    string.Format(ConstantsMessageIntegration.ErroCarregarIntegracao, codigo),
                    GetType().ToString());
                return null;
            }
        }
    }
}
