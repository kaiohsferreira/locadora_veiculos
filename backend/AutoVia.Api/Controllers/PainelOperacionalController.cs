using AutoVia.Communication.ViewObjects.PainelOperacional;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    [ApiController]
    [Route("api/painel-operacional")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    [ProducesResponseType(typeof(ReturnPainelDiarioVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class PainelOperacionalController : ControllerBase
    {
        private readonly IPainelOperacionalService _painelService;
        private readonly ILogService _logService;

        public PainelOperacionalController(IPainelOperacionalService painelService, ILogService logService)
        {
            _painelService = painelService;
            _logService = logService;
        }

        /// <summary>Consulta as retiradas e devolucoes previstas para o dia atual.</summary>
        [HttpGet("hoje")]
        public async Task<IActionResult> GetHojeAsync()
        {
            try
            {
                ReturnPainelDiarioVO retorno = await _painelService.GetPainelDiarioAsync();
                return Ok(retorno);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarPainelOperacional, GetType().ToString());
                throw;
            }
        }
    }
}
