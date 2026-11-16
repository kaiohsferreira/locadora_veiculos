using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Api.Middleware
{
    /// <summary>
    /// Tratamento global de erros. Converte qualquer excecao em ProblemDetails
    /// (RFC 7807) com mensagem em portugues, e nunca devolve stack trace nem
    /// mensagem tecnica ao cliente. A escrita do corpo fica no ProblemDetailsWriter.
    ///
    /// NotFoundException -> 404 | BusinessException -> 409 | LimiteExcedidoException -> 429 (titulo de limite de
    /// requisicoes, detail = mensagem da excecao, sem log: e teto de negocio, nao erro) |
    /// ValidationException -> 400 | UnauthorizedException -> 401 (com WWW-Authenticate: Bearer) |
    /// BadHttpRequestException (leitura do corpo recusada pelo framework, sem log: e erro do cliente) -> 400, com
    /// errors.corpo quando o corpo passou do teto ([RequestSizeLimit], 413) |
    /// violacao de restricao do banco -> 409 | IntegrationException -> 502 | qualquer outra -> 500.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        // Codigos SQLSTATE do PostgreSQL para violacao de restricao.
        private const string UniqueViolation = "23505";
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string NotNullViolation = "23502";

        // Corpo acima do teto: a chave do erro segue o formato de model binding do projeto (AddCustomResponseStartup).
        private const string CampoCorpo = "corpo";
        private const string TipoProblema = "application/problem+json";
        private const string PrefixoUriProblema = "https://httpstatuses.io/";

        private readonly RequestDelegate _next;
        private readonly ILogService _logService;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogService logService)
        {
            _next = next;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted)
                {
                    _logService.Write(exception, ConstantsMessage.ErroInterno, GetType().ToString());
                    throw;
                }

                await WriteProblemAsync(context, exception);
            }
        }

        private async Task WriteProblemAsync(HttpContext context, Exception exception)
        {
            if (exception is BadHttpRequestException requisicaoInvalida)
            {
                await EscreverRequisicaoInvalidaAsync(context, requisicaoInvalida);
                return;
            }

            int status;
            string title;
            string detail;
            string? cabecalhoAutenticacao = null;

            switch (exception)
            {
                case NotFoundException notFound:
                    status = StatusCodes.Status404NotFound;
                    title = ConstantsMessage.TituloRecursoNaoEncontrado;
                    detail = notFound.Message;
                    break;

                case BusinessException business:
                    status = StatusCodes.Status409Conflict;
                    title = ConstantsMessage.TituloConflito;
                    detail = business.Message;
                    break;

                case LimiteExcedidoException limite:
                    // Teto de negocio: a mensagem ja e pronta para o usuario e o caso e esperado, por isso nao e registrada como erro.
                    status = StatusCodes.Status429TooManyRequests;
                    title = ConstantsMessage.TituloLimiteRequisicoes;
                    detail = limite.Message;
                    break;

                case UnauthorizedException unauthorized:
                    // Login recusado: a mensagem ja e generica, por isso nao e registrada como erro.
                    status = StatusCodes.Status401Unauthorized;
                    title = ConstantsMessage.TituloNaoAutenticado;
                    detail = unauthorized.Message;
                    cabecalhoAutenticacao = ConstantsValues.TipoTokenBearer;
                    break;

                case FluentValidation.ValidationException validation:
                    status = StatusCodes.Status400BadRequest;
                    title = ConstantsMessage.TituloErroValidacao;
                    detail = validation.Errors.FirstOrDefault()?.ErrorMessage ?? ConstantsMessage.ErroRequisicaoInvalida;
                    break;

                case DbUpdateException dbUpdate when IsConstraintViolation(dbUpdate):
                    // Rede de seguranca: o servico deve validar antes (Prompt 3),
                    // mas se a restricao do banco disparar, o usuario recebe 409 e
                    // uma mensagem clara, nao o erro do PostgreSQL.
                    status = StatusCodes.Status409Conflict;
                    title = ConstantsMessage.TituloConflito;
                    detail = ConstantsMessage.ErroRestricaoBanco;
                    _logService.Write(dbUpdate, detail, GetType().ToString());
                    break;

                case IntegrationException integration:
                    status = StatusCodes.Status502BadGateway;
                    title = ConstantsMessage.TituloErroIntegracao;
                    detail = integration.Message;
                    _logService.Write(integration, detail, GetType().ToString());
                    break;

                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = ConstantsMessage.TituloErroInterno;
                    detail = ConstantsMessage.ErroInterno;
                    _logService.Write(exception, detail, GetType().ToString());
                    break;
            }

            await ProblemDetailsWriter.WriteAsync(context, status, title, detail, cabecalhoAutenticacao);
        }

        /// <summary>
        /// Leitura do corpo recusada pelo framework: 400 no formato do projeto, SEM log (erro do cliente). So o corpo acima
        /// do teto ([RequestSizeLimit], status 413) leva errors.corpo com ErroCampoInvalido; outra recusa sai so com a
        /// mensagem generica de requisicao invalida. O texto do framework nunca chega ao cliente.
        /// </summary>
        private static async Task EscreverRequisicaoInvalidaAsync(HttpContext context, BadHttpRequestException exception)
        {
            if (exception.StatusCode != StatusCodes.Status413PayloadTooLarge)
            {
                await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status400BadRequest, ConstantsMessage.TituloErroValidacao, ConstantsMessage.ErroRequisicaoInvalida);
                return;
            }

            Dictionary<string, string[]> erros = new Dictionary<string, string[]>
            {
                { CampoCorpo, new string[] { string.Format(ConstantsMessage.ErroCampoInvalido, CampoCorpo) } }
            };

            ValidationProblemDetails problema = new ValidationProblemDetails(erros)
            {
                Type = PrefixoUriProblema + StatusCodes.Status400BadRequest.ToString(),
                Title = ConstantsMessage.TituloErroValidacao,
                Status = StatusCodes.Status400BadRequest,
                Detail = ConstantsMessage.ErroRequisicaoInvalida,
                Instance = context.Request.Path
            };

            problema.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            List<KeyValuePair<string, StringValues>> cabecalhos = CapturarCabecalhosDeCors(context);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            foreach (KeyValuePair<string, StringValues> cabecalho in cabecalhos)
            {
                context.Response.Headers[cabecalho.Key] = cabecalho.Value;
            }

            await context.Response.WriteAsJsonAsync(problema, (JsonSerializerOptions?)null, TipoProblema);
        }

        /// <summary>Guarda os cabecalhos de CORS e Vary que a resposta ja tem, para regrava-los depois da limpeza (mesma regra do ProblemDetailsWriter).</summary>
        private static List<KeyValuePair<string, StringValues>> CapturarCabecalhosDeCors(HttpContext context)
        {
            List<KeyValuePair<string, StringValues>> preservados = new List<KeyValuePair<string, StringValues>>();

            foreach (KeyValuePair<string, StringValues> cabecalho in context.Response.Headers)
            {
                bool ehCors = cabecalho.Key.StartsWith(ConstantsValues.PrefixoCabecalhoCors, StringComparison.OrdinalIgnoreCase);
                bool ehVary = string.Equals(cabecalho.Key, HeaderNames.Vary, StringComparison.OrdinalIgnoreCase);

                if (ehCors || ehVary)
                {
                    preservados.Add(cabecalho);
                }
            }

            return preservados;
        }

        private static bool IsConstraintViolation(DbUpdateException exception)
        {
            PostgresException? postgres = exception.InnerException as PostgresException;

            if (postgres == null)
                return false;

            return postgres.SqlState == UniqueViolation
                || postgres.SqlState == ForeignKeyViolation
                || postgres.SqlState == CheckViolation
                || postgres.SqlState == NotNullViolation;
        }
    }
}
