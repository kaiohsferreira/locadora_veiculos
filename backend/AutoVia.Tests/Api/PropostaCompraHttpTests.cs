using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP da proposta de compra (P6-BE-02) com o servidor em memoria (sem PostgreSQL), reaproveitando o
    /// VeiculoWebFactory: a simulacao e publica (200 sem token), responde 400 em problem+json para parcelas fora de 1..60
    /// e para entrada maior que o valor, 404 para veiculo fora do catalogo; o envio da proposta exige token de cliente
    /// (401 sem token, 403 com token de funcionario).
    /// </summary>
    public class PropostaCompraHttpTests : IDisposable
    {
        private const string RotaPropostas = "/api/propostas-compra";
        private const string MediaProblema = "application/problem+json";
        private const string VersaoSessaoDeTeste = "versao-de-teste";

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public PropostaCompraHttpTests()
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
        public async Task Simulacao_SemLogin_Devolve200ComAUltimaParcelaAjustada()
        {
            Veiculo veiculo = Semear(10000m, StatusVeiculo.DISPONIVEL);

            HttpResponseMessage resposta = await _cliente.GetAsync(RotaSimulacao(veiculo.IdVeiculo, "0", 3));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(veiculo.IdVeiculo, corpo.GetProperty("idVeiculo").GetInt32());
            Assert.Equal(10000m, corpo.GetProperty("valorFinanciado").GetDecimal());
            Assert.Equal(3333.33m, corpo.GetProperty("valorParcela").GetDecimal());
            Assert.Equal(3333.34m, corpo.GetProperty("valorUltimaParcela").GetDecimal());
            Assert.False(corpo.TryGetProperty("placa", out JsonElement _));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(61)]
        public async Task Simulacao_ParcelasForaDoLimite_Devolve400ProblemJson(int numeroParcelas)
        {
            Veiculo veiculo = Semear(10000m, StatusVeiculo.DISPONIVEL);

            HttpResponseMessage resposta = await _cliente.GetAsync(RotaSimulacao(veiculo.IdVeiculo, "0", numeroParcelas));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            string texto = await resposta.Content.ReadAsStringAsync();
            Assert.Contains(string.Format(ConstantsMessage.ErroPropostaParcelasForaDoLimite, 1, 60), texto);
        }

        [Fact]
        public async Task Simulacao_EntradaMaiorQueOValor_Devolve400ProblemJson()
        {
            Veiculo veiculo = Semear(10000m, StatusVeiculo.DISPONIVEL);

            HttpResponseMessage resposta = await _cliente.GetAsync(RotaSimulacao(veiculo.IdVeiculo, "10000.01", 12));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            string texto = await resposta.Content.ReadAsStringAsync();
            Assert.Contains(ConstantsMessage.ErroPropostaEntradaMaiorQueValor, texto);
        }

        [Fact]
        public async Task Simulacao_VeiculoEmManutencao_Devolve404ProblemJson()
        {
            Veiculo veiculo = Semear(10000m, StatusVeiculo.MANUTENCAO);

            HttpResponseMessage resposta = await _cliente.GetAsync(RotaSimulacao(veiculo.IdVeiculo, "0", 12));

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Proposta_SemToken_Devolve401()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(RequisicaoProposta(null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task Proposta_TokenDeFuncionario_Devolve403()
        {
            string token = GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE);

            HttpResponseMessage resposta = await _cliente.SendAsync(RequisicaoProposta(token));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        private static string RotaSimulacao(int idVeiculo, string valorEntrada, int numeroParcelas)
        {
            return $"/api/veiculos/{idVeiculo}/simulacao?valorEntrada={valorEntrada}&numeroParcelas={numeroParcelas}";
        }

        private static HttpRequestMessage RequisicaoProposta(string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaPropostas)
            {
                Content = new StringContent("{\"idVeiculo\":1,\"valorEntrada\":0,\"numeroParcelas\":12}", Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
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
            string token = tokens.GerarToken(perfil, 1, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, 1).Token;

            return token;
        }

        private Veiculo Semear(decimal? valorVenda, StatusVeiculo status)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = _fabrica.CategoriaSuv.IdCategoria,
                Categoria = _fabrica.CategoriaSuv,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Jeep",
                Modelo = "Renegade",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 20000,
                ValorDiaria = 150m,
                ValorVenda = valorVenda,
                Status = status
            };

            _fabrica.Veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }
}
