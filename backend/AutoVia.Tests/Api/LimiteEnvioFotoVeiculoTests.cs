using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Politica de limite por IP do envio de foto (AutoViaEnvioFotoVeiculo): so a acao Enviar tem o atributo, o limite padrao e
    /// de 60 por minuto, e com o limite configurado em 2 a terceira chamada responde 429 em problem+json, antes da autenticacao.
    /// </summary>
    public sealed class LimiteEnvioFotoVeiculoTests
    {
        private const string RotaFotos = "/api/veiculos/1/fotos";
        private const string CaminhoLogin = "/api/auth/login";
        private const string CaminhoBusca = "/api/veiculos/disponiveis";
        private const int LimiteConfigurado = 2;
        private const int LimitePadrao = 60;
        private const string CampoTitle = "title";
        private const string CampoDetail = "detail";

        /// <summary>Servidor de teste com a chave de limite do envio de foto configurada em 2.</summary>
        private sealed class AutenticacaoWebFactoryLimiteEnvioFoto : AutenticacaoWebFactory
        {
            protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
            {
                valores[ConstantsValues.ChaveLimiteEnvioFotoVeiculo] = LimiteConfigurado.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        [Fact]
        public void SoAAcaoEnviarTemOLimiteDeEnvioDeFoto()
        {
            EnableRateLimitingAttribute? limite = typeof(VeiculoFotoController).GetMethod(nameof(VeiculoFotoController.Enviar))!.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteEnvioFotoVeiculo, limite.PolicyName);
            Assert.Null(typeof(VeiculoFotoController).GetMethod(nameof(VeiculoFotoController.GetAll))!.GetCustomAttribute<EnableRateLimitingAttribute>());
            Assert.Null(typeof(VeiculoFotoController).GetMethod(nameof(VeiculoFotoController.Remover))!.GetCustomAttribute<EnableRateLimitingAttribute>());
        }

        [Fact]
        public void PoliticaEChaveEPadraoDoContrato()
        {
            Assert.Equal("AutoViaEnvioFotoVeiculo", ConstantsValues.PoliticaLimiteEnvioFotoVeiculo);
            Assert.Equal("Veiculos:LimiteEnvioFotoPorMinuto", ConstantsValues.ChaveLimiteEnvioFotoVeiculo);
            Assert.Equal(LimitePadrao, ConstantsValues.LimiteEnvioFotoVeiculoPorMinutoPadrao);
        }

        [Fact]
        public async Task TerceiraChamadaComLimiteDeDois_Devolve429ComAMensagemDoLimite()
        {
            using AutenticacaoWebFactoryLimiteEnvioFoto fabrica = new AutenticacaoWebFactoryLimiteEnvioFoto();
            using HttpClient cliente = fabrica.CreateClient();

            for (int i = 0; i < LimiteConfigurado; i++)
            {
                using HttpResponseMessage dentro = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using HttpResponseMessage estourado = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
            string texto = await estourado.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.TooManyRequests, estourado.StatusCode);
            Assert.Equal("application/problem+json", estourado.Content.Headers.ContentType?.MediaType);

            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, documento.RootElement.GetProperty(CampoTitle).GetString());
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, documento.RootElement.GetProperty(CampoDetail).GetString());
        }

        [Fact]
        public async Task SemConfiguracao_Sessenta_PassamENaSessentaEUmaDa429()
        {
            using AutenticacaoWebFactory fabrica = new AutenticacaoWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            for (int i = 0; i < LimitePadrao; i++)
            {
                using HttpResponseMessage dentro = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using HttpResponseMessage estourado = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));

            Assert.Equal(HttpStatusCode.TooManyRequests, estourado.StatusCode);
        }

        [Fact]
        public void Enviar_DeclaraORetorno429()
        {
            Assert.Contains(typeof(VeiculoFotoController).GetMethod(nameof(VeiculoFotoController.Enviar))!.GetCustomAttributes<ProducesResponseTypeAttribute>(),
                atributo => atributo.StatusCode == StatusCodes.Status429TooManyRequests);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-5")]
        public async Task ChaveAusenteZeroOuNegativa_CaiNoPadraoDe60(string? valorConfigurado)
        {
            using LimiteEnvioFotoWebFactory fabrica = new LimiteEnvioFotoWebFactory(valorConfigurado);
            using HttpClient cliente = fabrica.CreateClient();

            for (int i = 0; i < LimitePadrao; i++)
            {
                using HttpResponseMessage dentro = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using HttpResponseMessage estourado = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));

            Assert.Equal(HttpStatusCode.TooManyRequests, estourado.StatusCode);
        }

        [Fact]
        public async Task Contador_EsgotadoOLoginEAsLeiturasPublicas_NaoTiraOEnvioDeFoto()
        {
            using LimiteEnvioLoginLeituraWebFactory fabrica = new LimiteEnvioLoginLeituraWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            for (int i = 0; i < LimiteConfigurado; i++)
            {
                using HttpResponseMessage login = await PostarLoginVazio(cliente);
                using HttpResponseMessage busca = await cliente.GetAsync(CaminhoBusca);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, login.StatusCode);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, busca.StatusCode);
            }

            using HttpResponseMessage loginEstourado = await PostarLoginVazio(cliente);
            using HttpResponseMessage buscaEstourada = await cliente.GetAsync(CaminhoBusca);
            Assert.Equal(HttpStatusCode.TooManyRequests, loginEstourado.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, buscaEstourada.StatusCode);

            for (int i = 0; i < LimiteConfigurado; i++)
            {
                using HttpResponseMessage dentro = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using HttpResponseMessage envioEstourado = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
            Assert.Equal(HttpStatusCode.TooManyRequests, envioEstourado.StatusCode);
        }

        [Fact]
        public async Task Contador_EsgotadoOEnvio_NaoTiraOLoginNemAsLeiturasPublicas()
        {
            using LimiteEnvioLoginLeituraWebFactory fabrica = new LimiteEnvioLoginLeituraWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            for (int i = 0; i < LimiteConfigurado; i++)
            {
                using HttpResponseMessage dentro = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using HttpResponseMessage envioEstourado = await cliente.PostAsync(RotaFotos, new ByteArrayContent(Array.Empty<byte>()));
            Assert.Equal(HttpStatusCode.TooManyRequests, envioEstourado.StatusCode);

            using HttpResponseMessage login = await PostarLoginVazio(cliente);
            using HttpResponseMessage busca = await cliente.GetAsync(CaminhoBusca);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, login.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, busca.StatusCode);
        }

        private static async Task<HttpResponseMessage> PostarLoginVazio(HttpClient cliente)
        {
            using StringContent corpoLogin = new StringContent("{}", Encoding.UTF8, "application/json");

            return await cliente.PostAsync(CaminhoLogin, corpoLogin);
        }

        /// <summary>Servidor de teste com a chave do envio informada. Valor nulo sobrepoe o 60 do appsettings e cai no padrao do LerLimite.</summary>
        private sealed class LimiteEnvioFotoWebFactory : VeiculoWebFactory
        {
            private readonly string? _valorLimite;

            public LimiteEnvioFotoWebFactory(string? valorLimite)
            {
                _valorLimite = valorLimite;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((contexto, configuracao) =>
                {
                    configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { ConstantsValues.ChaveLimiteEnvioFotoVeiculo, _valorLimite }
                    });
                });
            }
        }

        /// <summary>Servidor de teste com login, leituras publicas e envio de foto com limite 2 cada, para provar que os contadores sao separados.</summary>
        private sealed class LimiteEnvioLoginLeituraWebFactory : VeiculoWebFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration((contexto, configuracao) =>
                {
                    string limite = LimiteConfigurado.ToString(CultureInfo.InvariantCulture);
                    configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { ConstantsValues.ChaveLimitePublico, limite },
                        { ConstantsValues.ChaveLimiteLeituraPublica, limite },
                        { ConstantsValues.ChaveLimiteEnvioFotoVeiculo, limite }
                    });
                });
            }
        }
    }
}
