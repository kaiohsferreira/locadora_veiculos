using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoVia.Api.Controllers;

[ApiController]
[Route("api/configuracoes-cobrancas-extras")]
[Authorize(Policy = ConstantsValues.PoliticaAdministrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public sealed class ConfiguracoesCobrancasExtrasController : ControllerBase
{
    private readonly IConfiguracoesCobrancasExtrasService _service;
    private readonly ILogService _logService;
    public ConfiguracoesCobrancasExtrasController(IConfiguracoesCobrancasExtrasService service, ILogService logService)
    { _service = service; _logService = logService; }

    /// <summary>Lista as configurações ativas da locadora autenticada.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginaVO<ReturnConfiguracaoCobrancaExtraVO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] FiltroConfiguracaoCobrancaExtraVO filtro)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            PaginaVO<ReturnConfiguracaoCobrancaExtraVO> resultado = await _service.ListarAsync(filtro);
            return Ok(resultado);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Consulta uma configuração ativa da locadora autenticada.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ReturnConfiguracaoCobrancaExtraVO), StatusCodes.Status200OK)]
    public async Task<IActionResult> Buscar(int id)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ReturnConfiguracaoCobrancaExtraVO resultado = await _service.BuscarAsync(id);
            return Ok(resultado);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Cadastra parâmetros comerciais explícitos para uma categoria da locadora.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReturnConfiguracaoCobrancaExtraVO), StatusCodes.Status201Created)]
    public async Task<IActionResult> Inserir([FromBody] ConfiguracaoCobrancaExtraVO model)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ReturnConfiguracaoCobrancaExtraVO resultado = await _service.InserirAsync(model);
            return CreatedAtAction(nameof(Buscar), new { id = resultado.IdConfiguracao }, resultado);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Atualiza os parâmetros mediante conferência da versão esperada.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ConfiguracaoCobrancaExtraVO model)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            await _service.AtualizarAsync(id, model);
            return NoContent();
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }

    /// <summary>Desativa logicamente a configuração preservando as apurações anteriores.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desativar(int id)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || string.IsNullOrWhiteSpace(claim.Value)) throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            await _service.DesativarAsync(id);
            return NoContent();
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex) { _logService.Write(ex, ConstantsMessage.ErroConfiguracaoCobrancasExtras, GetType().ToString()); throw; }
    }
}
