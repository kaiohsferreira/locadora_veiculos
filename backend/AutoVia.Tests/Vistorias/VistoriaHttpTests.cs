using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Api;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Vistorias
{
    public class VistoriaHttpTests : IDisposable
    {
        private const string Rota = "/api/locacoes/10/vistorias/entrada";
        private const string CorpoValido = "{\"kmRegistrado\":100,\"nivelCombustivel\":0.75,\"observacoes\":\"  Bom estado  \"}";
        private readonly VistoriaWebFactory _fabrica = new VistoriaWebFactory();
        private readonly HttpClient _cliente;

        public VistoriaHttpTests()
        {
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        private string Token(PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120, Chave = _fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            return tokens.GerarToken(perfil, 20, "Teste", perfil == PerfilUsuario.FUNCIONARIO ? CargoFuncionario.ATENDENTE : null,
                "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? 1 : null).Token;
        }

        private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string rota, string? token, string? corpo = null)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);
            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }
            if (corpo != null)
            {
                requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
            }
            return await _cliente.SendAsync(requisicao);
        }

        private static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        {
            string json = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(json);
            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task Post201ComLocationConsultavelDuplicidade409EValoresDoServidor()
        {
            string token = Token();
            // Campos desconhecidos nao podem sobrescrever identidade, tipo nem relogio.
            string corpo = "{\"kmRegistrado\":100,\"nivelCombustivel\":0.75,\"observacoes\":\"  Bom estado  \",\"idFuncionario\":999,\"tipo\":\"SAIDA\",\"dataHora\":\"2000-01-01T00:00:00\"}";
            using HttpResponseMessage post = await Enviar(HttpMethod.Post, Rota, token, corpo);
            Assert.Equal(HttpStatusCode.Created, post.StatusCode);
            Assert.Equal(Rota, post.Headers.Location?.AbsolutePath);
            JsonElement criado = await LerJson(post);
            Assert.Equal(20, criado.GetProperty("idFuncionario").GetInt32());
            Assert.Equal("ENTRADA", criado.GetProperty("tipo").GetString());
            Assert.Equal("17/11/2026 10:30", criado.GetProperty("dataHoraStr").GetString());
            Assert.Equal("Bom estado", criado.GetProperty("observacoes").GetString());
            using HttpResponseMessage get = await Enviar(HttpMethod.Get, post.Headers.Location!.PathAndQuery, token);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            JsonElement lido = await LerJson(get);
            Assert.Equal(criado.GetProperty("idVistoria").GetInt32(), lido.GetProperty("idVistoria").GetInt32());
            using HttpResponseMessage duplicado = await Enviar(HttpMethod.Post, Rota, token, CorpoValido);
            Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);
            Assert.Equal("application/problem+json", duplicado.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"kmRegistrado\":0}")]
        [InlineData("{\"kmRegistrado\":-1,\"nivelCombustivel\":1}")]
        [InlineData("{\"kmRegistrado\":0,\"nivelCombustivel\":1.01}")]
        [InlineData("{\"kmRegistrado\":0,\"nivelCombustivel\":1.5}")]
        [InlineData("{\"kmRegistrado\":0,\"nivelCombustivel\":0.001}")]
        [InlineData("{\"kmRegistrado\":0,\"nivelCombustivel\":1,\"observacoes\":\"x\\u0000y\"}")]
        public async Task EntradaInvalida400PeloFiltroRealSemPersistir(string corpo)
        {
            using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
            Assert.Empty(_fabrica.Vistorias.Vistorias);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task SemToken401Cliente403(string metodo)
        {
            HttpMethod http = new HttpMethod(metodo);
            using HttpResponseMessage anonima = await Enviar(http, Rota, null, metodo == "POST" ? CorpoValido : null);
            Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
            using HttpResponseMessage cliente = await Enviar(http, Rota, Token(PerfilUsuario.CLIENTE), metodo == "POST" ? CorpoValido : null);
            Assert.Equal(HttpStatusCode.Forbidden, cliente.StatusCode);
        }

        [Fact]
        public async Task GetEntradaAusente404ELocacaoAusente404()
        {
            using HttpResponseMessage semEntrada = await Enviar(HttpMethod.Get, Rota, Token());
            Assert.Equal(HttpStatusCode.NotFound, semEntrada.StatusCode);
            using HttpResponseMessage semLocacao = await Enviar(HttpMethod.Post, "/api/locacoes/99/vistorias/entrada", Token(), CorpoValido);
            Assert.Equal(HttpStatusCode.NotFound, semLocacao.StatusCode);
        }
    }

    public class VistoriaWebFactory : VeiculoWebFactory
    {
        public FakeVistoriaRepository Vistorias { get; } = new FakeVistoriaRepository();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                FakeLocacaoRepository locacoes = new FakeLocacaoRepository();
                locacoes.Adicionar(new Locacao { IdLocacao = 10 });
                locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
                services.RemoveAll<ILocacaoRepository>();
                services.RemoveAll<IVistoriaRepository>();
                services.RemoveAll<IRelogio>();
                services.AddSingleton<ILocacaoRepository>(locacoes);
                services.AddSingleton<IVistoriaRepository>(Vistorias);
                services.AddSingleton<IRelogio>(new FakeRelogio(VistoriaServiceTests.Agora));
            });
        }
    }
}
