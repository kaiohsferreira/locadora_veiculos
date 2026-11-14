using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    [ApiController]
    [Route("api/reservas")]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    [ProducesResponseType(typeof(ReturnReservaCriadaVO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ReservaController : ControllerBase
    {
        private readonly IReservaService _reservaService;
        private readonly ILogService _logService;

        public ReservaController(IReservaService reservaService, ILogService logService)
        {
            _reservaService = reservaService;
            _logService = logService;
        }

        /// <summary>Cria uma reserva para o cliente autenticado.</summary>
        [HttpPost]
        public async Task<IActionResult> CriarAsync([FromBody] CriarReservaVO entrada)
        {
            try
            {
                Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
                int idCliente = 0;
                bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);
                if (!identificadorValido)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroSessaoInvalida);
                }

                ReturnReservaCriadaVO retorno = await _reservaService.CriarAsync(idCliente, entrada);
                string location = $"/api/reservas/{retorno.IdReserva}";
                Response.Headers.Location = location;
                return StatusCode(StatusCodes.Status201Created, retorno);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }
    }
}
