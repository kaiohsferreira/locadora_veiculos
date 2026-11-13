using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using System.Net;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP de GET /api/veiculos/a-venda com o servidor em memoria (sem PostgreSQL).
    /// Confirmam que a rota e publica mesmo com a politica Funcionario na classe do
    /// VeiculoController (sem token: 200), que so aparecem veiculos DISPONIVEL com valor de
    /// venda, que a resposta nao expoe placa, chassi nem renavam e que filtro invalido da 400.
    /// Reaproveita o VeiculoWebFactory (fakes de veiculo e categoria, categoria SUV semeada).
    /// </summary>
    public class CatalogoHttpTests : IDisposable
    {
        private const string RotaCatalogo = "/api/veiculos/a-venda";
        private const string MediaProblema = "application/problem+json";

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public CatalogoHttpTests()
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
        public async Task SemLogin_Devolve200SoComVeiculosAVenda()
        {
            Veiculo aVenda = Semear("Jeep", 89900m, StatusVeiculo.DISPONIVEL);
            Semear("Fiat", 70000m, StatusVeiculo.MANUTENCAO);
            Semear("VW", null, StatusVeiculo.DISPONIVEL);

            HttpResponseMessage resposta = await _cliente.GetAsync(RotaCatalogo);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(1, corpo.GetProperty("total").GetInt32());
            JsonElement item = corpo.GetProperty("itens")[0];
            Assert.Equal(aVenda.IdVeiculo, item.GetProperty("idVeiculo").GetInt32());
            Assert.Equal("Jeep", item.GetProperty("marca").GetString());
            Assert.Equal(89900m, item.GetProperty("valorVenda").GetDecimal());
            Assert.False(item.TryGetProperty("placa", out JsonElement _));
            Assert.False(item.TryGetProperty("chassi", out JsonElement _));
            Assert.False(item.TryGetProperty("renavam", out JsonElement _));
        }

        [Fact]
        public async Task SemLogin_FiltroDePrecoInvertido_Devolve400ProblemJson()
        {
            HttpResponseMessage resposta = await _cliente.GetAsync($"{RotaCatalogo}?precoMinimo=90000&precoMaximo=50000");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroCatalogoFaixaPreco, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task ListagemDaFrota_ContinuaExigindoLogin()
        {
            HttpResponseMessage resposta = await _cliente.GetAsync("/api/veiculos");

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        private Veiculo Semear(string marca, decimal? valorVenda, StatusVeiculo status)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = _fabrica.CategoriaSuv.IdCategoria,
                Categoria = _fabrica.CategoriaSuv,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = marca,
                Modelo = "Modelo",
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
