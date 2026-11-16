using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Politica de limite propria das leituras publicas de veiculo (veiculo.contract.json v3, T6, T7 e T8; D-3):
    /// AutoViaLeituraPublica, 60 por minuto por IP por padrao, contador somado entre busca e catalogo, separado do login.
    /// Estouro responde 429 em problem+json. A politica por atributo e checada por reflexao; o comportamento, por HTTP,
    /// nas duas direcoes da independencia (leitura nao derruba o login e login nao derruba a leitura) e o 401 da rota
    /// de funcionario sem token.
    /// </summary>
    public class LimiteRotasPublicasTests
    {
        private const string CaminhoBusca = "/api/veiculos/disponiveis?dataRetirada=2026-12-10&dataDevolucao=2026-12-12";
        private const string CaminhoCatalogo = "/api/veiculos/a-venda";
        private const string CaminhoListagemDaFrota = "/api/veiculos";
        private const string CaminhoLogin = "/api/auth/login";
        private const string NomeAcaoCatalogo = nameof(VeiculoController.GetCatalogo);
        private const string NomeAcaoLogin = nameof(AuthController.Login);
        private const string CampoTitulo = "title";
        private const string CampoDetalhe = "detail";
        private const string CampoErros = "errors";
        private const string CampoPagina = "pagina";
        private const string LimiteDeDoisPorMinuto = "2";
        private const string LimiteDeZero = "0";
        private const string LimiteNegativo = "-5";
        private const int PaginaInvalidaDoTeto = int.MaxValue;
        private const int TamanhoPaginaDoTeto = 100;
        private const int LimiteDoTesteAltoParaNaoInterferir = 1000;

        private static EnableRateLimitingAttribute? LimiteDaAcao(MethodInfo metodo)
        {
            return metodo.GetCustomAttribute<EnableRateLimitingAttribute>();
        }

        private static MethodInfo MetodoCatalogo()
        {
            return typeof(VeiculoController).GetMethod(NomeAcaoCatalogo)!;
        }

        private static async Task<JsonElement> LerCorpoDeProblema(HttpResponseMessage resposta)
        {
            string corpo = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(corpo);

            return documento.RootElement.Clone();
        }

        private static async Task<HttpResponseMessage> PostarLoginVazio(HttpClient cliente)
        {
            using StringContent corpoLogin = new StringContent("{}", Encoding.UTF8, "application/json");

            return await cliente.PostAsync(CaminhoLogin, corpoLogin);
        }

        [Fact]
        public void PoliticaDaBusca_EhAPropriaDasLeiturasPublicas_EDiferenteDoLogin()
        {
            EnableRateLimitingAttribute? limite = typeof(VeiculoDisponibilidadeController).GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteLeituraPublica, limite.PolicyName);
            Assert.NotEqual(ConstantsValues.PoliticaLimitePublico, limite.PolicyName);
        }

        [Fact]
        public void PoliticaDoCatalogo_EhNaAcaoEDiferenteDoLogin()
        {
            EnableRateLimitingAttribute? limite = LimiteDaAcao(MetodoCatalogo());

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimiteLeituraPublica, limite.PolicyName);
            Assert.NotEqual(ConstantsValues.PoliticaLimitePublico, limite.PolicyName);
        }

        [Fact]
        public void ClasseVeiculoController_NaoTemLimiteNaClasse_SoNaAcaoDoCatalogo()
        {
            Assert.Null(typeof(VeiculoController).GetCustomAttribute<EnableRateLimitingAttribute>());
            Assert.NotNull(MetodoCatalogo().GetCustomAttribute<AllowAnonymousAttribute>());
        }

        [Fact]
        public void LoginContinuaNaPoliticaPropria_NaoCompartilhaContadorComAsLeituras()
        {
            List<string?> politicasDoLogin = typeof(AuthController).GetMethods()
                .SelectMany(metodo => metodo.GetCustomAttributes<EnableRateLimitingAttribute>())
                .Select(limite => limite.PolicyName)
                .ToList();

            Assert.NotEmpty(politicasDoLogin);
            Assert.All(politicasDoLogin, politica => Assert.Equal(ConstantsValues.PoliticaLimitePublico, politica));
            Assert.DoesNotContain(ConstantsValues.PoliticaLimiteLeituraPublica, politicasDoLogin);
        }

        [Fact]
        public void AsDuasRotasDeLeitura_DeclaramORetorno429()
        {
            Assert.Contains(typeof(VeiculoDisponibilidadeController).GetMethod(nameof(VeiculoDisponibilidadeController.GetDisponiveis))!.GetCustomAttributes<ProducesResponseTypeAttribute>(),
                atributo => atributo.StatusCode == StatusCodes.Status429TooManyRequests);
            Assert.Contains(MetodoCatalogo().GetCustomAttributes<ProducesResponseTypeAttribute>(),
                atributo => atributo.StatusCode == StatusCodes.Status429TooManyRequests);
        }

        [Fact]
        public void ConstantesDoLimite_TemOsValoresDoContrato()
        {
            Assert.Equal("AutoViaLeituraPublica", ConstantsValues.PoliticaLimiteLeituraPublica);
            Assert.Equal("Veiculos:LimiteLeituraPublicaPorMinuto", ConstantsValues.ChaveLimiteLeituraPublica);
            Assert.Equal(60, ConstantsValues.LimiteLeituraPublicaPorMinutoPadrao);
        }

        [Fact]
        public async Task BuscaESomaComOCatalogo_NoMesmoContador_E429NaTerceiraChamada()
        {
            using LimiteLeituraWebFactory fabrica = new LimiteLeituraWebFactory(LimiteDeDoisPorMinuto);
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage primeira = await cliente.GetAsync(CaminhoBusca);
            HttpResponseMessage segunda = await cliente.GetAsync(CaminhoCatalogo);
            HttpResponseMessage terceira = await cliente.GetAsync(CaminhoBusca);

            Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
            Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
            Assert.Equal("application/problem+json", terceira.Content.Headers.ContentType?.MediaType);

            JsonElement corpo = await LerCorpoDeProblema(terceira);
            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, corpo.GetProperty(CampoTitulo).GetString());
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty(CampoDetalhe).GetString());
        }

        [Fact]
        public async Task LoginNaoRespondeMaisEstourado_DepoisDoEstouroDasLeituras()
        {
            using LimiteLeituraWebFactory fabrica = new LimiteLeituraWebFactory(LimiteDeDoisPorMinuto);
            using HttpClient cliente = fabrica.CreateClient();
            await cliente.GetAsync(CaminhoBusca);
            await cliente.GetAsync(CaminhoCatalogo);
            await cliente.GetAsync(CaminhoBusca);

            HttpResponseMessage login = await PostarLoginVazio(cliente);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, login.StatusCode);
        }

        [Fact]
        public async Task LoginEstourado_NaoTiraABuscaDasLeituras_NemOCatalogo()
        {
            using LimiteLoginELeituraWebFactory fabrica = new LimiteLoginELeituraWebFactory(LimiteDeDoisPorMinuto, LimiteDoTesteAltoParaNaoInterferir.ToString());
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage primeiroLogin = await PostarLoginVazio(cliente);
            HttpResponseMessage segundoLogin = await PostarLoginVazio(cliente);
            HttpResponseMessage terceiroLogin = await PostarLoginVazio(cliente);
            HttpResponseMessage busca = await cliente.GetAsync(CaminhoBusca);
            HttpResponseMessage catalogo = await cliente.GetAsync(CaminhoCatalogo);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, primeiroLogin.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, segundoLogin.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceiroLogin.StatusCode);
            Assert.Equal(HttpStatusCode.OK, busca.StatusCode);
            Assert.Equal(HttpStatusCode.OK, catalogo.StatusCode);
        }

        [Fact]
        public async Task ListagemDaFrotaSemToken_RespondeNaoAutenticado()
        {
            using LimiteLeituraWebFactory fabrica = new LimiteLeituraWebFactory(LimiteDoTesteAltoParaNaoInterferir.ToString());
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.GetAsync(CaminhoListagemDaFrota);

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            JsonElement corpo = await LerCorpoDeProblema(resposta);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty(CampoTitulo).GetString());
            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, corpo.GetProperty(CampoDetalhe).GetString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(LimiteDeZero)]
        [InlineData(LimiteNegativo)]
        public async Task ChaveAusenteZeroOuNegativa_CaiNoPadraoDe60(string? valorConfigurado)
        {
            using LimiteLeituraWebFactory fabrica = new LimiteLeituraWebFactory(valorConfigurado);
            using HttpClient cliente = fabrica.CreateClient();

            for (int chamada = 0; chamada < 3; chamada++)
            {
                HttpResponseMessage resposta = await cliente.GetAsync(CaminhoCatalogo);
                Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            }
        }

        [Fact]
        public async Task PaginaDoTetoDoInt_DaErro400NaRotaPublica_ComAMensagemDeDeslocamento()
        {
            using LimiteLeituraWebFactory fabrica = new LimiteLeituraWebFactory(LimiteDoTesteAltoParaNaoInterferir.ToString());
            using HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.GetAsync($"{CaminhoCatalogo}?pagina={PaginaInvalidaDoTeto}&tamanhoPagina={TamanhoPaginaDoTeto}");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerCorpoDeProblema(resposta);
            Assert.Equal(ConstantsMessage.ErroPaginaDeslocamentoInvalido, corpo.GetProperty(CampoErros).GetProperty(CampoPagina)[0].GetString());
        }
    }

    /// <summary>
    /// Servidor de teste das leituras publicas com Veiculos:LimiteLeituraPublicaPorMinuto no valor informado. Valor nulo
    /// e gravado como chave com valor nulo: sobrepoe o 60 do appsettings e cai no padrao do LerLimite.
    /// </summary>
    public class LimiteLeituraWebFactory : VeiculoWebFactory
    {
        private readonly string? _valorLimite;

        public LimiteLeituraWebFactory(string? valorLimite)
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
                    { ConstantsValues.ChaveLimiteLeituraPublica, _valorLimite }
                });
            });
        }
    }

    /// <summary>
    /// Servidor de teste com dois limites: o do login (Autenticacao:LimitePublicoPorMinuto, que a VeiculoWebFactory
    /// fixa em 1000) e o das leituras publicas (Veiculos:LimiteLeituraPublicaPorMinuto). Prova a independencia
    /// dos contadores nas duas direcoes.
    /// </summary>
    public class LimiteLoginELeituraWebFactory : VeiculoWebFactory
    {
        private readonly string _limiteLogin;
        private readonly string _limiteLeitura;

        public LimiteLoginELeituraWebFactory(string limiteLogin, string limiteLeitura)
        {
            _limiteLogin = limiteLogin;
            _limiteLeitura = limiteLeitura;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((contexto, configuracao) =>
            {
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { ConstantsValues.ChaveLimitePublico, _limiteLogin },
                    { ConstantsValues.ChaveLimiteLeituraPublica, _limiteLeitura }
                });
            });
        }
    }
}
