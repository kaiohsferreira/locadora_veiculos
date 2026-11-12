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
    /// Recuperacao de senha por link (N1, RF-49): pedido e redefinicao. As duas rotas sao publicas
    /// (AllowAnonymous explicito) e tem o mesmo limite por IP do login. Sem regra de negocio: o fluxo
    /// fica no IRecuperacaoSenhaService. Devolve so VO de retorno, nunca entidade.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public class RecuperacaoSenhaController : ControllerBase
    {
        private readonly IRecuperacaoSenhaService _recuperacaoSenhaService;
        private readonly ILogService _logService;

        public RecuperacaoSenhaController(IRecuperacaoSenhaService recuperacaoSenhaService, ILogService logService)
        {
            _recuperacaoSenhaService = recuperacaoSenhaService;
            _logService = logService;
        }

        /// <summary>
        /// Esqueci minha senha - Recebe CPF e perfil. Se a conta for elegivel, envia o link de redefinicao ao e-mail cadastrado.
        /// Responde 202 com a mesma mensagem em qualquer caso (conta inexistente, desativada, sem e-mail, limite atingido ou fila cheia).
        /// </summary>
        [HttpPost("recuperacao-senha")]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(typeof(ReturnRecuperacaoSolicitadaVO), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SolicitarRecuperacaoSenha([FromBody] SolicitarRecuperacaoSenhaVO vo)
        {
            try
            {
                ReturnRecuperacaoSolicitadaVO retorno = await _recuperacaoSenhaService.SolicitarAsync(vo);

                return Accepted(retorno);
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroSolicitarRecuperacaoSenha, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Redefinir senha - Troca a senha com o link de uso unico recebido por e-mail. Sem login automatico.
        /// Link expirado, usado, adulterado ou inventado, ou conta inativa, devolvem 409 com a mesma mensagem.
        /// </summary>
        [HttpPost("redefinicao-senha")]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaVO vo)
        {
            try
            {
                await _recuperacaoSenhaService.RedefinirAsync(vo);

                return NoContent();
            }
            catch (BusinessException)
            {
                throw;
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (UnauthorizedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroRedefinirSenha, GetType().ToString());
                throw;
            }
        }
    }
}
