using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Devolucao;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers;

[ApiController]
[Route("api/locacoes/{idLocacao:int}")]
[Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
public class DevolucaoController : ControllerBase
{
    private readonly IDevolucaoService _service;
    private readonly ILogService _logService;

    public DevolucaoController(IDevolucaoService service, ILogService logService)
    {
        _service = service;
        _logService = logService;
    }

    /// <summary>
    /// Encerra a locacao na devolucao (RF-25): apura as cobrancas extras, resolve a caucao, lanca a diaria e devolve o veiculo.
    /// Exige a vistoria de saida. So ATENDENTE e ADMINISTRADOR (D6). Repetir responde 200 sem mexer em nada.
    /// </summary>
    [HttpPost("devolucao")]
    [Authorize(Policy = ConstantsValues.PoliticaEncerramentoLocacao)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ReturnEncerramentoLocacaoVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Encerrar(int idLocacao, [FromBody] EncerrarLocacaoVO model)
    {
        try
        {
            Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) || idFuncionario <= 0)
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            ReturnEncerramentoLocacaoVO resposta = await _service.EncerrarAsync(idLocacao, model);
            return Ok(resposta);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (ValidationException) { throw; }
        catch (UnauthorizedException) { throw; }
        catch (IntegrationException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroEncerrarLocacao, GetType().ToString());
            throw;
        }
    }

    /// <summary>
    /// Lista as cobrancas extras lancadas na locacao, sem recalcular e sem provedor. Sem apuracao devolve lista vazia.
    /// </summary>
    [HttpGet("cobrancas")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ReturnCobrancasLocacaoVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListarCobrancas(int idLocacao)
    {
        try
        {
            ReturnCobrancasLocacaoVO resposta = await _service.ListarCobrancasAsync(idLocacao);
            return Ok(resposta);
        }
        catch (BusinessException) { throw; }
        catch (NotFoundException) { throw; }
        catch (Exception ex)
        {
            _logService.Write(ex, ConstantsMessage.ErroListarCobrancasLocacao, GetType().ToString());
            throw;
        }
    }
}
