using AutoVia.Api.Requests.Avaria;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Avarias;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>Endpoints de registro e armazenamento das fotos de avarias.</summary>
    [ApiController]
    [Route("api/vistorias/{idVistoria:int}/avarias")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public sealed class AvariaController : ControllerBase
    {
        private readonly IAvariaService _avariaService;
        private readonly ILogService _logService;

        public AvariaController(IAvariaService avariaService, ILogService logService)
        {
            _avariaService = avariaService;
            _logService = logService;
        }

        /// <summary>Cria a avaria, armazena a foto e devolve uma URL temporaria.</summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoFotoAvaria)]
        [ProducesResponseType(typeof(ReturnAvariaVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Criar(int idVistoria, [FromForm] CriarAvariaRequest request)
        {
            try
            {
                (byte[] conteudo, string contentType) = await LerFotoAsync(request.Foto, HttpContext.RequestAborted);
                ReturnAvariaVO criada = await _avariaService.CriarAsync(idVistoria, request.Descricao, request.Gravidade, request.ValorEstimado, conteudo, contentType, HttpContext.RequestAborted);
                return CreatedAtAction(nameof(GetById), new { idVistoria, idAvaria = criada.IdAvaria }, criada);
            }
            catch (AvariaUploadException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                ProblemDetails detalhe = new ProblemDetails
                {
                    Status = StatusCodes.Status502BadGateway,
                    Title = ConstantsMessage.TituloErroIntegracao,
                    Detail = ConstantsMessage.ErroUploadAvariaPendente
                };
                detalhe.Extensions[ConstantsValues.ExtensaoProblemaIdAvaria] = ex.IdAvaria;
                detalhe.Extensions[ConstantsValues.ExtensaoProblemaRetryUploadUrl] = string.Format(CultureInfo.InvariantCulture, ConstantsValues.FormatoRotaRetryFotoAvaria, ex.IdVistoria, ex.IdAvaria);
                return StatusCode(StatusCodes.Status502BadGateway, detalhe);
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroCriarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Consulta os dados da avaria e emite URL somente se HEAD confirmar a foto.</summary>
        [HttpGet("{idAvaria:int}")]
        [ProducesResponseType(typeof(ReturnAvariaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int idVistoria, int idAvaria)
        {
            try
            {
                ReturnAvariaVO avaria = await _avariaService.GetByIdAsync(idVistoria, idAvaria, HttpContext.RequestAborted);
                return Ok(avaria);
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Repete o upload de foto usando a chave deterministica da avaria existente.</summary>
        [HttpPut("{idAvaria:int}/foto")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoFotoAvaria)]
        [ProducesResponseType(typeof(ReturnAvariaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> RetryUpload(int idVistoria, int idAvaria, [FromForm] RetryAvariaFotoRequest request)
        {
            try
            {
                (byte[] conteudo, string contentType) = await LerFotoAsync(request.Foto, HttpContext.RequestAborted);
                ReturnAvariaVO avaria = await _avariaService.RetryUploadAsync(idVistoria, idAvaria, conteudo, contentType, HttpContext.RequestAborted);
                return Ok(avaria);
            }
            catch (AvariaUploadException ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                return CriarProblemRetry(ex);
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroArmazenarFotoAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Emite nova URL temporaria apos confirmar a existencia do objeto via HEAD.</summary>
        [HttpGet("{idAvaria:int}/foto/url")]
        [ProducesResponseType(typeof(ReturnAvariaVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GetFotoUrl(int idVistoria, int idAvaria)
        {
            try
            {
                ReturnAvariaVO avaria = await _avariaService.GetFotoUrlAsync(idVistoria, idAvaria, HttpContext.RequestAborted);
                return Ok(avaria);
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroConsultarAvaria, GetType().ToString());
                throw;
            }
        }

        /// <summary>Remove somente o objeto da foto; mantem a avaria e permite repeticao idempotente.</summary>
        [HttpDelete("{idAvaria:int}/foto")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> RemoverFoto(int idVistoria, int idAvaria)
        {
            try
            {
                await _avariaService.RemoverFotoAsync(idVistoria, idAvaria, HttpContext.RequestAborted);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logService.Write(CriarErroSanitizado(ex), ConstantsMessage.ErroRemoverFotoAvaria, GetType().ToString());
                throw;
            }
        }

        private static async Task<(byte[] Conteudo, string ContentType)> LerFotoAsync(IFormFile? foto, CancellationToken cancellationToken)
        {
            if (foto == null)
                throw new ValidationException(ConstantsMessage.ErroAvariaFotoObrigatoria);
            if (foto.Length <= 0 || foto.Length > ConstantsArmazenamento.TamanhoMaximoFotoBytes)
                throw new ValidationException(ConstantsMessage.ErroAvariaFotoFormatoInvalido);
            using MemoryStream memoria = new MemoryStream();
            await foto.CopyToAsync(memoria, cancellationToken);
            return (memoria.ToArray(), foto.ContentType);
        }

        private ObjectResult CriarProblemRetry(AvariaUploadException excecao)
        {
            ProblemDetails detalhe = new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = ConstantsMessage.TituloErroIntegracao,
                Detail = ConstantsMessage.ErroUploadAvariaPendente
            };
            detalhe.Extensions[ConstantsValues.ExtensaoProblemaIdAvaria] = excecao.IdAvaria;
            detalhe.Extensions[ConstantsValues.ExtensaoProblemaRetryUploadUrl] = string.Format(CultureInfo.InvariantCulture, ConstantsValues.FormatoRotaRetryFotoAvaria, excecao.IdVistoria, excecao.IdAvaria);
            return StatusCode(StatusCodes.Status502BadGateway, detalhe);
        }

        private static Exception CriarErroSanitizado(Exception exception)
        {
            return new Exception(exception.GetType().Name);
        }
    }
}
