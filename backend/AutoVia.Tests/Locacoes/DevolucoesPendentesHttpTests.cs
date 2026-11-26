using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// GET /api/locacoes (devolucoes pendentes) pela pipeline HTTP, com o repositorio em memoria: politica de funcionario (401 sem
    /// token, 403 de cliente), status invalido (400), filtro de atrasadas, paginacao, codigo da reserva nulo, cache sem
    /// armazenamento, nenhum campo proibido no JSON e log da falha (500). O isolamento por locadora fica em LocacaoConsultasSqlTests.
    /// </summary>
    public class DevolucoesPendentesHttpTests
    {
        private const string Rota = "/api/locacoes";
        private const string NomeFuncionario = "Atendente de teste";
        private const string CpfDoCliente = "12345678909";
        private const string NomeDoCliente = "Maria da Silva";
        private const string PlacaDoVeiculo = "ABC1D23";
        private const string ChaveItens = "itens";
        private const string ChaveIdLocacao = "idLocacao";
        private const string ChaveCodigoReserva = "codigoReserva";
        private const string ChaveAtrasada = "atrasada";
        private const string TextoFalhaDeTeste = "falha de teste";
        private static readonly string[] CamposProibidos = { "cpf", "chassi", "renavam", "valorcaucao", "idtransacao", "cnh", "email", "telefone", "senha" };

        private static string Token(DevolucoesWebFactory fabrica, PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO, int idEmpresa = 1, CargoFuncionario cargo = CargoFuncionario.ATENDENTE)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            return tokens.GerarToken(perfil, 20, NomeFuncionario, perfil == PerfilUsuario.FUNCIONARIO ? cargo : null,
                "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? idEmpresa : null).Token;
        }

        private static async Task<HttpResponseMessage> Consultar(HttpClient cliente, string rota, string? token)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, rota);
            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        {
            string json = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(json);
            return documento.RootElement.Clone();
        }

        private static List<int> IdsDosItens(JsonElement corpo)
        {
            return corpo.GetProperty(ChaveItens).EnumerateArray().Select(item => item.GetProperty(ChaveIdLocacao).GetInt32()).ToList();
        }

        [Fact]
        public async Task SemToken_Devolve401()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, null);

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task TokenDeCliente_Devolve403()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, Token(fabrica, PerfilUsuario.CLIENTE));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        [Theory]
        [InlineData("FINALIZADA")]
        [InlineData("em_andamento")]
        [InlineData("texto livre")]
        public async Task StatusForaDaLista_Devolve400(string status)
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, $"{Rota}?status={Uri.EscapeDataString(status)}", Token(fabrica));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task Funcionario_ListaAsEmAndamentoEAtrasadas_SemCamposProibidosESemCache()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, Token(fabrica));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.True(resposta.Headers.CacheControl?.NoStore);
            string textoBruto = await resposta.Content.ReadAsStringAsync();
            JsonElement corpo = JsonDocument.Parse(textoBruto).RootElement.Clone();
            Assert.Equal(3, corpo.GetProperty("total").GetInt32());
            Assert.Equal(new List<int> { 3, 1, 2 }, IdsDosItens(corpo));
            string minusculo = textoBruto.ToLowerInvariant();
            foreach (string campo in CamposProibidos)
            {
                Assert.DoesNotContain($"\"{campo}\"", minusculo);
            }
            Assert.DoesNotContain(CpfDoCliente, textoBruto);
        }

        [Fact]
        public async Task StatusAtrasada_DevolveSoAsAtrasadas()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, $"{Rota}?status=ATRASADA", Token(fabrica));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJson(resposta);
            Assert.Equal(2, corpo.GetProperty("total").GetInt32());
            Assert.Equal(new List<int> { 3, 1 }, IdsDosItens(corpo));
            Assert.All(corpo.GetProperty(ChaveItens).EnumerateArray(), item => Assert.True(item.GetProperty(ChaveAtrasada).GetBoolean()));
        }

        [Fact]
        public async Task Paginacao_DevolveAPaginaPedida()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, $"{Rota}?pagina=2&tamanhoPagina=2", Token(fabrica));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJson(resposta);
            Assert.Equal(3, corpo.GetProperty("total").GetInt32());
            Assert.Equal(2, corpo.GetProperty("pagina").GetInt32());
            Assert.Equal(2, corpo.GetProperty("tamanhoPagina").GetInt32());
            Assert.Equal(new List<int> { 2 }, IdsDosItens(corpo));
        }

        [Fact]
        public async Task LocacaoSemReserva_CodigoReservaNulo_ComReservaTrazOCodigo()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, Token(fabrica));

            JsonElement corpo = await LerJson(resposta);
            Dictionary<int, JsonElement> itens = corpo.GetProperty(ChaveItens).EnumerateArray()
                .ToDictionary(item => item.GetProperty(ChaveIdLocacao).GetInt32());
            Assert.Equal(JsonValueKind.Null, itens[1].GetProperty(ChaveCodigoReserva).ValueKind);
            Assert.Equal(RegrasReserva.FormateCodigoReserva(5), itens[2].GetProperty(ChaveCodigoReserva).GetString());
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task QualquerCargoDeFuncionario_VeAListaDaLocadora(CargoFuncionario cargo)
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            fabrica.Sessao.Situacao = new SessaoSituacaoVO
            {
                Ativo = true, IdEmpresa = 1, Cargo = cargo, EmpresaAtiva = true, EmpresaOperadora = false
            };

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, Token(fabrica, cargo: cargo));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task FalhaNaLeitura_Devolve500ERegistraOLogSemDadosDaLocacao()
        {
            using DevolucoesWebFactory fabrica = new DevolucoesWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            fabrica.Locacoes.FalhaNaListagemDevolucoes = new InvalidOperationException(TextoFalhaDeTeste);

            using HttpResponseMessage resposta = await Consultar(cliente, Rota, Token(fabrica));

            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            List<string> entradas = fabrica.Log.Snapshot();
            Assert.Contains(entradas, entrada => entrada.StartsWith(ConstantsMessage.ErroListarDevolucoesPendentes, StringComparison.Ordinal));
            foreach (string entrada in entradas)
            {
                Assert.DoesNotContain(PlacaDoVeiculo, entrada);
                Assert.DoesNotContain(CpfDoCliente, entrada);
                Assert.DoesNotContain(NomeDoCliente, entrada);
            }
        }

        /// <summary>Fabrica HTTP com o repositorio de locacao em memoria (sem banco), log em memoria e sessao de teste.</summary>
        private sealed class DevolucoesWebFactory : VeiculoWebFactory
        {
            public DevolucoesWebFactory()
            {
                DateTime agora = DateTime.Now;
                Locacoes.Adicionar(CriarLocacao(1, StatusLocacao.EM_ANDAMENTO, agora.AddDays(-6), agora.AddDays(-2), null));
                Locacoes.Adicionar(CriarLocacao(2, StatusLocacao.EM_ANDAMENTO, agora.AddDays(-1), agora.AddDays(3), 5));
                Locacoes.Adicionar(CriarLocacao(3, StatusLocacao.ATRASADA, agora.AddDays(-10), agora.AddDays(-8), null));
                Locacoes.Adicionar(CriarLocacao(4, StatusLocacao.FINALIZADA, agora.AddDays(-20), agora.AddDays(-15), null));
            }

            public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();

            public FakeLogService Log { get; } = new FakeLogService();

            private static Locacao CriarLocacao(int idLocacao, StatusLocacao status, DateTime retirada, DateTime previsao, int? idReserva)
            {
                return new Locacao
                {
                    IdLocacao = idLocacao,
                    IdReserva = idReserva,
                    IdCliente = 30,
                    IdVeiculo = 40,
                    IdFuncionario = 20,
                    DataRetirada = retirada,
                    DataDevolucaoPrevista = previsao,
                    KmSaida = 1000,
                    ValorDiaria = 200m,
                    Status = status,
                    Cliente = new Cliente { IdCliente = 30, Nome = NomeDoCliente, Cpf = CpfDoCliente },
                    Veiculo = new Veiculo { IdVeiculo = 40, IdEmpresa = 1, Placa = PlacaDoVeiculo, Marca = "Fiat", Modelo = "Toro", KmAtual = 1500, ValorDiaria = 200m, AnoModelo = 2024, IdCategoria = 3 }
                };
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ILocacaoRepository>();
                    services.AddSingleton<ILocacaoRepository>(Locacoes);
                    services.RemoveAll<ILogService>();
                    services.AddSingleton<ILogService>(Log);
                });
            }
        }
    }
}
