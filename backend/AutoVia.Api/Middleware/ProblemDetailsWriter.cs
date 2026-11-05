using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Api.Middleware
{
    /// <summary>
    /// Escreve a resposta de erro no formato ProblemDetails (RFC 7807, application/problem+json).
    /// Usado pelo ErrorHandlingMiddleware, pelo ProblemDetailsAuthorizationResultHandler e pelo
    /// limite de requisicoes, para que 401, 403, 409, 429 e 4xx/5xx tenham o mesmo corpo.
    /// Nunca inclui stack trace. A limpeza da resposta nao apaga os cabecalhos de CORS
    /// (Access-Control-*) nem o Vary gravados pelo UseCors: eles sao guardados e regravados.
    /// </summary>
    public static class ProblemDetailsWriter
    {
        /// <summary>
        /// Limpa a resposta, define o status e grava o ProblemDetails. Os cabecalhos de CORS e Vary
        /// que ja estavam na resposta sao preservados. Quando informado, o cabecalho WWW-Authenticate
        /// e gravado depois da limpeza (ex.: Bearer para 401).
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

            List<KeyValuePair<string, StringValues>> cabecalhosPreservados = CapturarCabecalhosPreservados(context);

            context.Response.Clear();
            context.Response.StatusCode = status;

            RestaurarCabecalhos(context, cabecalhosPreservados);

            if (!string.IsNullOrEmpty(cabecalhoAutenticacao))
            {
                context.Response.Headers[HeaderNames.WWWAuthenticate] = cabecalhoAutenticacao;
            }

            await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json");
        }

        /// <summary>Guarda os cabecalhos de CORS e Vary que a resposta ja tem, antes da limpeza.</summary>
        private static List<KeyValuePair<string, StringValues>> CapturarCabecalhosPreservados(HttpContext context)
        {
            List<KeyValuePair<string, StringValues>> preservados = new List<KeyValuePair<string, StringValues>>();

            foreach (KeyValuePair<string, StringValues> cabecalho in context.Response.Headers)
            {
                if (EhCabecalhoPreservado(cabecalho.Key))
                {
                    preservados.Add(cabecalho);
                }
            }

            return preservados;
        }

        /// <summary>Regrava os cabecalhos guardados na resposta ja limpa.</summary>
        private static void RestaurarCabecalhos(HttpContext context, List<KeyValuePair<string, StringValues>> cabecalhos)
        {
            foreach (KeyValuePair<string, StringValues> cabecalho in cabecalhos)
            {
                context.Response.Headers[cabecalho.Key] = cabecalho.Value;
            }
        }

        /// <summary>Indica se o cabecalho e de CORS (prefixo Access-Control-) ou o Vary, que o UseCors grava.</summary>
        private static bool EhCabecalhoPreservado(string nome)
        {
            bool ehCors = nome.StartsWith(ConstantsValues.PrefixoCabecalhoCors, StringComparison.OrdinalIgnoreCase);
            bool ehVary = string.Equals(nome, HeaderNames.Vary, StringComparison.OrdinalIgnoreCase);

            return ehCors || ehVary;
        }
    }
}
