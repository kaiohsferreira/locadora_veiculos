using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
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
    /// Locacoes do proprio cliente (tela 27, RF-46). Restrito ao perfil CLIENTE pela politica Cliente, na classe. O id do
    /// cliente vem so da claim do token (nunca do corpo ou da rota). Sem regra de negocio aqui.
    /// </summary>
    [ApiController]
    [Route("api/locacoes/minhas")]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    public class MinhasLocacoesController : ControllerBase
    {
        private readonly IMinhasLocacoesService _minhasLocacoesService;
        private readonly ILogService _logService;

        public MinhasLocacoesController(IMinhasLocacoesService minhasLocacoesService, ILogService logService)
        {
            _minhasLocacoesService = minhasLocacoesService;
            _logService = logService;
        }

        /// <summary>
        /// Lista as locacoes do proprio cliente autenticado, em andamento ou no historico, paginadas. Sem situacao, vale em
        /// andamento. Devolve 200 com a pagina (lista vazia quando nao ha nada).
        /// </summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(PaginaVO<ReturnMinhaLocacaoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ListarMinhas([FromQuery] FiltroMinhaLocacaoVO filtro)
        {
            try
            {
                int idCliente = LerIdCliente();

                PaginaVO<ReturnMinhaLocacaoVO> pagina = await _minhasLocacoesService.ListarAsync(idCliente, filtro);

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
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarMinhasLocacoes, GetType().ToString());
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
