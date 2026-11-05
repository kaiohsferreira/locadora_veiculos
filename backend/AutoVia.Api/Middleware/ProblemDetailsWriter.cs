using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Api.Middleware
{
    /// <summary>
    /// Escreve a resposta de erro no formato ProblemDetails (RFC 7807, application/problem+json).
    /// Usado pelo ErrorHandlingMiddleware e pelo ProblemDetailsAuthorizationResultHandler,
    /// para que 401, 403, 429 e 4xx/5xx tenham o mesmo corpo. Nunca inclui stack trace.
    /// </summary>
    public static class ProblemDetailsWriter
    {
        /// <summary>
        /// Limpa a resposta, define o status e grava o ProblemDetails. Quando informado, o
        /// cabecalho WWW-Authenticate e gravado depois da limpeza (ex.: Bearer para 401).
        /// </summary>
        public static async Task WriteAsync(HttpContext context, int status, string title, string detail, string? cabecalhoAutenticacao = null)
        {
            ProblemDetails problem = new ProblemDetails
            {
                Type = $"https://httpstatuses.io/{status}",
                Title = title,
                Status = status,
                Detail = detail,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.Clear();
            context.Response.StatusCode = status;

            if (!string.IsNullOrEmpty(cabecalhoAutenticacao))
            {
                context.Response.Headers[HeaderNames.WWWAuthenticate] = cabecalhoAutenticacao;
            }

            await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
