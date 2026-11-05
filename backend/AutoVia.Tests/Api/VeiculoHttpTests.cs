using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP de /api/veiculos com o servidor em memoria (WebApplicationFactory sobre
    /// Program). Cada teste sobe o proprio servidor (o xUnit cria uma instancia da classe por
    /// teste), com configuracao em memoria, chave JWT gerada no teste e os repositorios de
    /// veiculo e de categoria trocados por fakes semeados. Nenhum PostgreSQL e acessado.
    /// Cobre 401 e 403 por perfil, 201 com Location e status DISPONIVEL, 400 do validador,
    /// 409 de duplicidade, 200, 204 e a listagem paginada com filtros.
    /// </summary>
    public class VeiculoHttpTests : IDisposable
    {
        private const string RotaVeiculos = "/api/veiculos";
        private const string MediaProblema = "application/problem+json";

        private readonly VeiculoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public VeiculoHttpTests()
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
        public async Task SemToken_Devolve401ProblemJson()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaVeiculos, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty("title").GetString());
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403Problem()
        {
            string tokenCliente = GerarToken(PerfilUsuario.CLIENTE, null);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaVeiculos, tokenCliente));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Post_Valido_Devolve201ComLocationEStatusDisponivel()
        {
            VeiculoVO vo = CriarVOValido(_fabrica.CategoriaSuv.IdCategoria);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaVeiculos, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            int idVeiculo = corpo.GetProperty("idVeiculo").GetInt32();
            Assert.True(idVeiculo > 0);
            Assert.Equal("DISPONIVEL", corpo.GetProperty("status").GetString());
            Assert.Equal($"{RotaVeiculos}/{idVeiculo}", resposta.Headers.Location?.AbsolutePath);
        }

        [Fact]
        public async Task Post_PlacaVazia_Devolve400ComMensagemDoValidador()
        {
            VeiculoVO vo = CriarVOValido(_fabrica.CategoriaSuv.IdCategoria);
            vo.Placa = string.Empty;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaVeiculos, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaInvalida, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Post_AnoModeloAnteriorAoDeFabricacao_Devolve400NoCampoAnoModelo()
        {
            VeiculoVO vo = CriarVOValido(_fabrica.CategoriaSuv.IdCategoria);
            vo.AnoFabricacao = 2022;
            vo.AnoModelo = 2020;

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaVeiculos, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoAnoModeloAnterior, corpo.GetProperty("detail").GetString());
            Assert.Equal(ConstantsMessage.ErroVeiculoAnoModeloAnterior, corpo.GetProperty("errors").GetProperty("anoModelo")[0].GetString());
        }

        [Fact]
        public async Task Post_PlacaDeVeiculoAtivo_Devolve409ProblemJson()
        {
            SemearVeiculo("ABC1D23", "1HGCM82633A004352", "98765432100", StatusVeiculo.LOCADO);
            VeiculoVO vo = CriarVOValido(_fabrica.CategoriaSuv.IdCategoria);
            vo.Chassi = "9BWZZZ377VT004251";
            vo.Renavam = "12345678901";

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Post, RotaVeiculos, TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroVeiculoPlacaDuplicada, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task GetPorId_VeiculoAtivo_Devolve200()
        {
            Veiculo veiculo = SemearVeiculo("ABC1D23", "9BWZZZ377VT004251", "12345678901", StatusVeiculo.DISPONIVEL);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}/{veiculo.IdVeiculo}", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal("ABC1D23", corpo.GetProperty("placa").GetString());
            Assert.Equal("SUV", corpo.GetProperty("categoriaNome").GetString());
        }

        [Fact]
        public async Task Put_Valido_Devolve204ESemAlterarOStatus()
        {
            Veiculo veiculo = SemearVeiculo("ABC1D23", "9BWZZZ377VT004251", "12345678901", StatusVeiculo.LOCADO);
            VeiculoVO vo = CriarVOValido(_fabrica.CategoriaSuv.IdCategoria);
            vo.Marca = "Ford";

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Put, $"{RotaVeiculos}/{veiculo.IdVeiculo}", TokenDeFuncionario(), vo));

            Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
            HttpResponseMessage consulta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}/{veiculo.IdVeiculo}", TokenDeFuncionario()));
            JsonElement corpo = await LerJsonAsync(consulta);
            Assert.Equal("Ford", corpo.GetProperty("marca").GetString());
            Assert.Equal("LOCADO", corpo.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetLista_FiltroDeStatusDisponivel_DevolveSoOsAtivosDisponiveisComTotal()
        {
            SemearVeiculo("AAA1A11", "9BWZZZ377VT004251", "12345678901", StatusVeiculo.DISPONIVEL);
            SemearVeiculo("BBB2B22", "9BWZZZ377VT004252", "12345678902", StatusVeiculo.LOCADO);
            SemearVeiculo("CCC3C33", "9BWZZZ377VT004253", "12345678903", StatusVeiculo.DISPONIVEL);
            SemearVeiculo("DDD4D44", "9BWZZZ377VT004254", "12345678904", StatusVeiculo.DISPONIVEL, desativado: true);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}?status=DISPONIVEL", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(2, corpo.GetProperty("total").GetInt32());
            List<string?> placas = new List<string?>();
            List<string?> status = new List<string?>();
            foreach (JsonElement item in corpo.GetProperty("itens").EnumerateArray())
            {
                placas.Add(item.GetProperty("placa").GetString());
                status.Add(item.GetProperty("status").GetString());
            }
            Assert.Equal(new List<string?> { "AAA1A11", "CCC3C33" }, placas);
            Assert.All(status, valor => Assert.Equal("DISPONIVEL", valor));
        }

        [Fact]
        public async Task GetLista_PaginaZero_Devolve400ProblemJson()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}?pagina=0", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPaginaInvalida, corpo.GetProperty("detail").GetString());
        }

        /// <summary>
        /// Status numerico fora do enum e recusado pelo model binding, antes do FluentValidation:
        /// a resposta e o 400 generico (ErroRequisicaoInvalida), com a chave "Status" (PascalCase, vinda
        /// do ModelState) em errors. O contrato previa ErroVeiculoStatusInvalido para esse caso; o teste
        /// registra o comportamento real.
        /// </summary>
        [Fact]
        public async Task GetLista_StatusInvalido_Devolve400ProblemJson()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, $"{RotaVeiculos}?status=99", TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, corpo.GetProperty("detail").GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "Status"), corpo.GetProperty("errors").GetProperty("Status")[0].GetString());
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
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE);
        }

        private Veiculo SemearVeiculo(string placa, string chassi, string renavam, StatusVeiculo status, bool desativado = false)
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
                Status = status
            };

            if (desativado)
            {
                veiculo.Disable();
            }

            _fabrica.Veiculos.Adicionar(veiculo);

            return veiculo;
        }

        private static VeiculoVO CriarVOValido(int idCategoria)
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

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }

    /// <summary>
    /// Servidor de teste dos veiculos: configuracao em memoria (chave JWT gerada a cada teste,
    /// conexao fictícia), repositorios de veiculo e de categoria trocados por fakes e uma
    /// categoria "SUV" semeada. Nao acessa o PostgreSQL.
    /// </summary>
    public class VeiculoWebFactory : WebApplicationFactory<Program>
    {
        public const string Emissor = "AutoVia.Api";
        public const string Audiencia = "AutoVia.Web";

        public VeiculoWebFactory()
        {
            ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Veiculos = new FakeVeiculoRepository();
            Categorias = new FakeCategoriaRepository();
            CategoriaSuv = new Categoria { Nome = "SUV", ValorDiariaBase = 200m };
            Categorias.Adicionar(CategoriaSuv);
        }

        /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada instancia).</summary>
        public string ChaveJwt { get; }

        /// <summary>Repositorio de veiculos em memoria; os testes semeiam os veiculos por aqui.</summary>
        public FakeVeiculoRepository Veiculos { get; }

        /// <summary>Repositorio de categorias em memoria.</summary>
        public FakeCategoriaRepository Categorias { get; }

        /// <summary>Categoria "SUV" ativa, semeada no servidor de teste.</summary>
        public Categoria CategoriaSuv { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((contexto, configuracao) =>
            {
                Dictionary<string, string?> valores = new Dictionary<string, string?>
                {
                    { "Jwt:Emissor", Emissor },
                    { "Jwt:Audiencia", Audiencia },
                    { "Jwt:ExpiracaoMinutos", "120" },
                    { "Jwt:Chave", ChaveJwt },
                    { "Autenticacao:LimitePublicoPorMinuto", "1000" },
                    { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                };

                configuracao.AddInMemoryCollection(valores);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IVeiculoRepository>();
                services.AddSingleton<IVeiculoRepository>(Veiculos);
                services.RemoveAll<ICategoriaRepository>();
                services.AddSingleton<ICategoriaRepository>(Categorias);
            });
        }
    }
}
