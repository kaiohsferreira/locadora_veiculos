using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// GET /api/veiculos por HTTP com paginacao (item 2 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026):
    /// o JSON devolve pagina e tamanhoPagina iguais aos pedidos, junto com total e itens da pagina certa. Servidor em
    /// memoria (VeiculoWebFactory, com o repositorio de veiculos trocado por fake); nada chega ao PostgreSQL.
    /// </summary>
    public class VeiculoListagemPaginaHttpTests : IDisposable
    {
        private const string RotaVeiculos = "/api/veiculos";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string PropriedadePagina = "pagina";
        private const string PropriedadeTamanhoPagina = "tamanhoPagina";
        private const string PropriedadeTotal = "total";
        private const string PropriedadeItens = "itens";
        private const string PropriedadePlaca = "placa";
        private const string PlacaDaPrimeira = "AAA1A11";
        private const string PlacaDaSegunda = "BBB2B22";
        private const string PlacaDaTerceira = "CCC3C33";
        private const int PaginaDoTeste = 2;
        private const int TamanhoDaPaginaDoTeste = 2;
        private const int TotalDeVeiculosSemeados = 3;
        private const int QuantidadeNaUltimaPagina = 1;

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public VeiculoListagemPaginaHttpTests()
        {
            _fabrica = new VeiculoWebFactory();
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        /// <summary>Segunda pagina de 2 itens com 3 veiculos: pagina 2, tamanhoPagina 2, total 3 e so o terceiro veiculo.</summary>
        [Fact]
        public async Task SegundaPagina_DevolvePaginaETamanhoPaginaPedidosEOTerceiroVeiculo()
        {
            SemearTresVeiculos();

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao($"{RotaVeiculos}?pagina={PaginaDoTeste}&tamanhoPagina={TamanhoDaPaginaDoTeste}"));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(PaginaDoTeste, corpo.GetProperty(PropriedadePagina).GetInt32());
            Assert.Equal(TamanhoDaPaginaDoTeste, corpo.GetProperty(PropriedadeTamanhoPagina).GetInt32());
            Assert.Equal(TotalDeVeiculosSemeados, corpo.GetProperty(PropriedadeTotal).GetInt32());
            JsonElement itens = corpo.GetProperty(PropriedadeItens);
            Assert.Equal(QuantidadeNaUltimaPagina, itens.GetArrayLength());
            Assert.Equal(PlacaDaTerceira, itens[0].GetProperty(PropriedadePlaca).GetString());
        }

        /// <summary>Sem parametros, a primeira pagina sai com os valores padrao (pagina 1 e tamanho 20) e os tres veiculos.</summary>
        [Fact]
        public async Task SemParametros_DevolvePaginaUmComTamanhoPadraoETodosOsVeiculos()
        {
            SemearTresVeiculos();

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaVeiculos));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsValues.PaginaPadrao, corpo.GetProperty(PropriedadePagina).GetInt32());
            Assert.Equal(ConstantsValues.TamanhoPaginaPadrao, corpo.GetProperty(PropriedadeTamanhoPagina).GetInt32());
            Assert.Equal(TotalDeVeiculosSemeados, corpo.GetProperty(PropriedadeTotal).GetInt32());
            List<string?> placas = corpo.GetProperty(PropriedadeItens).EnumerateArray().Select(item => item.GetProperty(PropriedadePlaca).GetString()).ToList();
            Assert.Equal(new List<string?> { PlacaDaPrimeira, PlacaDaSegunda, PlacaDaTerceira }, placas);
        }

        private void SemearTresVeiculos()
        {
            SemearVeiculo(PlacaDaPrimeira, "9BWZZZ377VT004251", "12345678901");
            SemearVeiculo(PlacaDaSegunda, "9BWZZZ377VT004252", "12345678902");
            SemearVeiculo(PlacaDaTerceira, "9BWZZZ377VT004253", "12345678903");
        }

        private void SemearVeiculo(string placa, string chassi, string renavam)
        {
            Veiculo veiculo = new Veiculo
            {
                IdCategoria = _fabrica.CategoriaSuv.IdCategoria,
                Categoria = _fabrica.CategoriaSuv,
                Placa = placa,
                Chassi = chassi,
                Renavam = renavam,
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                KmAtual = 1000,
                ValorDiaria = 150m,
                Status = StatusVeiculo.DISPONIVEL
            };

            _fabrica.Veiculos.Adicionar(veiculo);
        }

        private HttpRequestMessage Requisicao(string rota)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, rota);
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, TokenDeFuncionario());

            return requisicao;
        }

        private string TokenDeFuncionario()
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor,
                Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, 1, "Usuario de teste", CargoFuncionario.ATENDENTE, VersaoSessaoDeTeste, false, 1).Token;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }
}
