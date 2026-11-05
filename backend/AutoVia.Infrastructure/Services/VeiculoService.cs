using AutoMapper;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Regras de negocio de veiculos (modulo Frota). Arquivo parcial (base comum SEAM):
    /// os metodos entram nos arquivos parciais de cada tarefa, com try/catch e log.
    /// </summary>
    public partial class VeiculoService : IVeiculoService
    {
        private readonly IVeiculoRepository _veiculoRepository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IMapper _mapper;
        private readonly ILogService _logService;

        public VeiculoService(
            IVeiculoRepository veiculoRepository,
            ICategoriaRepository categoriaRepository,
            IMapper mapper,
            ILogService logService)
        {
            _veiculoRepository = veiculoRepository;
            _categoriaRepository = categoriaRepository;
            _mapper = mapper;
            _logService = logService;
        }
    }
}
