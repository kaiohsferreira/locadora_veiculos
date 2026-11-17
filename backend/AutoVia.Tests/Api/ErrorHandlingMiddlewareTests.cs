using AutoVia.Api.Middleware;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
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
        private const int IdVeiculoDaFila = 7;

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

        /// <summary>Teto de negocio (LimiteExcedidoException): 429 em problem+json com o titulo do limite e o detail da excecao, sem log de erro.</summary>
        [Fact]
        public async Task LimiteExcedidoException_Vira429ComTituloDeLimiteESemLog()
        {
            FakeLogService log = new FakeLogService();
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/clientes/minhas-locadoras";
            context.Response.Body = new MemoryStream();
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw new LimiteExcedidoException(ConstantsMessage.ErroLimitePedidosAprovacao), log);

            await middleware.InvokeAsync(context);

            Assert.Equal(429, context.Response.StatusCode);
            Assert.StartsWith("application/problem+json", context.Response.ContentType ?? string.Empty);
            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, document.RootElement.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroLimitePedidosAprovacao, document.RootElement.GetProperty("detail").GetString());
            Assert.Empty(log.Snapshot());
        }

        /// <summary>
        /// Fila do processador de foto cheia, pelo servico real de fotos: o envio relanca a LimiteExcedidoException sem enviar nem gravar
        /// e sem log, e o middleware responde 429 com o titulo de limite e a mensagem da fila.
        /// </summary>
        [Fact]
        public async Task LimiteExcedidoException_DaFilaDoProcessadorDeFoto_Vira429ComTituloDeLimiteESemLog()
        {
            FakeLogService log = new FakeLogService();
            FakeVeiculoFotoRepository repositorio = new FakeVeiculoFotoRepository();
            repositorio.VeiculosAtivos.Add(IdVeiculoDaFila);
            FakeProcessadorFotoService processador = new FakeProcessadorFotoService { Falha = new LimiteExcedidoException(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado) };
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            VeiculoFotoService servico = new VeiculoFotoService(repositorio, processador, armazenamento, ContextoEmpresaDeTeste.Criar(), log);
            byte[] jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

            LimiteExcedidoException excecao = await Assert.ThrowsAsync<LimiteExcedidoException>(() => servico.EnviarAsync(IdVeiculoDaFila, jpeg, ConstantsArmazenamento.MimeJpeg));

            Assert.Equal(0, armazenamento.UploadCount);
            Assert.Equal(0, repositorio.InsertCount);
            Assert.Empty(log.Snapshot());

            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/veiculos/7/fotos";
            context.Response.Body = new MemoryStream();
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw excecao, log);

            await middleware.InvokeAsync(context);

            Assert.Equal(429, context.Response.StatusCode);
            Assert.StartsWith("application/problem+json", context.Response.ContentType ?? string.Empty);
            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, document.RootElement.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado, document.RootElement.GetProperty("detail").GetString());
            Assert.Empty(log.Snapshot());
        }

        /// <summary>Corpo acima do teto (413 do framework): 400 no formato do projeto com errors.corpo, sem log e sem texto do framework.</summary>
        [Fact]
        public async Task BadHttpRequestException_CorpoAcimaDoTeto_Vira400ComErrorsCorpoESemLog()
        {
            FakeLogService log = new FakeLogService();
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/clientes/verificar-cadastro";
            context.Response.Body = new MemoryStream();
            BadHttpRequestException excecao = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw excecao, log);

            await middleware.InvokeAsync(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.StartsWith("application/problem+json", context.Response.ContentType ?? string.Empty);
            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            JsonElement raiz = document.RootElement;
            Assert.Equal(ConstantsMessage.TituloErroValidacao, raiz.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, raiz.GetProperty("detail").GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "corpo"), raiz.GetProperty("errors").GetProperty("corpo")[0].GetString());
            Assert.DoesNotContain("too large", raiz.GetRawText());
            Assert.Empty(log.Snapshot());
        }

        /// <summary>Outra recusa de leitura do corpo: 400 com a mensagem generica do projeto, sem errors, sem texto do framework e sem log.</summary>
        [Fact]
        public async Task BadHttpRequestException_OutraRecusaDeLeitura_Vira400GenericoSemErrorsESemLog()
        {
            FakeLogService log = new FakeLogService();
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/clientes/verificar-cadastro";
            context.Response.Body = new MemoryStream();
            BadHttpRequestException excecao = new BadHttpRequestException("Unexpected end of request content.", StatusCodes.Status400BadRequest);
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(_ => throw excecao, log);

            await middleware.InvokeAsync(context);

            Assert.Equal(400, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
            JsonElement raiz = document.RootElement;
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, raiz.GetProperty("detail").GetString());
            Assert.False(raiz.TryGetProperty("errors", out JsonElement _));
            Assert.DoesNotContain("Unexpected", raiz.GetRawText());
            Assert.Empty(log.Snapshot());
        }

        /// <summary>O 400 de corpo grande mantem os cabecalhos de CORS que a resposta ja tinha (regravados depois da limpeza).</summary>
        [Fact]
        public async Task BadHttpRequestException_CorpoAcimaDoTeto_PreservaCabecalhosDeCors()
        {
            DefaultHttpContext context = new DefaultHttpContext();
            context.Request.Path = "/api/vistorias/1/avarias";
            context.Response.Body = new MemoryStream();
            BadHttpRequestException excecao = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);
            RequestDelegate proximo = contextoRecebido =>
            {
                contextoRecebido.Response.Headers["Access-Control-Allow-Origin"] = ConstantsValues.OrigemFrontendLocal;
                throw excecao;
            };
            ErrorHandlingMiddleware middleware = new ErrorHandlingMiddleware(proximo, new FakeLogService());

            await middleware.InvokeAsync(context);

            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal(ConstantsValues.OrigemFrontendLocal, context.Response.Headers["Access-Control-Allow-Origin"].ToString());
        }
    }
}
