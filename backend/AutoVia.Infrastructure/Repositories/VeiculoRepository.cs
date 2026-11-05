using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Persistence;

namespace AutoVia.Infrastructure.Repositories
{
    /// <summary>
    /// Acesso a dados de veiculos (modulo Frota). Arquivo parcial (base comum SEAM):
    /// os metodos entram nos arquivos parciais de cada tarefa, com try/catch e log.
    /// </summary>
    public partial class VeiculoRepository : IVeiculoRepository
    {
        private readonly AutoViaDbContext _db;
        private readonly ILogService _logService;

        public VeiculoRepository(AutoViaDbContext db, ILogService logService)
        {
            _db = db;
            _logService = logService;
        }
    }
}
