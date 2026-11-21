using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Proposta de compra de seminovo (P6-BE-02, RF-27). Envio e detalhe sao do cliente autenticado (politica Cliente, como
    /// a reserva). A simulacao e PUBLICA: o [AllowAnonymous] sobrepoe a politica da classe, porque a tela 31 calcula as
    /// parcelas antes do login; a rota dela fica sob /api/veiculos, ao lado do catalogo. Sem regra de negocio aqui.
    /// </summary>
    [ApiController]
    [Route("api/propostas-compra")]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class PropostaCompraController : ControllerBase
    {
        private readonly IPropostaCompraService _propostaCompraService;
        private readonly ILogService _logService;

        public PropostaCompraController(IPropostaCompraService propostaCompraService, ILogService logService)
        {
            _propostaCompraService = propostaCompraService;
            _logService = logService;
        }

        /// <summary>
        /// SimularAsync - Simula o parcelamento de um veiculo do catalogo (entrada e numero de parcelas), sem gravar nada.
        /// </summary>
        [HttpGet("~/api/veiculos/{idVeiculo:int}/simulacao")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteLeituraPublica)]
        [ProducesResponseType(typeof(ReturnSimulacaoParcelamentoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SimularAsync(int idVeiculo, [FromQuery] SimulacaoParcelamentoVO simulacao)
        {
            try
            {
                ReturnSimulacaoParcelamentoVO retorno = await _propostaCompraService.SimularAsync(idVeiculo, simulacao);
                return Ok(retorno);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSimularParcelamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>CriarAsync - Registra a proposta de compra do cliente autenticado.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnPropostaCompraVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> CriarAsync([FromBody] PropostaCompraVO propostaVO)
        {
            try
            {
                int idCliente = ObterIdClienteAutenticado();

                ReturnPropostaCompraVO retorno = await _propostaCompraService.CriarAsync(idCliente, propostaVO);
                return CreatedAtAction(nameof(GetByIdAsync), new { idPropostaCompra = retorno.IdPropostaCompra }, retorno);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRegistrarProposta, GetType().ToString());
                throw;
            }
        }

        /// <summary>GetByIdAsync - Retorna uma proposta do cliente autenticado (a de outro cliente responde 404).</summary>
        [HttpGet("{idPropostaCompra:int}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ActionName(nameof(GetByIdAsync))]
        [ProducesResponseType(typeof(ReturnPropostaCompraVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetByIdAsync(int idPropostaCompra)
        {
            try
            {
                int idCliente = ObterIdClienteAutenticado();

                ReturnPropostaCompraVO retorno = await _propostaCompraService.GetByIdAsync(idPropostaCompra, idCliente);
                return Ok(retorno);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarProposta, GetType().ToString());
                throw;
            }
        }

        /// <summary>Id do cliente pelo claim NameIdentifier do token; sem claim valido, 401.</summary>
        private int ObterIdClienteAutenticado()
        {
            try
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
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroNaoAutenticado, GetType().ToString());
                throw;
            }
        }
    }
}
