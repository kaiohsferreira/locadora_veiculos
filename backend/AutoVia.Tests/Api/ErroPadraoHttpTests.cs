using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Formato unico de erro da API (item 4 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026). Cobre: o 400 de
    /// validacao (FluentValidation) e o 400 de model binding saem com type e traceId, e com errors e detail como antes; o 404
    /// de rota inexistente e o 405 de metodo nao aceito saem em ProblemDetails; 401, 403 e 429 mantem o mesmo corpo. Servidor
    /// em memoria (fabricas de teste), sem PostgreSQL.
    /// </summary>
    public class ErroPadraoHttpTests : IDisposable
    {
        private const string RotaVeiculos = "/api/veiculos";
        private const string RotaInexistente = "/api/rota-que-nao-existe";
        private const string RotaLogin = "/api/auth/login";
        private const string MediaProblema = "application/problem+json";
        private const string PrefixoTipo = "https://httpstatuses.io/";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string CpfLogin = "39053344705";
        private const string CorpoLoginSenhaErrada = "{\"perfil\":\"FUNCIONARIO\",\"cpf\":\"" + CpfLogin + "\",\"senha\":\"SenhaErrada1\",\"lembrar\":false}";

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public ErroPadraoHttpTests()
        {
            _fabrica = new VeiculoWebFactory();
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        [Fact]
        public async Task Validacao400_TemTypeETraceIdEMantemErrosEDetail()
        {
            VeiculoVO vo = CriarVeiculoValido(_fabrica.CategoriaSuv.IdCategoria);
            vo.AnoFabricacao = 2022;
            vo.AnoModelo = 2020;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaVeiculos, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "400", corpo.GetProperty("type").GetString());
            Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
            Assert.Equal(ConstantsMessage.TituloErroValidacao, corpo.GetProperty("title").GetString());
            Assert.Equal(400, corpo.GetProperty("status").GetInt32());
            Assert.Equal(RotaVeiculos, corpo.GetProperty("instance").GetString());
            Assert.Equal(ConstantsMessage.ErroVeiculoAnoModeloAnterior, corpo.GetProperty("detail").GetString());
            Assert.Equal(ConstantsMessage.ErroVeiculoAnoModeloAnterior, corpo.GetProperty("errors").GetProperty("anoModelo")[0].GetString());
        }

        [Fact]
        public async Task ModelBinding400_TemTypeETraceIdEErrorsComChavePascal()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}?status=99", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "400", corpo.GetProperty("type").GetString());
            Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
            Assert.Equal(ConstantsMessage.TituloErroValidacao, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, corpo.GetProperty("detail").GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "Status"), corpo.GetProperty("errors").GetProperty("Status")[0].GetString());
        }

        /// <summary>Sem token, a rota inexistente cai na politica de fallback (usuario autenticado) e responde 401 com corpo, como hoje.</summary>
        [Fact]
        public async Task RotaInexistenteSemToken_Continua401ComCorpo()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaInexistente, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty("title").GetString());
        }

        [Fact]
        public async Task RotaInexistenteComToken_Devolve404ProblemComCorpo()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaInexistente, TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "404", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloRecursoNaoEncontrado, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroRecursoNaoEncontrado, corpo.GetProperty("detail").GetString());
            Assert.Equal(RotaInexistente, corpo.GetProperty("instance").GetString());
            Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
        }

        [Fact]
        public async Task MetodoNaoPermitido_Devolve405ProblemComAllow()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Delete, RotaVeiculos, TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.MethodNotAllowed, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PrefixoTipo + "405", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloMetodoNaoPermitido, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroMetodoNaoPermitido, corpo.GetProperty("detail").GetString());
            Assert.Equal(RotaVeiculos, corpo.GetProperty("instance").GetString());
            Assert.False(string.IsNullOrWhiteSpace(corpo.GetProperty("traceId").GetString()));
            Assert.Contains(HttpMethod.Get.Method, resposta.Content.Headers.Allow);
            Assert.Contains(HttpMethod.Post.Method, resposta.Content.Headers.Allow);
        }

        [Fact]
        public async Task SemToken401_MantemOCorpoDeProblemaComWwwAuthenticate()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaVeiculos, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Contains(resposta.Headers.WwwAuthenticate, cabecalho => cabecalho.Scheme == ConstantsValues.TipoTokenBearer);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ChavesDeProblemaSemErros, ChavesDoCorpo(corpo));
            Assert.Equal(PrefixoTipo + "401", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task TokenDeCliente403_MantemOCorpoDeProblema()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaVeiculos, TokenDeCliente()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ChavesDeProblemaSemErros, ChavesDoCorpo(corpo));
            Assert.Equal(PrefixoTipo + "403", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroAcessoNegado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task LimiteDeRequisicoes429_MantemOCorpoDeProblema()
        {
            using AutenticacaoWebFactoryLimitado fabricaLimitada = new AutenticacaoWebFactoryLimitado();
            using HttpClient clienteLimitado = fabricaLimitada.CreateClient();

            HttpResponseMessage ultima = null!;
            for (int tentativa = 0; tentativa < 3; tentativa++)
            {
                ultima = await clienteLimitado.SendAsync(JsonRequisicao(HttpMethod.Post, RotaLogin, CorpoLoginSenhaErrada));
            }

            Assert.Equal((HttpStatusCode)429, ultima.StatusCode);
            Assert.Equal(MediaProblema, ultima.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(ultima);
            Assert.Equal(ChavesDeProblemaSemErros, ChavesDoCorpo(corpo));
            Assert.Equal(PrefixoTipo + "429", corpo.GetProperty("type").GetString());
            Assert.Equal(ConstantsMessage.TituloLimiteRequisicoes, corpo.GetProperty("title").GetString());
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty("detail").GetString());
        }

        /// <summary>Chaves do ProblemDetails sem errors, em ordem alfabetica (o corpo de 401, 403 e 429 nao tem errors).</summary>
        private static readonly string[] ChavesDeProblemaSemErros = { "detail", "instance", "status", "title", "traceId", "type" };

        private static List<string> ChavesDoCorpo(JsonElement corpo)
        {
            List<string> chaves = new List<string>();

            foreach (JsonProperty propriedade in corpo.EnumerateObject())
            {
                chaves.Add(propriedade.Name);
            }

            chaves.Sort(StringComparer.Ordinal);

            return chaves;
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE);
        }

        private string TokenDeCliente()
        {
            return GerarToken(PerfilUsuario.CLIENTE, null);
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor,
                Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, 1, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, 1).Token;
        }

        private static VeiculoVO CriarVeiculoValido(int idCategoria)
        {
            VeiculoVO vo = new VeiculoVO
            {
                IdCategoria = idCategoria,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                Cor = "Branco",
                KmAtual = 0,
                ValorDiaria = 150m
            };

            return vo;
        }

        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token, VeiculoVO? corpo = null)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            if (corpo != null)
            {
                requisicao.Content = new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
            }

            return requisicao;
        }

        private static HttpRequestMessage JsonRequisicao(HttpMethod metodo, string rota, string json)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);
            requisicao.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return requisicao;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }
}
