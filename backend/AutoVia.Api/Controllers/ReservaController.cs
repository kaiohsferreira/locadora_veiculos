using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Communication.CustomExceptions;
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
    [ApiController]
    [Route("api/reservas")]
    [Authorize(Policy = ConstantsValues.PoliticaCliente)]
    [ProducesResponseType(typeof(ReturnReservaCriadaVO), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReturnReservaVO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginaVO<ReturnReservaVO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> CriarAsync([FromBody] CriarReservaVO entrada)
        {
            try
            {
                Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
                int idCliente = 0;
                bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);
                if (!identificadorValido)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                ReturnReservaCriadaVO retorno = await _reservaService.CriarAsync(idCliente, entrada);
                return CreatedAtAction(nameof(GetByIdAsync), new { idReserva = retorno.IdReserva }, retorno);
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Retorna uma reserva pertencente ao cliente autenticado.</summary>
        [HttpGet("{idReserva:int}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ActionName(nameof(GetByIdAsync))]
        public async Task<IActionResult> GetByIdAsync(int idReserva)
        {
            try
            {
                Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
                int idCliente = 0;
                bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);
                if (!identificadorValido)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                ReturnReservaVO retorno = await _reservaService.GetByIdAsync(idReserva, idCliente);
                return Ok(retorno);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConsultarReserva, GetType().ToString());
                throw;
            }
        }

        /// <summary>Lista as reservas do cliente autenticado com paginação e filtros opcionais de situação, status e busca.</summary>
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetAllAsync([FromQuery] FiltroReservaVO filtro)
        {
            try
            {
                Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
                int idCliente = 0;
                bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);
                if (!identificadorValido)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                PaginaVO<ReturnReservaVO> retorno = await _reservaService.GetAllAsync(idCliente, filtro);
                return Ok(retorno);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroListarReservas, GetType().ToString());
                throw;
            }
        }

        /// <summary>Cancela logicamente uma reserva do cliente autenticado.</summary>
        [HttpDelete("{idReserva:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> CancelarAsync(int idReserva)
        {
            try
            {
                Claim? identificador = User.FindFirst(ClaimTypes.NameIdentifier);
                int idCliente = 0;
                bool identificadorValido = identificador != null && int.TryParse(identificador.Value, out idCliente);
                if (!identificadorValido)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                await _reservaService.CancelarAsync(idReserva, idCliente);
                return NoContent();
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCancelarReserva, GetType().ToString());
                throw;
            }
        }
    }
}
