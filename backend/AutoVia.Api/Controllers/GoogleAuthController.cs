using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Continuar com o Google (N3, RF-51): configuracao publica, login e vinculo de cliente com conta Google.
    /// As tres rotas sao publicas (AllowAnonymous explicito). Login e vinculo tem o mesmo limite por IP do
    /// login por CPF; a configuracao nao tem limite. Sem regra de negocio: o fluxo fica no IAutenticacaoGoogleService.
    /// Devolve so VO de retorno, nunca entidade. Erros de negocio e de integracao sao repassados ao middleware.
    /// </summary>
    [ApiController]
    [Route(ConstantsValues.RotaGoogle)]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IAutenticacaoGoogleService _autenticacaoGoogleService;
        private readonly ILogService _logService;

        public GoogleAuthController(IAutenticacaoGoogleService autenticacaoGoogleService, ILogService logService)
        {
            _autenticacaoGoogleService = autenticacaoGoogleService;
            _logService = logService;
        }

        /// <summary>
        /// Configuracao do login com Google - Informa se o botao aparece, o Client ID (so quando habilitado) e se o
        /// provedor e o de teste. Falha de integracao devolve habilitado = false, sem erro para a tela.
        /// </summary>
        [HttpGet(ConstantsValues.RotaGoogleConfiguracao)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ReturnGoogleConfiguracaoVO), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObterConfiguracao()
        {
            try
            {
                ReturnGoogleConfiguracaoVO retorno = await _autenticacaoGoogleService.ObterConfiguracaoAsync(HttpContext.RequestAborted);

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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                // Cliente desistiu: sem log de erro e sem embrulhar; segue o tratamento padrao do ASP.NET.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroConfiguracaoGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Login com Google - Valida a credencial do Google e devolve a situacao: AUTENTICADO (com sessao), CONFIRMACAO_NECESSARIA
        /// (pede CPF e senha para vincular), CADASTRO_NECESSARIO (sem cadastro com o e-mail) ou CONTA_AMBIGUA (mais de um cadastro).
        /// Nada e gravado nas situacoes sem sessao. Prova invalida devolve 401; Google indisponivel devolve 502.
        /// </summary>
        [HttpPost(ConstantsValues.RotaGoogleLogin)]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(typeof(ReturnGoogleLoginVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Login([FromBody] GoogleLoginVO vo)
        {
            try
            {
                ReturnGoogleLoginVO retorno = await _autenticacaoGoogleService.LoginAsync(vo, HttpContext.RequestAborted);

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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                // Cliente desistiu: sem log de erro e sem embrulhar; segue o tratamento padrao do ASP.NET.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLoginGoogle, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Vinculo com Google - Depois de provar CPF e senha da conta AutoVia, grava a conta Google no cliente e abre a sessao.
        /// Senha, CPF, e-mail ou prova errados devolvem 401 com a mesma mensagem. Conta Google ou cliente ja vinculados, ou
        /// e-mail com mais de um cadastro, devolvem 409. Mesmo vinculo ja gravado segue e abre a sessao.
        /// </summary>
        [HttpPost(ConstantsValues.RotaGoogleVinculo)]
        [AllowAnonymous]
        [EnableRateLimiting(ConstantsValues.PoliticaLimitePublico)]
        [ProducesResponseType(typeof(ReturnLoginVO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Vinculo([FromBody] GoogleVinculoVO vo)
        {
            try
            {
                ReturnLoginVO retorno = await _autenticacaoGoogleService.VincularAsync(vo, HttpContext.RequestAborted);

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
            catch (IntegrationException)
            {
                throw;
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                // Cliente desistiu: sem log de erro e sem embrulhar; segue o tratamento padrao do ASP.NET.
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroVincularGoogle, GetType().ToString());
                throw;
            }
        }
    }
}
