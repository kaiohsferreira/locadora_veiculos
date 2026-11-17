using AutoVia.Api.Requests.VeiculoFoto;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Fotos do veiculo (envio, listagem e remocao). Restrito a funcionarios pela politica Funcionario, na classe. A locadora vem
    /// do contexto da sessao; veiculo de outra locadora responde 404. Sem regra de negocio aqui: o servico decide.
    /// </summary>
    [ApiController]
    [Route("api/veiculos/{idVeiculo:int}/fotos")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public sealed class VeiculoFotoController : ControllerBase
    {
        private readonly IVeiculoFotoService _veiculoFotoService;
        private readonly ILogService _logService;

        public VeiculoFotoController(IVeiculoFotoService veiculoFotoService, ILogService logService)
        {
            _veiculoFotoService = veiculoFotoService;
            _logService = logService;
        }

        /// <summary>Enviar - Envia uma foto JPEG ou PNG para o veiculo da locadora, guarda a versao recodificada no armazenamento e devolve a foto com URL temporaria.</summary>
        [HttpPost]
        [Consumes(ConstantsValues.TipoMultipartFormData)]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoFotoVeiculo)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteEnvioFotoVeiculo)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnVeiculoFotoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Enviar(int idVeiculo, [FromForm] EnviarFotoVeiculoRequest request)
        {
            try
            {
                (byte[]? conteudo, string? contentType) = await LerFotoAsync(request.Foto, HttpContext.RequestAborted);
                ReturnVeiculoFotoVO foto = await _veiculoFotoService.EnviarAsync(idVeiculo, conteudo, contentType, HttpContext.RequestAborted);
                return CreatedAtAction(nameof(GetAll), new { idVeiculo }, foto);
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
            catch (FluentValidation.ValidationException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEnviarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>GetAll - Lista as fotos ativas do veiculo da locadora, em ordem, com URL temporaria.</summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(List<ReturnVeiculoFotoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GetAll(int idVeiculo)
        {
            try
            {
                List<ReturnVeiculoFotoVO> fotos = await _veiculoFotoService.GetAllAsync(idVeiculo, HttpContext.RequestAborted);
                return Ok(fotos);
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
            catch (System.Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarFotosVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Remover - Remove a foto do veiculo: apaga o arquivo no armazenamento e desativa o registro.</summary>
        [HttpDelete("{idFoto:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Remover(int idVeiculo, int idFoto)
        {
            try
            {
                await _veiculoFotoService.RemoverAsync(idVeiculo, idFoto, HttpContext.RequestAborted);
                return NoContent();
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
            catch (System.Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRemoverFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>Copia o arquivo do formulario para a memoria. Sem arquivo devolve nulos: o validador ja recusou antes da action.</summary>
        private static async Task<(byte[]? Conteudo, string? ContentType)> LerFotoAsync(IFormFile? foto, System.Threading.CancellationToken cancellationToken)
        {
            if (foto == null)
            {
                return (null, null);
            }

            using MemoryStream memoria = new MemoryStream();
            await foto.CopyToAsync(memoria, cancellationToken);

            return (memoria.ToArray(), foto.ContentType);
        }
    }
}
