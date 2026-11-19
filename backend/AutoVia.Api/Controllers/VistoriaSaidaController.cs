using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace AutoVia.Api.Controllers;

[ApiController]
[Route("api/locacoes/{idLocacao:int}")]
[Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
public class VistoriaSaidaController : ControllerBase
{
    private readonly IVistoriaSaidaService _service;
    private readonly ILogService _logService;

    public VistoriaSaidaController(IVistoriaSaidaService service, ILogService logService)
    {
        _service = service;
        _logService = logService;
    }

    /// <summary>Prepara a devolucao com a locacao, entrada e comparacao de saida, quando existente.</summary>
    [HttpGet("devolucao")]
    [ProducesResponseType(typeof(ContextoDevolucaoVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContexto(int idLocacao)
    {
        try
        {
            ContextoDevolucaoVO contexto = await _service.GetContextoAsync(idLocacao);
            return Ok(contexto);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    /// <summary>Consulta a saida e os grupos historicos de avarias da locacao autenticada.</summary>
    [HttpGet("vistorias/saida")]
    [ProducesResponseType(typeof(ComparacaoSaidaVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSaida(int idLocacao)
    {
        try
        {
            ComparacaoSaidaVO saida = await _service.GetSaidaAsync(idLocacao);
            return Ok(saida);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroBuscarVistoriaSaida, GetType().ToString());
            throw;
        }
    }

    /// <summary>Registra atomicamente a saida e snapshots de avarias, sem finalizar a locacao.</summary>
    [HttpPost("vistorias/saida")]
    [ProducesResponseType(typeof(ComparacaoSaidaVO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegistrarSaida(int idLocacao, [FromBody] VistoriaSaidaVO model)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) || idFuncionario <= 0)
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ComparacaoSaidaVO saida = await _service.RegistrarSaidaAsync(idLocacao, model, idFuncionario);
            return CreatedAtAction(nameof(GetSaida), new { idLocacao }, saida);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroSalvarVistoriaSaida, GetType().ToString());
            throw;
        }
    }
}
