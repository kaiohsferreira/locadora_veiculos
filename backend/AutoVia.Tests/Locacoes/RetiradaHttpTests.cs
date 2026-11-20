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

namespace AutoVia.Tests.Locacoes
{
    public class RetiradaHttpTests
    {
        private const string Rota = "/api/locacoes/retirada/10";
        private const string Cnh = "12345678901";
        private const string NomeCliente = "Maria da Silva";

        private static string Token(RetiradaWebFactory fabrica, PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO, int idEmpresa = 1)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
                ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
            };
            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            // Cargo no token propositalmente diferente do cargo atual conferido na sessao.
            return tokens.GerarToken(perfil, 20, NomeCliente, perfil == PerfilUsuario.FUNCIONARIO ? CargoFuncionario.ADMINISTRADOR : null,
                "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? idEmpresa : null).Token;
        }

        private static async Task<HttpResponseMessage> Enviar(HttpClient cliente, string rota, string? token, string? corpo = null)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(corpo == null ? HttpMethod.Get : HttpMethod.Post, rota);
            if (token != null)
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }
            if (corpo != null)
            {
                requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
            }
            return await cliente.SendAsync(requisicao);
        }

        private static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        {
            string json = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(json);
            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task Consulta200FlatFormatadaSemCacheSemMutacaoNemSegredos()
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, Rota, Token(fabrica));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.True(resposta.Headers.CacheControl?.NoStore);
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(10, dados.GetProperty("idReserva").GetInt32());
            Assert.Equal(NomeCliente, dados.GetProperty("clienteNome").GetString());
            Assert.Equal("123.***.***-09", dados.GetProperty("clienteCpfMascarado").GetString());
            Assert.Equal("18/11/2026 09:30", dados.GetProperty("dataRetiradaPrevistaStr").GetString());
            Assert.Equal("19/11/2026 09:30", dados.GetProperty("dataDevolucaoPrevistaStr").GetString());
            Assert.Equal("01/01/2030", dados.GetProperty("cnhValidadeStr").GetString());
            Assert.Equal(StaticMethods.FormateToMonetary(200m), dados.GetProperty("valorEstimadoStr").GetString());
            Assert.Equal(StaticMethods.FormateToMonetary(200m), dados.GetProperty("valorDiariaStr").GetString());
            Assert.Equal("ABC1D23", dados.GetProperty("placa").GetString());
            Assert.Equal("Fiat", dados.GetProperty("marca").GetString());
            Assert.Equal("Toro", dados.GetProperty("modelo").GetString());
            Assert.Equal(1500, dados.GetProperty("kmAtual").GetInt32());
            Assert.Equal("PENDENTE", dados.GetProperty("statusCadastroLocadora").GetString());
            Assert.Equal(JsonValueKind.Null, dados.GetProperty("idLocacaoExistente").ValueKind);
            Assert.False(dados.TryGetProperty("senhaHash", out JsonElement _));
            Assert.False(dados.TryGetProperty("cliente", out JsonElement _));
            Assert.Equal(0, fabrica.Locacoes.ChamadasStartFromReserva);
            Assert.Equal(StatusReserva.CONFIRMADA, fabrica.Locacoes.ObterReserva(10)!.Status);
            Assert.Equal(StatusVeiculo.RESERVADO, fabrica.Locacoes.ObterVeiculo(10)!.Status);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE, Cnh)]
        [InlineData(CargoFuncionario.ADMINISTRADOR, Cnh)]
        [InlineData(CargoFuncionario.VENDEDOR, "********901")]
        [InlineData(CargoFuncionario.GESTOR_FROTA, "********901")]
        [InlineData(CargoFuncionario.FINANCEIRO, "********901")]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, "********901")]
        public async Task CnhSegueCargoAtualDoServidorIgnorandoCargoEmpresaForjadosNoToken(CargoFuncionario cargo, string esperada)
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, Rota, Token(fabrica));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(esperada, dados.GetProperty("cnhNumero").GetString());
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task CpfSempreMascaradoParaQualquerCargo(CargoFuncionario cargo)
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            fabrica.Sessao.Situacao.Cargo = cargo;
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, Rota, Token(fabrica));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            string corpo = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(corpo);
            Assert.Equal("123.***.***-09", documento.RootElement.GetProperty("clienteCpfMascarado").GetString());
            Assert.False(documento.RootElement.TryGetProperty("clienteCpfStr", out JsonElement _));
            Assert.DoesNotContain("12345678909", corpo);
            Assert.DoesNotContain("123.456.789-09", corpo);
        }

        [Fact]
        public async Task SemToken401Cliente403()
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage anonimo = await Enviar(cliente, Rota, null);
            using HttpResponseMessage externo = await Enviar(cliente, Rota, Token(fabrica, PerfilUsuario.CLIENTE));
            Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, externo.StatusCode);
        }

        [Theory]
        [InlineData("ausente")]
        [InlineData("empresa")]
        [InlineData("reserva")]
        [InlineData("cliente")]
        [InlineData("veiculo")]
        public async Task AusenteOuForaDoEscopo404(string caso)
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory(reserva =>
            {
                if (caso == "empresa") reserva.Veiculo.IdEmpresa = 2;
                if (caso == "reserva") reserva.Disable();
                if (caso == "cliente") reserva.Cliente.Disable();
                if (caso == "veiculo") reserva.Veiculo.Disable();
            });
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, caso == "ausente" ? "/api/locacoes/retirada/999" : Rota, Token(fabrica));
            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task SemVinculoLocalStatusNuloMesmoClienteGlobalAprovado()
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory(reserva => reserva.Cliente.Locadoras.Clear());
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, Rota, Token(fabrica));
            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement dados = await LerJson(resposta);
            Assert.Equal(JsonValueKind.Null, dados.GetProperty("statusCadastroLocadora").ValueKind);
        }

        [Fact]
        public async Task PostExistenteUsaKmDoServidorEConsultaRecuperaLocacaoComReservaConcluida()
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            string token = Token(fabrica);
            using HttpResponseMessage criada = await Enviar(cliente, "/api/locacoes", token, "{\"idReserva\":10,\"kmSaida\":1}");
            Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
            JsonElement locacao = await LerJson(criada);
            Assert.Equal(1500, locacao.GetProperty("kmSaida").GetInt32());
            using HttpResponseMessage consulta = await Enviar(cliente, Rota, token);
            Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
            JsonElement dados = await LerJson(consulta);
            Assert.Equal("CONCLUIDA", dados.GetProperty("statusReserva").GetString());
            Assert.Equal(locacao.GetProperty("idLocacao").GetInt32(), dados.GetProperty("idLocacaoExistente").GetInt32());
            using HttpResponseMessage duplicada = await Enviar(cliente, "/api/locacoes", token, "{\"idReserva\":10}");
            Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
            Assert.Equal(1, fabrica.Locacoes.ChamadasStartFromReserva);
        }

        [Fact]
        public async Task EmpresaDoTokenDiferenteDaSessaoRetorna401()
        {
            using RetiradaWebFactory fabrica = new RetiradaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();
            using HttpResponseMessage resposta = await Enviar(cliente, Rota, Token(fabrica, PerfilUsuario.FUNCIONARIO, 999));
            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }
        private sealed class RetiradaWebFactory : VeiculoWebFactory
        {
            public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();
            public FakeSessaoService Sessao { get; } = new FakeSessaoService();

            public RetiradaWebFactory(Action<Reserva>? configurar = null)
            {
                Cliente cliente = new Cliente
                {
                    IdCliente = 30, Nome = NomeCliente, Cpf = "12345678909", CnhNumero = Cnh,
                    CnhCategoria = CategoriaCnh.B, CnhValidade = new DateOnly(2030, 1, 1),
                    StatusCadastro = StatusCadastroCliente.APROVADO, SenhaHash = "segredo-interno"
                };
                // Menor id ativo DO PAR prevalece; desativado, outra empresa e outro cliente nao podem interferir.
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 1, IdCliente = 30, IdEmpresa = 2, Status = StatusCadastroCliente.RECUSADO });
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 2, IdCliente = 30, IdEmpresa = 1, Status = StatusCadastroCliente.RECUSADO, DisabledAt = DateTime.Now });
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 3, IdCliente = 31, IdEmpresa = 1, Status = StatusCadastroCliente.RECUSADO });
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 5, IdCliente = 30, IdEmpresa = 1, Status = StatusCadastroCliente.APROVADO });
                cliente.Locadoras.Add(new AutoVia.Core.Entities.PgSql.ClienteLocadora { IdClienteLocadora = 4, IdCliente = 30, IdEmpresa = 1, Status = StatusCadastroCliente.PENDENTE });
                Reserva reserva = new Reserva
                {
                    IdReserva = 10, IdCliente = 30, Cliente = cliente, IdVeiculo = 40,
                    DataRetiradaPrevista = new DateTime(2026, 11, 18, 9, 30, 0),
                    DataDevolucaoPrevista = new DateTime(2026, 11, 19, 9, 30, 0),
                    ValorEstimado = 200m, Status = StatusReserva.CONFIRMADA,
                    Veiculo = new Veiculo { IdVeiculo = 40, IdEmpresa = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO }
                };
                configurar?.Invoke(reserva);
                Locacoes.Adicionar(reserva);
                Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ILocacaoRepository>();
                    services.RemoveAll<ISessaoService>();
                    services.AddSingleton<ILocacaoRepository>(Locacoes);
                    services.AddSingleton<ISessaoService>(Sessao);
                });
            }
        }
    }
}