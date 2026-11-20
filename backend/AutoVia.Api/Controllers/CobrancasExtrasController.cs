using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoVia.Api.Controllers;

[ApiController]
[Route("api/locacoes/{idLocacao:int}/cobrancas-extras")]
[Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public sealed class CobrancasExtrasController : ControllerBase
{
    private readonly IApuracaoCobrancasExtrasService _service;
    private readonly ILogService _logService;
    public CobrancasExtrasController(IApuracaoCobrancasExtrasService service, ILogService logService)
    { _service = service; _logService = logService; }

    /// <summary>Simula os quatro tipos de cobrança usando a configuração cadastrada para a categoria.</summary>
    [HttpPost("simulacao")]
    [ProducesResponseType(typeof(ReturnSimulacaoCobrancasExtrasVO), StatusCodes.Status200OK)]
    public async Task<IActionResult> Simular(int idLocacao)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ReturnSimulacaoCobrancasExtrasVO resultado = await _service.SimularAsync(idLocacao);
            return Ok(resultado);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroSimularCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Consulta a apuração registrada e sua memória imutável.</summary>
    [HttpGet("apuracao")]
    [ProducesResponseType(typeof(ReturnApuracaoCobrancasExtrasVO), StatusCodes.Status200OK)]
    public async Task<IActionResult> BuscarApuracao(int idLocacao)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ReturnApuracaoCobrancasExtrasVO resultado = await _service.BuscarAsync(idLocacao);
            return Ok(resultado);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Registra cobranças atomicamente; repetições devolvem a apuração anterior.</summary>
    [HttpPost("apuracao")]
    [ProducesResponseType(typeof(ReturnApuracaoCobrancasExtrasVO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReturnApuracaoCobrancasExtrasVO), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegistrarApuracao(int idLocacao, [FromBody] ApurarCobrancasExtrasVO model)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ResultadoRegistroApuracaoVO resultado = await _service.RegistrarAsync(idLocacao, model);
            if (!resultado.Criada) return Ok(resultado.Apuracao);
            return CreatedAtAction(nameof(BuscarApuracao), new { idLocacao }, resultado.Apuracao);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroApuracaoCobrancasExtras, GetType().ToString()); throw; }
    }
}
