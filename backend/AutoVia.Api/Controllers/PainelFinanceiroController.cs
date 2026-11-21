using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Painel financeiro da locadora (P7-BE-05, RF-35): indicadores de cobranca e lista de titulos. Politica Financeiro (cargo
    /// FINANCEIRO ou ADMINISTRADOR; demais cargos e cliente recebem 403). Sem regra de negocio aqui: o FluentValidation roda antes
    /// da action, o servico calcula e o controller devolve o VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/painel/financeiro")]
    [Authorize(Policy = ConstantsValues.PoliticaFinanceiro)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class PainelFinanceiroController : ControllerBase
    {
        private readonly IPainelFinanceiroService _painelFinanceiroService;
        private readonly ILogService _logService;

        public PainelFinanceiroController(IPainelFinanceiroService painelFinanceiroService, ILogService logService)
        {
            _painelFinanceiroService = painelFinanceiroService;
            _logService = logService;
        }

        /// <summary>
        /// GetIndicadores - Recebido (pela data do pagamento), a receber, em atraso e ticket medio (pelo vencimento) da locadora do
        /// funcionario no periodo de/ate (AAAA-MM-DD). Sem datas, vale o mes corrente; so uma data, ou invertidas, ou acima de 366 dias, e 400.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ReturnPainelFinanceiroVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetIndicadores([FromQuery] FiltroPainelFinanceiroVO filtro)
        {
            try
            {
                ReturnPainelFinanceiroVO indicadores = await _painelFinanceiroService.ObterIndicadoresAsync(filtro);

                return Ok(indicadores);
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
                _logService.Write(ex, ConstantsMessage.ErroLogPainelFinanceiro, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// GetTitulos - Lista de titulos da aba (TODOS por padrao, EM_ATRASO, VENCE_HOJE ou PAGOS_RECENTES), paginada, com os quatro
        /// contadores. O CPF sai mascarado; PAGOS_RECENTES ignora o periodo e pega os ultimos 30 dias.
        /// </summary>
        [HttpGet("titulos")]
        [ProducesResponseType(typeof(ReturnTitulosFinanceirosVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTitulos([FromQuery] FiltroTitulosFinanceirosVO filtro)
        {
            try
            {
                ReturnTitulosFinanceirosVO titulos = await _painelFinanceiroService.ObterTitulosAsync(filtro);

                return Ok(titulos);
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
                _logService.Write(ex, ConstantsMessage.ErroLogTitulosFinanceiros, GetType().ToString());
                throw;
            }
        }
    }
}
