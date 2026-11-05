using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Autenticacao: login de clientes e funcionarios com emissao de token JWT. Rota
    /// publica (AllowAnonymous) com limite de requisicoes por IP. Sem regra de negocio:
    /// o fluxo fica no IAutenticacaoService.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAutenticacaoService _autenticacaoService;
        private readonly ILogService _logService;

        public AuthController(IAutenticacaoService autenticacaoService, ILogService logService)
        {
            _autenticacaoService = autenticacaoService;
            _logService = logService;
        }

        /// <summary>
        /// Login - Autentica cliente ou funcionario pelo perfil, CPF e senha, e devolve o token Bearer.
        /// Credenciais invalidas, conta sem acesso ou funcionario inativo devolvem 401 com a mesma mensagem.
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(typeof(ReturnLoginVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginVO loginVO)
        {
            try
            {
                ReturnLoginVO retorno = await _autenticacaoService.LoginAsync(loginVO);

                return Ok(retorno);
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
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAutenticar, GetType().ToString());
                throw;
            }
        }
    }
}
