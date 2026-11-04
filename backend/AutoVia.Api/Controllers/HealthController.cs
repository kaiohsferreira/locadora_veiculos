using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Verificacao de saude da API. Unico controller da fundacao: os controllers
    /// de negocio chegam com o Prompt 3 de cada modulo.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly ILogService _logService;

        public HealthController(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Health - Informa se a API esta no ar. Nao consulta o banco, para que
        /// uma oscilacao do PostgreSQL nao derrube o healthcheck.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(HealthReturnVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult Get()
        {
            try
            {
                HealthReturnVO health = new HealthReturnVO
                {
                    Status = ConstantsMessage.StatusSaudeOk,
                    Mensagem = ConstantsMessage.SucessoAplicacaoNoAr,
                    DataHoraStr = StaticMethods.FormateDateTimeToPTBR(DateTime.Now)
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroInterno, GetType().ToString());

                ProblemDetails problem = new ProblemDetails
                {
                    Title = ConstantsMessage.TituloErroInterno,
                    Detail = ConstantsMessage.ErroInterno,
                    Status = StatusCodes.Status500InternalServerError
                };

                return StatusCode(StatusCodes.Status500InternalServerError, problem);
            }
        }
    }
}
