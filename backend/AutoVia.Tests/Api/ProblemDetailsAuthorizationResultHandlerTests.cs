using AutoVia.Api.Authorization;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do ProblemDetailsAuthorizationResultHandler: sem token valido devolve 401 em
    /// problem+json com WWW-Authenticate: Bearer; token sem o perfil devolve 403; politica
    /// atendida segue para a action sem alterar a resposta.
    /// </summary>
    public class ProblemDetailsAuthorizationResultHandlerTests
    {
        private static AuthorizationPolicy PoliticaQualquer()
        {
            return new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        }

        private static async Task<JsonElement> LerCorpoAsync(DefaultHttpContext contexto)
        {
            contexto.Response.Body.Position = 0;
            JsonDocument documento = await JsonDocument.ParseAsync(contexto.Response.Body);

            return documento.RootElement.Clone();
        }

        private static DefaultHttpContext CriarContexto()
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Request.Path = "/api/teste";
            contexto.Response.Body = new MemoryStream();

            return contexto;
        }

        [Fact]
        public async Task Challenge_Devolve401ProblemComWwwAuthenticateBearer()
        {
            ProblemDetailsAuthorizationResultHandler handler = new ProblemDetailsAuthorizationResultHandler(new FakeLogService());
            DefaultHttpContext contexto = CriarContexto();
            bool proximoChamado = false;
            RequestDelegate next = _ =>
            {
                proximoChamado = true;
                return Task.CompletedTask;
            };

            await handler.HandleAsync(next, contexto, PoliticaQualquer(), PolicyAuthorizationResult.Challenge());

            Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
            Assert.False(proximoChamado);
            Assert.StartsWith("application/problem+json", contexto.Response.ContentType);
            Assert.Equal(ConstantsValues.TipoTokenBearer, contexto.Response.Headers[HeaderNames.WWWAuthenticate].ToString());
            JsonElement corpo = await LerCorpoAsync(contexto);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Forbid_Devolve403ProblemSemWwwAuthenticate()
        {
            ProblemDetailsAuthorizationResultHandler handler = new ProblemDetailsAuthorizationResultHandler(new FakeLogService());
            DefaultHttpContext contexto = CriarContexto();
            bool proximoChamado = false;
            RequestDelegate next = _ =>
            {
                proximoChamado = true;
                return Task.CompletedTask;
            };

            await handler.HandleAsync(next, contexto, PoliticaQualquer(), PolicyAuthorizationResult.Forbid());

            Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
            Assert.False(proximoChamado);
            Assert.StartsWith("application/problem+json", contexto.Response.ContentType);
            Assert.False(contexto.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
            JsonElement corpo = await LerCorpoAsync(contexto);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroAcessoNegado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task PoliticaAtendida_SegueParaAActionSemAlterarARespostaDeErro()
        {
            ProblemDetailsAuthorizationResultHandler handler = new ProblemDetailsAuthorizationResultHandler(new FakeLogService());
            DefaultHttpContext contexto = CriarContexto();
            bool proximoChamado = false;
            RequestDelegate next = _ =>
            {
                proximoChamado = true;
                return Task.CompletedTask;
            };

            await handler.HandleAsync(next, contexto, PoliticaQualquer(), PolicyAuthorizationResult.Success());

            Assert.True(proximoChamado);
            Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
        }
    }
}
