using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP de /api/seguros com o servidor em memoria (sem PostgreSQL). Comprovam o
    /// criterio do RF-41 pelo pipeline real: vigencia invertida devolve 400 problem+json,
    /// 401 sem token, 403 com token de cliente, 201 com Location e o destaque venceEm60Dias
    /// em camelCase na listagem.
    /// </summary>
    public class SeguroHttpTests : IDisposable
    {
        private const string RotaSeguros = "/api/seguros";
        private const string MediaProblema = "application/problem+json";

        private readonly SeguroWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public SeguroHttpTests()
        {
            _fabrica = new SeguroWebFactory();
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        [Fact]
        public async Task SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaSeguros, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403()
        {
            string tokenCliente = GerarToken(PerfilUsuario.CLIENTE, null);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaSeguros, tokenCliente));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Fact]
        public async Task Post_VigenciaInvertida_Devolve400ComMensagemDeVigencia()
        {
            SeguroVO vo = CriarVOValido();
            vo.DataInicio = new DateOnly(2026, 12, 31);
            vo.DataFim = new DateOnly(2026, 1, 1);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaSeguros, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroSeguroVigenciaInvalida, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Post_Valido_Devolve201ComLocation()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaSeguros, TokenDeFuncionario(), CriarVOValido()));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            int idSeguro = corpo.GetProperty("idSeguro").GetInt32();
            Assert.True(idSeguro > 0);
            Assert.Equal($"{RotaSeguros}/{idSeguro}", resposta.Headers.Location?.AbsolutePath);
        }

        [Fact]
        public async Task Post_NumeroDuplicado_Devolve409()
        {
            await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaSeguros, TokenDeFuncionario(), CriarVOValido()));

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaSeguros, TokenDeFuncionario(), CriarVOValido()));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroSeguroNumeroApoliceDuplicado, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task GetLista_ApoliceQueVenceEm30Dias_VemDestacada()
        {
            DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);
            _fabrica.Seguros.Adicionar(new Seguro
            {
                IdVeiculo = _fabrica.Veiculo.IdVeiculo,
                Veiculo = _fabrica.Veiculo,
                Seguradora = "Porto Seguro",
                NumeroApolice = "AP-30",
                DataInicio = hoje.AddYears(-1),
                DataFim = hoje.AddDays(30),
                ValorPremio = 900m
            });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaSeguros}?apenasAVencer=true", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            JsonElement item = corpo.GetProperty("itens")[0];
            Assert.Equal("AP-30", item.GetProperty("numeroApolice").GetString());
            Assert.True(item.GetProperty("venceEm60Dias").GetBoolean());
            Assert.Equal(30, item.GetProperty("diasParaVencer").GetInt32());
        }

        private SeguroVO CriarVOValido()
        {
            DateOnly hoje = DateOnly.FromDateTime(DateTime.Now);
            SeguroVO vo = new SeguroVO
            {
                IdVeiculo = _fabrica.Veiculo.IdVeiculo,
                Seguradora = "Porto Seguro",
                NumeroApolice = "AP-HTTP-1",
                DataInicio = hoje,
                DataFim = hoje.AddYears(1),
                ValorPremio = 1200m,
                Cobertura = "Compreensiva"
            };

            return vo;
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
            string token = tokens.GerarToken(perfil, 1, "Usuario de teste", cargo).Token;

            return token;
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.GESTOR_FROTA);
        }

        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token, SeguroVO? corpo = null)
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

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }

    /// <summary>
    /// Servidor de teste dos seguros: reaproveita o VeiculoWebFactory (configuracao em memoria,
    /// chave JWT, fakes de veiculo e categoria) e troca tambem o ISeguroRepository por fake.
    /// Semeia um veiculo ativo para as apolices.
    /// </summary>
    public class SeguroWebFactory : VeiculoWebFactory
    {
        public SeguroWebFactory()
        {
            Seguros = new FakeSeguroRepository();
            Veiculo = new Veiculo
            {
                IdCategoria = CategoriaSuv.IdCategoria,
                Categoria = CategoriaSuv,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                ValorDiaria = 150m,
                Status = StatusVeiculo.DISPONIVEL
            };
            Veiculos.Adicionar(Veiculo);
        }

        /// <summary>Repositorio de apolices em memoria.</summary>
        public FakeSeguroRepository Seguros { get; }

        /// <summary>Veiculo ativo semeado para receber apolices.</summary>
        public Veiculo Veiculo { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISeguroRepository>();
                services.AddSingleton<ISeguroRepository>(Seguros);
            });
        }
    }
}
