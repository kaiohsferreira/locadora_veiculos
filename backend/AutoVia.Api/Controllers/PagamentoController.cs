using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints da cobranca para a locadora (RF-31): lancamento, baixa e listagem. Restrito ao financeiro (FINANCEIRO e
    /// ADMINISTRADOR) pela politica Financeiro, aplicada na classe. Sem regra de negocio: a entrada e validada pelo
    /// FluentValidation (antes da action), a logica fica no IPagamentoService e a resposta sai so em VO. Erros viram
    /// ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/pagamentos")]
    [Authorize(Policy = ConstantsValues.PoliticaFinanceiro)]
    public class PagamentoController : ControllerBase
    {
        private readonly IPagamentoService _pagamentoService;
        private readonly ILogService _logService;

        public PagamentoController(IPagamentoService pagamentoService, ILogService logService)
        {
            _pagamentoService = pagamentoService;
            _logService = logService;
        }

        /// <summary>
        /// Lancar - Lanca uma cobranca PENDENTE, por locacao OU por venda (nunca as duas), com tipo DIARIA, COBRANCA_EXTRA
        /// ou VENDA, valor e data de vencimento. Devolve 201 com a cobranca criada, sem cabecalho Location.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPost]
        [ProducesResponseType(typeof(ReturnPagamentoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Lancar([FromBody] LancarPagamentoVO lancarPagamentoVO)
        {
            try
            {
                ReturnPagamentoVO criado = await _pagamentoService.LancarAsync(lancarPagamentoVO);

                return StatusCode(StatusCodes.Status201Created, criado);
            }
            catch (BusinessException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroLancarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Baixar - Da baixa numa cobranca PENDENTE ou ATRASADA: vira PAGO com a data de hoje e a forma (PIX, CARTAO, BOLETO,
        /// DINHEIRO ou TRANSFERENCIA). Caucao, cobranca ja paga e cancelada devolvem 409. Devolve 200 com a cobranca.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPatch("{idPagamento:int}/baixa")]
        [ProducesResponseType(typeof(ReturnPagamentoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Baixar(int idPagamento, [FromBody] BaixarPagamentoVO baixarPagamentoVO)
        {
            try
            {
                ReturnPagamentoVO baixado = await _pagamentoService.BaixarAsync(idPagamento, baixarPagamentoVO);

                return Ok(baixado);
            }
            catch (BusinessException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroBaixarPagamento, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Listar - Lista as cobrancas da locadora, com status exato (PENDENTE, PAGO, ATRASADO ou CANCELADO), intervalo de
        /// vencimento e paginacao. Inclui a caucao. Devolve 200 com a pagina.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpGet]
        [ProducesResponseType(typeof(PaginaVO<ReturnPagamentoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Listar([FromQuery] FiltroPagamentoVO filtro)
        {
            try
            {
                PaginaVO<ReturnPagamentoVO> pagina = await _pagamentoService.ListarAsync(filtro);

                return Ok(pagina);
            }
            catch (BusinessException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroListarPagamentos, GetType().ToString());
                throw;
            }
        }
    }
}
