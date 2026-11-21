using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
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
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes HTTP do painel financeiro (P7-BE-05, RF-35) com o servidor em memoria, o repositorio trocado por
    /// FakePainelFinanceiroRepository, a sessao por FakeSessaoService e o relogio fixo em 21/11/2026. Cobre 401 sem token;
    /// 403 para cliente e para cargos fora de FINANCEIRO e ADMINISTRADOR; 200 para os dois cargos permitidos; 400 para periodo
    /// incompleto, invertido e aba invalida. Nenhum PostgreSQL e acessado.
    /// </summary>
    public class PainelFinanceiroHttpTests : IDisposable
    {
        private const string RotaIndicadores = "/api/painel/financeiro";
        private const string RotaTitulos = "/api/painel/financeiro/titulos";
        private const string PeriodoCompleto = "?de=2026-11-01&ate=2026-11-30";
        private const string PeriodoSoDe = "?de=2026-11-01";
        private const string PeriodoInvertido = "?de=2026-11-30&ate=2026-11-01";
        private const string AbaInvalida = "?aba=CANCELADO";
        private const string MediaProblema = "application/problem+json";
        private const string VersaoSessaoDeTeste = "versao-de-teste";
        private const string NomeDetail = "detail";
        private const string NomeRecebidoStr = "recebidoStr";
        private const string NomePagina = "pagina";
        private const int IdFuncionarioDoTeste = 9;
        private const int IdClienteDoTeste = 7;
        private const int EmpresaDoTeste = 1;

        private readonly PainelFinanceiroWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public PainelFinanceiroHttpTests()
        {
            _fabrica = new PainelFinanceiroWebFactory();
            DefinirCargo(CargoFuncionario.FINANCEIRO);
            _cliente = _fabrica.CreateClient();
        }

        public void Dispose()
        {
            _cliente.Dispose();
            _fabrica.Dispose();
        }

        private void DefinirCargo(CargoFuncionario cargo)
        {
            _fabrica.Sessao.Situacao = new SessaoSituacaoVO
            {
                Ativo = true,
                IdEmpresa = EmpresaDoTeste,
                Cargo = cargo,
                EmpresaAtiva = true,
                EmpresaOperadora = false
            };
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo, int idUsuario)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = PainelFinanceiroWebFactory.Emissor,
                Audiencia = PainelFinanceiroWebFactory.Audiencia,
                ExpiracaoMinutos = 120,
                Chave = _fabrica.ChaveJwt
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());

            return tokens.GerarToken(perfil, idUsuario, "Usuario de teste", cargo, VersaoSessaoDeTeste, false, EmpresaDoTeste).Token;
        }

        private string TokenDoCargo(CargoFuncionario cargo)
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, cargo, IdFuncionarioDoTeste);
        }

        private string TokenDoCliente()
        {
            return GerarToken(PerfilUsuario.CLIENTE, null, IdClienteDoTeste);
        }

        private static HttpRequestMessage Requisicao(string rota, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task SemToken_NasDuasRotas_Devolve401ProblemJson()
        {
            HttpResponseMessage indicadores = await _cliente.SendAsync(Requisicao(RotaIndicadores, null));
            HttpResponseMessage titulos = await _cliente.SendAsync(Requisicao(RotaTitulos, null));

            Assert.Equal(HttpStatusCode.Unauthorized, indicadores.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, titulos.StatusCode);
            Assert.Equal(MediaProblema, indicadores.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task ClienteNasDuasRotas_Devolve403Problem()
        {
            HttpResponseMessage indicadores = await _cliente.SendAsync(Requisicao(RotaIndicadores, TokenDoCliente()));
            HttpResponseMessage titulos = await _cliente.SendAsync(Requisicao(RotaTitulos, TokenDoCliente()));

            Assert.Equal(HttpStatusCode.Forbidden, indicadores.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, titulos.StatusCode);
            Assert.Equal(MediaProblema, indicadores.Content.Headers.ContentType?.MediaType);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        public async Task CargoSemPermissao_Devolve403NasDuasRotas(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);

            HttpResponseMessage indicadores = await _cliente.SendAsync(Requisicao(RotaIndicadores + PeriodoCompleto, TokenDoCargo(cargo)));
            HttpResponseMessage titulos = await _cliente.SendAsync(Requisicao(RotaTitulos, TokenDoCargo(cargo)));

            Assert.Equal(HttpStatusCode.Forbidden, indicadores.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, titulos.StatusCode);
        }

        [Theory]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task CargoPermitido_Devolve200ComOsIndicadores(CargoFuncionario cargo)
        {
            DefinirCargo(cargo);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaIndicadores + PeriodoCompleto, TokenDoCargo(cargo)));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.TryGetProperty(NomeRecebidoStr, out JsonElement recebido));
            Assert.False(string.IsNullOrEmpty(recebido.GetString()));
        }

        [Fact]
        public async Task CargoPermitido_Devolve200NaListaDeTitulosComPagina()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaTitulos + PeriodoCompleto, TokenDoCargo(CargoFuncionario.FINANCEIRO)));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.TryGetProperty(NomePagina, out JsonElement pagina));
            Assert.True(pagina.TryGetProperty("total", out JsonElement total));
            Assert.Equal(JsonValueKind.Number, total.ValueKind);
        }

        [Fact]
        public async Task PeriodoSoComDe_Devolve400ComAMensagemDoPeriodo()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaIndicadores + PeriodoSoDe, TokenDoCargo(CargoFuncionario.FINANCEIRO)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIncompleto, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task PeriodoInvertido_Devolve400ComAMensagemDeIntervalo()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaIndicadores + PeriodoInvertido, TokenDoCargo(CargoFuncionario.FINANCEIRO)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIntervalo, corpo.GetProperty(NomeDetail).GetString());
        }

        [Fact]
        public async Task AbaInvalida_Devolve400ComAMensagemDeAba()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(RotaTitulos + AbaInvalida, TokenDoCargo(CargoFuncionario.FINANCEIRO)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroAbaTitulosFinanceirosInvalida, corpo.GetProperty(NomeDetail).GetString());
        }
    }

    /// <summary>
    /// Servidor de teste do painel financeiro: configuracao em memoria (chave JWT gerada a cada teste), repositorio trocado
    /// por FakePainelFinanceiroRepository (locadora 1, com uma cobranca), sessao por FakeSessaoService e relogio fixo em
    /// 21/11/2026. Nao acessa o PostgreSQL.
    /// </summary>
    public class PainelFinanceiroWebFactory : WebApplicationFactory<Program>
    {
        public const string Emissor = "AutoVia.Api";
        public const string Audiencia = "AutoVia.Web";

        public PainelFinanceiroWebFactory()
        {
            ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Log = new FakeLogService();
            Sessao = new FakeSessaoService();
        }

        public FakeLogService Log { get; }

        public string ChaveJwt { get; }

        public FakeSessaoService Sessao { get; }

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
                    { "Autenticacao:Recuperacao:UrlPublicaFront", "https://front.exemplo.com" },
                    { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                };

                configuracao.AddInMemoryCollection(valores);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPainelFinanceiroRepository>();
                services.AddSingleton<IPainelFinanceiroRepository>(CriarRepositorioFalso());
                services.RemoveAll<IRelogio>();
                services.AddSingleton<IRelogio>(new FakeRelogio(new DateTime(2026, 11, 21, 10, 0, 0)));
                services.RemoveAll<ISessaoService>();
                services.AddSingleton<ISessaoService>(Sessao);
                services.RemoveAll<ILogService>();
                services.AddSingleton<ILogService>(Log);
            });
        }

        private static FakePainelFinanceiroRepository CriarRepositorioFalso()
        {
            Dictionary<int, List<Pagamento>> dados = new Dictionary<int, List<Pagamento>>
            {
                [EmpresaDoTeste] = new List<Pagamento>
                {
                    FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 100m, new DateOnly(2026, 11, 25), null)
                }
            };

            return new FakePainelFinanceiroRepository(ContextoEmpresaDeTeste.Criar(EmpresaDoTeste), dados);
        }

        private const int EmpresaDoTeste = 1;
    }
}
