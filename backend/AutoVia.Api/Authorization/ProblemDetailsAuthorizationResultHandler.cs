using AutoVia.Api.Middleware;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Trata o resultado da autorizacao de cada rota: sem token valido (Challenged) vira
    /// 401 e token valido sem o perfil exigido (Forbidden) vira 403, ambos em
    /// application/problem+json. Quando a politica passa, a requisicao segue normalmente.
    /// </summary>
    public class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly ILogService _logService;

        public ProblemDetailsAuthorizationResultHandler(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>Responde 401 ou 403 em ProblemDetails; sem falha, deixa a requisicao seguir para a action.</summary>
        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Challenged)
            {
                await EscreverProblemaAsync(context, StatusCodes.Status401Unauthorized, ConstantsMessage.TituloNaoAutenticado, ConstantsMessage.ErroNaoAutenticado, ConstantsValues.TipoTokenBearer);
                return;
            }

            if (authorizeResult.Forbidden)
            {
                await EscreverProblemaAsync(context, StatusCodes.Status403Forbidden, ConstantsMessage.TituloAcessoNegado, ConstantsMessage.ErroAcessoNegado, null);
                return;
            }

            await next(context);
        }

        /// <summary>Grava o ProblemDetailsWriter; falha na escrita e registrada no log antes de subir.</summary>
        private async Task EscreverProblemaAsync(HttpContext context, int status, string title, string detail, string? cabecalhoAutenticacao)
        {
            try
            {
                await ProblemDetailsWriter.WriteAsync(context, status, title, detail, cabecalhoAutenticacao);
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroInterno, GetType().ToString());
                throw;
            }
        }
    }
}
