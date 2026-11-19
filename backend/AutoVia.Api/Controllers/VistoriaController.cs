using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    [ApiController]
    [Route("api/locacoes/{idLocacao:int}/vistorias/entrada")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public class VistoriaController : ControllerBase
    {
        private readonly IVistoriaService _vistoriaService;
        private readonly ILogService _logService;

        public VistoriaController(IVistoriaService vistoriaService, ILogService logService)
        {
            _vistoriaService = vistoriaService;
            _logService = logService;
        }

        /// <summary>Registra a vistoria de entrada da locacao e devolve a rota de consulta em Location.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnVistoriaVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RegistrarEntrada(int idLocacao, [FromBody] VistoriaEntradaVO entrada)
        {
            try
            {
                Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) || idFuncionario <= 0)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                ReturnVistoriaVO criada = await _vistoriaService.RegistrarEntradaAsync(idLocacao, entrada, idFuncionario);
                return CreatedAtAction(nameof(GetEntrada), new { idLocacao }, criada);
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
                _logService.Write(ex, ConstantsMessage.ErroSalvarVistoria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta a vistoria de entrada ativa de uma locacao da locadora autenticada.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ReturnVistoriaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEntrada(int idLocacao)
        {
            try
            {
                ReturnVistoriaVO vistoria = await _vistoriaService.GetEntradaAsync(idLocacao);
                return Ok(vistoria);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarVistoria, GetType().ToString());
                throw;
            }
        }
    }
}
