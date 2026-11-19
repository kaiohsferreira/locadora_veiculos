using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Manutencao;
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
    /// Endpoints da manutencao (P8-BE-01, P8-BE-02 e P8-BE-03): abertura e encerramento da ordem e alertas de revisao.
    /// Restrito a GESTOR_FROTA e ADMINISTRADOR pela politica GestorFrota, aplicada na classe. Sem regra de negocio: a
    /// entrada e validada pelo FluentValidation (antes da action), a logica fica no IManutencaoService e a resposta sai
    /// so em VO. Erros viram ProblemDetails no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/manutencoes")]
    [Authorize(Policy = ConstantsValues.PoliticaGestorFrota)]
    public class ManutencaoController : ControllerBase
    {
        private readonly IManutencaoService _manutencaoService;
        private readonly ILogService _logService;

        public ManutencaoController(IManutencaoService manutencaoService, ILogService logService)
        {
            _manutencaoService = manutencaoService;
            _logService = logService;
        }

        /// <summary>
        /// Abrir - Abre uma ordem de manutencao (PREVENTIVA ou CORRETIVA) para o veiculo. O veiculo passa a MANUTENCAO e some
        /// da busca de disponiveis. Devolve 201 com a ordem criada.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnManutencaoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Abrir([FromBody] AbrirManutencaoVO abrirManutencaoVO)
        {
            try
            {
                ReturnManutencaoVO criada = await _manutencaoService.AbrirAsync(abrirManutencaoVO);

                return StatusCode(StatusCodes.Status201Created, criada);
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
                _logService.Write(ex, ConstantsMessage.ErroAbrirManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Encerrar - Encerra a ordem de manutencao informada, com a data de saida, a quilometragem do veiculo e o custo. Se o
        /// veiculo estiver em MANUTENCAO, ele volta a DISPONIVEL. Devolve 200 com a ordem encerrada.
        /// </summary>
        [HttpPatch("{idManutencao:int}/encerrar")]
        [ProducesResponseType(typeof(ReturnManutencaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Encerrar(int idManutencao, [FromBody] EncerrarManutencaoVO encerrarManutencaoVO)
        {
            try
            {
                ReturnManutencaoVO encerrada = await _manutencaoService.EncerrarAsync(idManutencao, encerrarManutencaoVO);

                return Ok(encerrada);
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
                _logService.Write(ex, ConstantsMessage.ErroEncerrarManutencao, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Alertas - Lista os veiculos em alerta de revisao (VENCIDA ou PROXIMA), do mais atrasado para o menos atrasado, com
        /// o total. A proxima revisao e a ultima preventiva mais 10.000 km.
        /// </summary>
        [HttpGet("alertas")]
        [ProducesResponseType(typeof(ReturnAlertasRevisaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Alertas()
        {
            try
            {
                ReturnAlertasRevisaoVO alertas = await _manutencaoService.ListarAlertasRevisaoAsync();

                return Ok(alertas);
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
                _logService.Write(ex, ConstantsMessage.ErroListarAlertasRevisao, GetType().ToString());
                throw;
            }
        }
    }
}
