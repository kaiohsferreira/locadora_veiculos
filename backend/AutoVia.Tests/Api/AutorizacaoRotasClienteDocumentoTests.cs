using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Declaracoes da controller de documentos do cliente: politica Cliente na classe, nenhuma acao anonima, limite por IP proprio
    /// em cada acao, no-store, teto de corpo do envio e a rota. Em arquivo proprio (AutorizacaoRotasTests.cs nao muda).
    /// </summary>
    public sealed class AutorizacaoRotasClienteDocumentoTests
    {
        private const string NomeEnviar = nameof(ClienteDocumentoController.Enviar);
        private const string NomeListar = nameof(ClienteDocumentoController.Listar);

        [Fact]
        public void Classe_ExigePoliticaClienteEARotaDeDocumentos()
        {
            AuthorizeAttribute? autorizacao = typeof(ClienteDocumentoController).GetCustomAttribute<AuthorizeAttribute>();
            RouteAttribute? rota = typeof(ClienteDocumentoController).GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaCliente, autorizacao.Policy);
            Assert.NotNull(rota);
            Assert.Equal("api/clientes/{idCliente:int}/documentos", rota.Template);
            Assert.True(Attribute.IsDefined(typeof(ClienteDocumentoController), typeof(ApiControllerAttribute)));
        }

        [Fact]
        public void NenhumaAcao_EAnonima()
        {
            foreach (MethodInfo acao in typeof(ClienteDocumentoController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.False(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)), acao.Name);
            }
        }

        [Fact]
        public void Enviar_TemLimiteDaPoliticaDeEnvioETetoDeCorpo()
        {
            MethodInfo acao = typeof(ClienteDocumentoController).GetMethod(NomeEnviar)!;

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();
            RequestSizeLimitAttribute? corpo = acao.GetCustomAttribute<RequestSizeLimitAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteEnvioDocumentoCliente, limite.PolicyName);
            Assert.NotNull(corpo);
            IRequestSizeLimitMetadata metadado = corpo;
            Assert.Equal<long?>(ConstantsValues.TamanhoMaximoCorpoDocumentoCliente, metadado.MaxRequestBodySize);
            Assert.NotNull(acao.GetCustomAttribute<ConsumesAttribute>());
        }

        [Fact]
        public void Listar_TemLimiteDaPoliticaDeListagemECaiNoLimiteProprio()
        {
            MethodInfo acao = typeof(ClienteDocumentoController).GetMethod(NomeListar)!;

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteListaDocumentoCliente, limite.PolicyName);
            Assert.NotEqual(ConstantsValues.PoliticaLimiteEnvioDocumentoCliente, limite.PolicyName);
        }

        [Fact]
        public void Envio_E_Listagem_TemNoStore()
        {
            ResponseCacheAttribute? envio = typeof(ClienteDocumentoController).GetMethod(NomeEnviar)!.GetCustomAttribute<ResponseCacheAttribute>();
            ResponseCacheAttribute? lista = typeof(ClienteDocumentoController).GetMethod(NomeListar)!.GetCustomAttribute<ResponseCacheAttribute>();

            Assert.NotNull(envio);
            Assert.True(envio.NoStore);
            Assert.NotNull(lista);
            Assert.True(lista.NoStore);
        }

        [Fact]
        public void Envio_DeclaraOs201e429E_Listagem_DeclaraO200()
        {
            List<int> codigosEnvio = typeof(ClienteDocumentoController).GetMethod(NomeEnviar)!
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(atributo => atributo.StatusCode)
                .ToList();
            List<int> codigosListagem = typeof(ClienteDocumentoController).GetMethod(NomeListar)!
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Select(atributo => atributo.StatusCode)
                .ToList();

            Assert.Contains(StatusCodes.Status201Created, codigosEnvio);
            Assert.Contains(StatusCodes.Status400BadRequest, codigosEnvio);
            Assert.Contains(StatusCodes.Status404NotFound, codigosEnvio);
            Assert.Contains(StatusCodes.Status429TooManyRequests, codigosEnvio);
            Assert.Contains(StatusCodes.Status502BadGateway, codigosEnvio);
            Assert.Contains(StatusCodes.Status200OK, codigosListagem);
            Assert.Contains(StatusCodes.Status404NotFound, codigosListagem);
        }
    }
}
