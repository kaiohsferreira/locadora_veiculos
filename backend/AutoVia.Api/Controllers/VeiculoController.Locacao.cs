using AutoVia.Api.Filters;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    public partial class VeiculoController
    {
        /// <summary>Obtém a ficha pública de um veículo para locação, com galeria e disponibilidade opcional.</summary>
        [HttpGet("locacao/{idVeiculo:int:min(1)}")]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteLeituraPublica)]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(ReturnVeiculoLocacaoVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetParaLocacao(
            [FromRoute] int idVeiculo,
            [FromQuery, ModelBinder(BinderType = typeof(FiltroVeiculoLocacaoModelBinder))] FiltroVeiculoLocacaoVO filtro,
            CancellationToken cancellationToken)
        {
            try
            {
                ReturnVeiculoLocacaoVO ficha = await _veiculoService.GetParaLocacaoAsync(idVeiculo, filtro, cancellationToken);
                return Ok(ficha);
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
                _logService.Write(ex, ConstantsMessage.ErroConsultarVeiculoLocacao, GetType().ToString());
                throw;
            }
        }
    }
}
