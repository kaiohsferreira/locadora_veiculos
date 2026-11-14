using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    [ApiController]
    [Route("api/veiculos/disponiveis")]
    [AllowAnonymous]
    [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
    public class VeiculoDisponibilidadeController : ControllerBase
    {
        private readonly IVeiculoService _veiculoService;
        private readonly ILogService _logService;

        public VeiculoDisponibilidadeController(IVeiculoService veiculoService, ILogService logService)
        {
            _veiculoService = veiculoService;
            _logService = logService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PaginaVO<ReturnVeiculoDisponivelVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetDisponiveis([FromQuery] FiltroVeiculoDisponibilidadeVO filtro)
        {
            try
            {
                PaginaVO<ReturnVeiculoDisponivelVO> pagina = await _veiculoService.GetDisponiveisAsync(filtro);
                return Ok(pagina);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarDisponibilidade, GetType().ToString());
                throw;
            }
        }
    }
}
