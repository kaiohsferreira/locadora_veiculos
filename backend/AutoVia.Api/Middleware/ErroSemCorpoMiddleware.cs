using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AutoVia.Api.Middleware
{
    /// <summary>
    /// Da formato de ProblemDetails aos 404 (rota inexistente, com usuario autenticado) e aos 405 (metodo que a rota nao aceita),
    /// que o framework responde sem corpo. So age quando a resposta ainda nao comecou: qualquer resposta que ja tem corpo
    /// (401, 403, 409, 429, 502 e outras) passa intacta. Fica logo depois do ErrorHandlingMiddleware.
    /// </summary>
    public class ErroSemCorpoMiddleware
    {
        private readonly RequestDelegate _next;

        public ErroSemCorpoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>Executa o restante do pipeline e, se a resposta ainda estiver vazia com 404 ou 405, grava o ProblemDetails.</summary>
        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            if (context.Response.HasStarted)
            {
                return;
            }

            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status404NotFound, ConstantsMessage.TituloRecursoNaoEncontrado, ConstantsMessage.ErroRecursoNaoEncontrado);
                return;
            }

            if (context.Response.StatusCode == StatusCodes.Status405MethodNotAllowed)
            {
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status405MethodNotAllowed, ConstantsMessage.TituloMetodoNaoPermitido, ConstantsMessage.ErroMetodoNaoPermitido);
            }
        }
    }
}
