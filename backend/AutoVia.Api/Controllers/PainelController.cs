using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Painel;
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
    /// Painel da locadora (P11-BE-01 e P11-BE-02): visao geral com indicadores e situacao da frota. Exige a politica
    /// Funcionario (qualquer cargo de funcionario); cliente recebe 403. A locadora vem do contexto do token e o corte dos
    /// valores em reais e feito no servico. Sem regra de negocio aqui: o controller so valida (FluentValidation, antes da
    /// action), chama o servico e devolve o VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/painel")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public class PainelController : ControllerBase
    {
        private readonly IPainelService _painelService;
        private readonly ILogService _logService;

        public PainelController(IPainelService painelService, ILogService logService)
        {
            _painelService = painelService;
            _logService = logService;
        }

        /// <summary>
        /// GetVisaoGeral - Indicadores da locadora do funcionario autenticado no periodo informado (hoje, 7 dias, 30 dias ou
        /// mes atual; padrao 30 dias). Valores em reais so para ADMINISTRADOR e FINANCEIRO; os demais cargos recebem as
        /// quantidades, a frota e a ocupacao, com os campos em reais nulos.
        /// </summary>
        [HttpGet("visao-geral")]
        [ProducesResponseType(typeof(ReturnVisaoGeralVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetVisaoGeral([FromQuery] FiltroVisaoGeralVO filtro)
        {
            try
            {
                ReturnVisaoGeralVO visaoGeral = await _painelService.ObterVisaoGeralAsync(filtro.Periodo);

                return Ok(visaoGeral);
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
                _logService.Write(ex, ConstantsMessage.ErroConsultarVisaoGeral, GetType().ToString());
                throw;
            }
        }
    }
}
