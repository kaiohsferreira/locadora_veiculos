using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints de veiculos (modulo Frota). Restrito a funcionarios pela politica
    /// Funcionario, aplicada na classe. Arquivo parcial (base comum SEAM): as acoes
    /// entram nos arquivos parciais de cada tarefa. Sem regra de negocio aqui.
    /// </summary>
    [ApiController]
    [Route("api/veiculos")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public partial class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _veiculoService;
        private readonly ILogService _logService;

        public VeiculoController(IVeiculoService veiculoService, ILogService logService)
        {
            _veiculoService = veiculoService;
            _logService = logService;
        }
    }
}
