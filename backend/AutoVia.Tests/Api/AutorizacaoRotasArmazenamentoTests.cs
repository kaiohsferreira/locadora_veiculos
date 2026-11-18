using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Declaracao da rota publica de leitura de arquivo: anonima por desenho (a imagem nao envia Authorization), com a rota
    /// certa, com o limite por IP so na acao Ler, e com os codigos de resposta documentados no Swagger.
    /// </summary>
    public sealed class AutorizacaoRotasArmazenamentoTests
    {
        private static readonly int[] CodigosDocumentados = new int[]
        {
            StatusCodes.Status200OK,
            StatusCodes.Status304NotModified,
            StatusCodes.Status400BadRequest,
            StatusCodes.Status404NotFound,
            StatusCodes.Status429TooManyRequests,
            StatusCodes.Status500InternalServerError
        };

        private static MethodInfo Acao()
        {
            return typeof(ArmazenamentoArquivoController).GetMethod(nameof(ArmazenamentoArquivoController.Ler))!;
        }

        [Fact]
        public void Classe_EAnonimaESemAutorizacao()
        {
            Assert.True(Attribute.IsDefined(typeof(ArmazenamentoArquivoController), typeof(AllowAnonymousAttribute)));
            Assert.False(Attribute.IsDefined(typeof(ArmazenamentoArquivoController), typeof(AuthorizeAttribute)));
        }

        [Fact]
        public void Rota_EAPublicaDeArquivosDoArmazenamento()
        {
            RouteAttribute? rota = typeof(ArmazenamentoArquivoController).GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(rota);
            Assert.Equal(ConstantsValues.RotaLeituraArquivoArmazenado, rota!.Template);
            Assert.Equal("api/armazenamento/arquivos", rota.Template);
            Assert.True(Attribute.IsDefined(typeof(ArmazenamentoArquivoController), typeof(ApiControllerAttribute)));
        }

        [Fact]
        public void Ler_EGetComLimiteDaPoliticaDaLeituraDeArquivo()
        {
            Assert.NotNull(Acao().GetCustomAttribute<HttpGetAttribute>());

            EnableRateLimitingAttribute? limite = Acao().GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteLeituraArquivo, limite!.PolicyName);
        }

        [Fact]
        public void LimiteDeIp_SoNaAcaoLer_ETaUnicaAcaoPublicaDoController()
        {
            Assert.Null(typeof(ArmazenamentoArquivoController).GetCustomAttribute<EnableRateLimitingAttribute>());
            Assert.Single(typeof(ArmazenamentoArquivoController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        }

        [Fact]
        public void Ler_DocumentaTodosOsCodigosDeResposta()
        {
            int[] declarados = Acao()
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(atributo => atributo.StatusCode)
                .ToArray();

            Assert.Equal(CodigosDocumentados.OrderBy(codigo => codigo), declarados.OrderBy(codigo => codigo));
        }
    }
}
