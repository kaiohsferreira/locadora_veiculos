using AutoVia.Api.Middleware;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Garante o contrato de erro da API: toda excecao vira ProblemDetails
    /// (RFC 7807) com o status certo e mensagem em portugues, sem vazar detalhe tecnico.
    /// </summary>
    public class ErrorHandlingMiddlewareTests
    {
        private static async Task<(int Status, string ContentType, JsonElement Body)> ExecutarAsync(Exception exception)
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/teste";
            context.Response.Body = new MemoryStream();

            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw exception, new FakeLogService());
            await middleware.InvokeAsync(context);

            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);

            return (context.Response.StatusCode, context.Response.ContentType ?? string.Empty, document.RootElement);
        }

        [Fact]
        public async Task NotFoundException_Vira404()
        {
            (int status, string contentType, JsonElement body) = await ExecutarAsync(new NotFoundException("Veículo não encontrado."));

            Assert.Equal(404, status);
            Assert.StartsWith("application/problem+json", contentType);
            Assert.Equal("Veículo não encontrado.", body.GetProperty("detail").GetString());
            Assert.Equal(ConstantsMessage.TituloRecursoNaoEncontrado, body.GetProperty("title").GetString());
        }

        [Fact]
        public async Task BusinessException_Vira409()
        {
            (int status, string contentType, JsonElement body) = await ExecutarAsync(new BusinessException("Veículo já reservado no período."));

            Assert.Equal(409, status);
            Assert.Equal("Veículo já reservado no período.", body.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task IntegrationException_Vira502()
        {
            IntegrationException exception = new IntegrationException(ConstantsIntegration.Pagamento, "A integração PAGAMENTO está inativa.");

            (int status, string contentType, JsonElement body) = await ExecutarAsync(exception);

            Assert.Equal(502, status);
            Assert.Contains(ConstantsIntegration.Pagamento, body.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task UnauthorizedException_Vira401ComWwwAuthenticateBearer()
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/auth/login";
            context.Response.Body = new MemoryStream();
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas), new FakeLogService());

            await middleware.InvokeAsync(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.Equal(ConstantsValues.TipoTokenBearer, context.Response.Headers[HeaderNames.WWWAuthenticate].ToString());
            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, document.RootElement.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, document.RootElement.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task ExcecaoInesperada_Vira500_SemVazarMensagemTecnica()
        {
            (int status, string contentType, JsonElement body) = await ExecutarAsync(new InvalidOperationException("Npgsql: connection refused em 10.0.0.5"));

            Assert.Equal(500, status);
            Assert.Equal(ConstantsMessage.ErroInterno, body.GetProperty("detail").GetString());
            Assert.DoesNotContain("Npgsql", body.GetRawText());
            Assert.True(body.TryGetProperty("traceId", out JsonElement _));
        }
    }
}
