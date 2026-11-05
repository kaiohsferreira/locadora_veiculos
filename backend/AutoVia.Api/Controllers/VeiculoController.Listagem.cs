using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Listagem de veiculos (P1-BE-03). Arquivo parcial do VeiculoController: sem regra de
    /// negocio. A entrada e validada pelo FluentValidationActionFilter antes da action, e a
    /// resposta sai somente em PaginaVO. A politica Funcionario ja vem da classe.
    /// </summary>
    public partial class VeiculoController
    {
        /// <summary>
        /// GetAll - Lista os veiculos ativos, paginados e ordenados por placa, com filtros opcionais por categoria, status e placa.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PaginaVO<ReturnVeiculoVO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] FiltroVeiculoVO filtro)
        {
            try
            {
                PaginaVO<ReturnVeiculoVO> pagina = await _veiculoService.GetAllAsync(filtro);

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
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarVeiculos, GetType().ToString());
                throw;
            }
        }
    }
}
