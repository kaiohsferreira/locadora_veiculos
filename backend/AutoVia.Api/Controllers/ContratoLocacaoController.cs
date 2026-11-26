using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Contratos;
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

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Contrato digital da locacao (RF-21, RP-06; contrato-locacao v5). So ATENDENTE e ADMINISTRADOR da locadora do contexto
    /// (PoliticaContratoLocacao). Sem regra de negocio aqui: o servico decide, e a resposta e sempre um VO de retorno.
    /// </summary>
    [ApiController]
    [Route("api/locacoes/{idLocacao:int}/contrato")]
    public class ContratoLocacaoController : ControllerBase
    {
        private readonly IContratoLocacaoService _contratoService;
        private readonly ILogService _logService;

        public ContratoLocacaoController(IContratoLocacaoService contratoService, ILogService logService)
        {
            _contratoService = contratoService;
            _logService = logService;
        }

        /// <summary>Retorna o contrato da locacao: o documento montado enquanto nao assinado; a copia guardada depois de assinado.</summary>
        [HttpGet]
        [Authorize(Policy = ConstantsValues.PoliticaContratoLocacao)]
        [ProducesResponseType(typeof(ReturnContratoLocacaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ObterContrato(int idLocacao)
        {
            try
            {
                ReturnContratoLocacaoVO contrato = await _contratoService.ObterAsync(idLocacao);

                return Ok(contrato);
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
                _logService.Write(ex, ConstantsMessage.ErroBuscarContratoLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Coleta a assinatura do contrato: grava o documento, o SHA-256, a imagem e o funcionario, e devolve a rota de consulta em Location.</summary>
        [HttpPost("assinatura")]
        [Authorize(Policy = ConstantsValues.PoliticaContratoLocacao)]
        [RequestSizeLimit(ConstantsValues.ContratoAssinaturaLimiteCorpoBytes)]
        [ProducesResponseType(typeof(ReturnContratoLocacaoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssinarContrato(int idLocacao, [FromBody] AssinarContratoLocacaoVO? model)
        {
            try
            {
                Claim? claim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (claim == null || !int.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) || idFuncionario <= 0)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                AssinarContratoLocacaoVO corpo = model ?? new AssinarContratoLocacaoVO();
                ReturnContratoLocacaoVO assinado = await _contratoService.AssinarAsync(idLocacao, idFuncionario, corpo);

                return CreatedAtAction(nameof(ObterContrato), new { idLocacao }, assinado);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAssinarContratoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
