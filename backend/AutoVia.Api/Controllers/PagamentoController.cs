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
        private readonly ICobrancaPixService _cobrancaPixService;
        private readonly ILogService _logService;
        private readonly TetoGeracaoPixPorCliente _tetoGeracaoPix;
        private readonly IPagamentoAtrasoService _pagamentoAtrasoService;

        public PagamentoController(IPagamentoService pagamentoService, ICobrancaPixService cobrancaPixService, ILogService logService, TetoGeracaoPixPorCliente tetoGeracaoPix, IPagamentoAtrasoService pagamentoAtrasoService)
        {
            _pagamentoService = pagamentoService;
            _cobrancaPixService = cobrancaPixService;
            _logService = logService;
            _tetoGeracaoPix = tetoGeracaoPix;
            _pagamentoAtrasoService = pagamentoAtrasoService;
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
        /// DINHEIRO ou TRANSFERENCIA). Caucao, cobranca ja paga e cancelada devolvem 409. Com PIX gerado, forma diferente de PIX
        /// e confirmacao, cancela o PIX no provedor antes; se o provedor falha, devolve 502 e nada e baixado. Devolve 200 com a cobranca.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPatch("{idPagamento:int}/baixa")]
        [ProducesResponseType(typeof(ReturnPagamentoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
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
            catch (IntegrationException)
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
        /// GerarOuReabrirPix - Gera o PIX de uma cobranca da locadora (PENDENTE ou ATRASADA, nunca caucao, paga ou cancelada), ou
        /// reabre o PIX valido gravado sem chamar o provedor. Sem corpo: valor, pagador e locadora vem do banco. Devolve 200 com o
        /// QR, o copia e cola, a validade e o identificador do provedor.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteGeracaoPix)]
        [HttpPost("{idPagamento:int}/pix")]
        [ProducesResponseType(typeof(ReturnCobrancaPixFuncionarioVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> GerarOuReabrirPix(int idPagamento)
        {
            try
            {
                VerificarTetoGeracaoPix(LerIdentidadeFuncionario());

                ReturnCobrancaPixFuncionarioVO pix = await _cobrancaPixService.GerarOuReabrirDaLocadoraAsync(idPagamento);

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

        /// <summary>
        /// ProcessarAtrasos - Rotina manual do financeiro (P10, RF-34): marca como ATRASADO os pagamentos PENDENTE vencidos da
        /// locadora do contexto. Sem corpo. Devolve 200 com so a quantidade marcada, sem dado de cobranca.
        /// </summary>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [HttpPost("atrasos/processar")]
        [ProducesResponseType(typeof(ReturnResultadoAtrasosVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ProcessarAtrasos()
        {
            try
            {
                ReturnResultadoAtrasosVO resultado = await _pagamentoAtrasoService.MarcarAtrasosDaLocadoraAsync();

                return Ok(resultado);
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
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosLocadora, GetType().ToString());
                throw;
            }
        }

        /// <summary>Le a identidade do funcionario pela claim NameIdentifier; sem identificador, recusa com 401.</summary>
        private string LerIdentidadeFuncionario()
        {
            Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);

            if (identificador == null || string.IsNullOrWhiteSpace(identificador.Value))
            {
                throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
            }

            return identificador.Value;
        }

        /// <summary>Conta a geracao de PIX no teto da identidade do funcionario (depois da autenticacao); recusa com 429 quando esgotado.</summary>
        private void VerificarTetoGeracaoPix(string identidade)
        {
            string chave = $"{ConstantsValues.PrefixoChaveLimiteGeracaoPixFuncionario}{identidade}";

            if (!_tetoGeracaoPix.TentarEntrar(chave))
            {
                throw new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes);
            }
        }
    }
}
