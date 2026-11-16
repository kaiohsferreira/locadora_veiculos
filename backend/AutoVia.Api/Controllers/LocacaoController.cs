using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>Endpoints de locacao restritos a funcionarios autenticados.</summary>
    [ApiController]
    [Route("api/locacoes")]
    [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
    public class LocacaoController : ControllerBase
    {
        private readonly ILocacaoService _locacaoService;
        private readonly ILogService _logService;

        public LocacaoController(ILocacaoService locacaoService, ILogService logService)
        {
            _locacaoService = locacaoService;
            _logService = logService;
        }

        /// <summary>Consulta uma locacao ativa pelo identificador.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReturnLocacaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                ReturnLocacaoVO locacao = await _locacaoService.GetByIdAsync(id);

                return Ok(locacao);
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
            catch (UnauthorizedException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroNaoAutenticado, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroBuscarLocacao, GetType().ToString());
                throw;
            }
        }

        /// <summary>Inicia uma locacao de uma reserva ativa ou confirmada; devolve 201 e Location.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ReturnLocacaoVO), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> StartFromReserva([FromBody] LocacaoVO locacaoVO)
        {
            try
            {
                Claim? claimFuncionario = User.FindFirst(ClaimTypes.NameIdentifier);

                if (claimFuncionario == null ||
                    !int.TryParse(claimFuncionario.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idFuncionario) ||
                    idFuncionario <= 0)
                {
                    throw new UnauthorizedException(ConstantsMessage.ErroNaoAutenticado);
                }

                ReturnLocacaoVO criada = await _locacaoService.StartFromReservaAsync(locacaoVO, idFuncionario);

                return CreatedAtAction(nameof(GetById), new { id = criada.IdLocacao }, criada);
            }
            catch (BusinessException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (NotFoundException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
            catch (UnauthorizedException ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroNaoAutenticado, GetType().ToString());
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSalvarLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
