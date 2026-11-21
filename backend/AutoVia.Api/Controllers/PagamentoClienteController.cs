using AutoVia.Api.Extensions;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
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
        private readonly ICobrancaPixService _cobrancaPixService;
        private readonly ILogService _logService;
        private readonly TetoGeracaoPixPorCliente _tetoGeracaoPix;

        public PagamentoClienteController(IPagamentoService pagamentoService, ICobrancaPixService cobrancaPixService, ILogService logService, TetoGeracaoPixPorCliente tetoGeracaoPix)
        {
            _pagamentoService = pagamentoService;
            _cobrancaPixService = cobrancaPixService;
            _logService = logService;
            _tetoGeracaoPix = tetoGeracaoPix;
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

        /// <summary>
        /// GerarOuReabrirPixMeuPagamento - Gera o PIX de uma cobranca propria (PENDENTE ou ATRASADA, nunca caucao, paga ou
        /// cancelada), ou reabre o PIX valido gravado sem chamar o provedor. Sem corpo: valor, pagador e locadora vem do banco.
        /// Devolve 200 com o QR, o copia e cola e a validade (sem o identificador do provedor).
        /// </summary>
        [HttpPost("meus/{idPagamento:int}/pix")]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteGeracaoPix)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnCobrancaPixVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GerarOuReabrirPixMeuPagamento(int idPagamento)
        {
            try
            {
                int idCliente = LerIdCliente();
                VerificarTetoGeracaoPix(idCliente);

                ReturnCobrancaPixVO pix = await _cobrancaPixService.GerarOuReabrirDoClienteAsync(idCliente, idPagamento);

                return Ok(pix);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
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
            catch (IntegrationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroGerarPixPagamento, GetType().ToString());
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

        /// <summary>Conta a geracao de PIX no teto do proprio cliente (depois da autenticacao); recusa com 429 quando esgotado.</summary>
        private void VerificarTetoGeracaoPix(int idCliente)
        {
            string chave = $"{ConstantsValues.PrefixoChaveLimiteGeracaoPixCliente}{idCliente}";

            if (!_tetoGeracaoPix.TentarEntrar(chave))
            {
                throw new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes);
            }
        }
    }
}
