using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Middleware
{
    /// <summary>
    /// Tratamento global de erros. Converte qualquer excecao em ProblemDetails
    /// (RFC 7807) com mensagem em portugues, e nunca devolve stack trace nem
    /// mensagem tecnica ao cliente. A escrita do corpo fica no ProblemDetailsWriter.
    ///
    /// NotFoundException -> 404 | BusinessException -> 409 |
    /// ValidationException -> 400 | UnauthorizedException -> 401 (com WWW-Authenticate: Bearer) |
    /// violacao de restricao do banco -> 409 | IntegrationException -> 502 | qualquer outra -> 500.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        // Codigos SQLSTATE do PostgreSQL para violacao de restricao.
        private const string UniqueViolation = "23505";
        private const string ForeignKeyViolation = "23503";
        private const string CheckViolation = "23514";
        private const string NotNullViolation = "23502";

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
