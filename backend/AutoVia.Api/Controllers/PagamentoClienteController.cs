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
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Endpoints das cobrancas do proprio cliente (RF-36): listagem e resumo. Restrito ao perfil CLIENTE pela politica
    /// Cliente, na classe. O id do cliente vem da claim do token (nunca do corpo ou da rota), e as consultas so devolvem as
    /// cobrancas dele. As rotas literais meus e meus/resumo nao colidem com as da locadora (id com restricao int).
    /// </summary>
    [ApiController]
    [Route("api/pagamentos")]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    public class PagamentoClienteController : ControllerBase
    {
        private readonly IPagamentoService _pagamentoService;
        private readonly ILogService _logService;

        public PagamentoClienteController(IPagamentoService pagamentoService, ILogService logService)
        {
            _pagamentoService = pagamentoService;
            _logService = logService;
        }

        /// <summary>
        /// MeusPagamentos - Lista as proprias cobrancas do cliente autenticado, com aba (TODOS, PENDENTES ou PAGOS), periodo
        /// (DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO) e paginacao. Cada item traz situacao e dias para vencer. Devolve 200.
        /// </summary>
        [HttpGet("meus")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(PaginaVO<ReturnMeuPagamentoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ListarMeus([FromQuery] FiltroMeusPagamentosVO filtro)
        {
            try
            {
                int idCliente = LerIdCliente();

                PaginaVO<ReturnMeuPagamentoVO> pagina = await _pagamentoService.ListarMeusAsync(idCliente, filtro);

                return Ok(pagina);
            }
            catch (UnauthorizedException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroListarMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// ResumoMeusPagamentos - Resumo das cobrancas do cliente autenticado: total liquidado no ano, quitadas no ano, total e
        /// quantidade pendentes e contagens das abas. So o periodo afeta a contagem da aba Pagos. Devolve 200.
        /// </summary>
        [HttpGet("meus/resumo")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnResumoMeusPagamentosVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ResumoMeus([FromQuery] FiltroResumoMeusPagamentosVO filtro)
        {
            try
            {
                int idCliente = LerIdCliente();

                ReturnResumoMeusPagamentosVO resumo = await _pagamentoService.ResumirMeusAsync(idCliente, filtro);

                return Ok(resumo);
            }
            catch (UnauthorizedException)
            {
                throw;
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
                _logService.Write(ex, ConstantsMessage.ErroResumirMeusPagamentos, GetType().ToString());
                throw;
            }
        }

        /// <summary>Le o id do cliente pela claim NameIdentifier; sem id numerico valido, recusa com 401.</summary>
        private int LerIdCliente()
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
    }
}
