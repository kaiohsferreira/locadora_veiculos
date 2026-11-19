using AutoVia.Api.Requests.ClienteDocumento;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Documentos do cliente (CNH e comprovante de residencia, RF-08): envio e listagem. So o proprio cliente logado (politica
    /// Cliente na classe; o id da rota tem de ser o da sessao, senao 404 no servico). Sem regra de negocio aqui.
    /// </summary>
    [ApiController]
    [Route(ConstantsValues.RotaClienteDocumento)]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    public sealed class ClienteDocumentoController : ControllerBase
    {
        private readonly IClienteDocumentoService _clienteDocumentoService;
        private readonly ILogService _logService;

        public ClienteDocumentoController(IClienteDocumentoService clienteDocumentoService, ILogService logService)
        {
            _clienteDocumentoService = clienteDocumentoService;
            _logService = logService;
        }

        /// <summary>Enviar - Envia a CNH ou o comprovante de residencia do cliente logado (PDF, JPG ou PNG ate 10 MB), guarda o arquivo como veio e devolve o documento com URL temporaria.</summary>
        [HttpPost]
        [Consumes(ConstantsValues.TipoMultipartFormData)]
        [RequestSizeLimit(ConstantsValues.TamanhoMaximoCorpoDocumentoCliente)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteEnvioDocumentoCliente)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnClienteDocumentoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Enviar(int idCliente, [FromForm] EnviarDocumentoClienteRequest request)
        {
            try
            {
                int idClienteSessao = LerIdClienteSessao();
                byte[]? conteudo = await LerArquivoAsync(request.Arquivo, HttpContext.RequestAborted);
                ReturnClienteDocumentoVO documento = await _clienteDocumentoService.EnviarAsync(
                    idCliente,
                    idClienteSessao,
                    request.Tipo,
                    conteudo,
                    request.Arquivo?.FileName,
                    HttpContext.RequestAborted);

                return CreatedAtAction(nameof(Listar), new { idCliente }, documento);
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
                _logService.Write(ex, ConstantsMessage.ErroEnviarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Listar - Lista os documentos ativos do cliente logado (no maximo um por tipo, CNH antes do comprovante), cada um com URL temporaria nova.</summary>
        [HttpGet]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteListaDocumentoCliente)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(List<ReturnClienteDocumentoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Listar(int idCliente)
        {
            try
            {
                int idClienteSessao = LerIdClienteSessao();
                List<ReturnClienteDocumentoVO> documentos = await _clienteDocumentoService.ListarAsync(idCliente, idClienteSessao, HttpContext.RequestAborted);

                return Ok(documentos);
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
                _logService.Write(ex, ConstantsMessage.ErroListarClienteDocumento, GetType().ToString());
                throw;
            }
        }

        /// <summary>Le o id do cliente da claim de identificador da sessao; ausente ou invalido responde 401.</summary>
        private int LerIdClienteSessao()
        {
            Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
            int idCliente = 0;
            bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);

            if (!identificadorValido)
            {
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            }

            return idCliente;
        }

        /// <summary>
        /// Le o arquivo do formulario para um vetor do tamanho exato, direto do fluxo (uma copia so). Sem arquivo devolve nulo:
        /// o validador ja recusou antes da action.
        /// </summary>
        private static async Task<byte[]?> LerArquivoAsync(IFormFile? arquivo, System.Threading.CancellationToken cancellationToken)
        {
            if (arquivo == null)
            {
                return null;
            }

            byte[] conteudo = new byte[arquivo.Length];
            using Stream fluxo = arquivo.OpenReadStream();
            await fluxo.ReadExactlyAsync(conteudo, 0, conteudo.Length, cancellationToken);

            return conteudo;
        }
    }
}
