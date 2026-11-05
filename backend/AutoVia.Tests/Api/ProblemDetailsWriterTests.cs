using AutoVia.Api.Middleware;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do ProblemDetailsWriter sobre um HttpContext em memoria: os cabecalhos de CORS
    /// (Access-Control-*) e Vary que ja estao na resposta sobrevivem a limpeza, enquanto o
    /// cabecalho comum e removido. Status e WWW-Authenticate continuam sendo gravados.
    /// </summary>
    public class ProblemDetailsWriterTests
    {
        private const string CabecalhoComum = "X-Teste-Limpeza";
        private const string ValorCabecalhoComum = "removido";
        private const string MediaProblema = "application/problem+json";

        [Fact]
        public async Task WriteAsync_PreservaCabecalhosDeCorsEVary()
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Response.Body = new MemoryStream();
            contexto.Response.Headers[HeaderNames.AccessControlAllowOrigin] = ConstantsValues.OrigemFrontendLocal;
            contexto.Response.Headers[HeaderNames.AccessControlExposeHeaders] = HeaderNames.Location;
            contexto.Response.Headers[HeaderNames.Vary] = HeaderNames.Origin;
            contexto.Response.Headers[CabecalhoComum] = ValorCabecalhoComum;

            await ProblemDetailsWriter.WriteAsync(contexto, StatusCodes.Status401Unauthorized, ConstantsMessage.TituloNaoAutenticado, ConstantsMessage.ErroNaoAutenticado, ConstantsValues.TipoTokenBearer);

            Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
            Assert.Equal(MediaProblema, contexto.Response.ContentType);
            Assert.Equal(ConstantsValues.TipoTokenBearer, contexto.Response.Headers[HeaderNames.WWWAuthenticate].ToString());
            Assert.Equal(ConstantsValues.OrigemFrontendLocal, contexto.Response.Headers[HeaderNames.AccessControlAllowOrigin].ToString());
            Assert.Equal(HeaderNames.Location, contexto.Response.Headers[HeaderNames.AccessControlExposeHeaders].ToString());
            Assert.Equal(HeaderNames.Origin, contexto.Response.Headers[HeaderNames.Vary].ToString());
            Assert.False(contexto.Response.Headers.ContainsKey(CabecalhoComum));
        }

        [Fact]
        public async Task WriteAsync_SemCabecalhoDeCors_GravaSoOProblema()
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Response.Body = new MemoryStream();

            await ProblemDetailsWriter.WriteAsync(contexto, StatusCodes.Status403Forbidden, ConstantsMessage.TituloAcessoNegado, ConstantsMessage.ErroAcessoNegado);

            Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
            Assert.Equal(MediaProblema, contexto.Response.ContentType);
            Assert.False(contexto.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
            Assert.False(contexto.Response.Headers.ContainsKey(HeaderNames.AccessControlAllowOrigin));
        }
    }
}
