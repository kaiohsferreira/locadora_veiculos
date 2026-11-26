using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Api;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Devolucao;

/// <summary>
/// HTTP do encerramento (POST /api/locacoes/{id}/devolucao) e da lista de cobrancas (GET /cobrancas): 200 na primeira chamada e
/// na repeticao, 400 do validador, 401 sem token, 403 para cliente e para cargo fora de ATENDENTE e ADMINISTRADOR, 404 de
/// locacao ausente, 409 sem vistoria e 502 do provedor com o texto aprovado.
/// </summary>
public sealed class DevolucaoHttpTests : IDisposable
{
    private const string Rota = "/api/locacoes/10/devolucao";
    private const string Cobrancas = "/api/locacoes/10/cobrancas";
    private const string CorpoValido = "{\"idConfiguracao\":1,\"versaoEsperada\":1,\"totalConfirmado\":200}";
    private readonly EncerramentoWebFactory _fabrica = new EncerramentoWebFactory();
    private readonly HttpClient _cliente;

    public DevolucaoHttpTests() { _cliente = _fabrica.CreateClient(); }

    public void Dispose() { _cliente.Dispose(); _fabrica.Dispose(); }

    private string Token(PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO, CargoFuncionario? cargo = CargoFuncionario.ATENDENTE)
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = _fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        string token = tokens.GerarToken(perfil, 20, "Teste", perfil == PerfilUsuario.FUNCIONARIO ? cargo : null,
            "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? 1 : null).Token;

        return token;
    }

    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string rota, string? token, string? corpo = null)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(metodo, rota);
        if (token != null) pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        if (corpo != null) pedido.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        HttpResponseMessage resposta = await _cliente.SendAsync(pedido);

        return resposta;
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using JsonDocument doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task AtendentePrimeiraChamadaE200ERepeticaoTambem200SemMexerNaSegunda()
    {
        using HttpResponseMessage primeira = await Enviar(HttpMethod.Post, Rota, Token(), CorpoValido);
        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        JsonElement corpo = await Ler(primeira);
        Assert.Equal("FINALIZADA", corpo.GetProperty("locacao").GetProperty("status").GetString());
        Assert.Equal("DISPONIVEL", corpo.GetProperty("veiculo").GetProperty("status").GetString());
        Assert.Equal(150, corpo.GetProperty("veiculo").GetProperty("kmAtual").GetInt32());
        Assert.False(corpo.ToString().Contains("idTransacao", StringComparison.OrdinalIgnoreCase));
        Assert.False(corpo.ToString().Contains("id_transacao", StringComparison.OrdinalIgnoreCase));
        int pagamentosAposPrimeira = _fabrica.Locacoes.LocacoesGravadas().Single().Pagamentos.Count;
        int chamadasAoProvedorAntes = _fabrica.Gateway.TotalDeChamadas;

        using HttpResponseMessage repeticao = await Enviar(HttpMethod.Post, Rota, Token(), CorpoValido);
        Assert.Equal(HttpStatusCode.OK, repeticao.StatusCode);
        Assert.Equal(1, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(pagamentosAposPrimeira, _fabrica.Locacoes.LocacoesGravadas().Single().Pagamentos.Count);
        Assert.Equal(chamadasAoProvedorAntes, _fabrica.Gateway.TotalDeChamadas);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"idConfiguracao\":0,\"versaoEsperada\":1,\"totalConfirmado\":200}")]
    [InlineData("{\"idConfiguracao\":1,\"versaoEsperada\":1,\"totalConfirmado\":-1}")]
    [InlineData("{\"idConfiguracao\":1,\"versaoEsperada\":1,\"totalConfirmado\":200.001}")]
    public async Task CorpoInvalido400ComTextoAprovadoSemGravar(string corpo)
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, _fabrica.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task CampoAMaisNoCorpoNaoMudaNada()
    {
        const string corpo = "{\"idConfiguracao\":1,\"versaoEsperada\":1,\"totalConfirmado\":200,\"valorCaucao\":1,\"idTransacao\":\"x\"}";

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(200m, (await Ler(resposta)).GetProperty("locacao").GetProperty("valorTotal").GetDecimal());
    }

    [Fact]
    public async Task SemVistoriaDeSaida409ComTextoAprovadoSemGravarNemChamarOProvedor()
    {
        _fabrica.Saidas.Vistorias.Vistorias.Clear();

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), CorpoValido);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroDevolucaoSemVistoriaSaida, (await Ler(resposta)).GetProperty("detail").GetString());
        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, _fabrica.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task LocacaoDeOutraLocadoraOuInexistente404()
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, "/api/locacoes/999/devolucao", Token(), CorpoValido);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroLocacaoNaoEncontrada, (await Ler(resposta)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task FalhaDoProvedorDevolve502ComTextoAprovadoESemGravar()
    {
        _fabrica.Gateway.ExcecaoConsulta = new IntegrationException(ConstantsIntegration.Pagamento, "falha de rede simulada");

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), CorpoValido);

        Assert.Equal(HttpStatusCode.BadGateway, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroDevolucaoProvedorIndisponivel, (await Ler(resposta)).GetProperty("detail").GetString());
        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Fact]
    public async Task SemTokenDevolve401ECliente403()
    {
        using HttpResponseMessage anonima = await Enviar(HttpMethod.Post, Rota, null, CorpoValido);
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);

        using HttpResponseMessage cliente = await Enviar(HttpMethod.Post, Rota, Token(PerfilUsuario.CLIENTE), CorpoValido);
        Assert.Equal(HttpStatusCode.Forbidden, cliente.StatusCode);

        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
    }

    /// <summary>Token de funcionario sem identificacao valida (claim NameIdentifier nao positiva): 401 e nada gravado.</summary>
    [Fact]
    public async Task PostComTokenSemIdentificacaoDeUsuarioDevolve401SemGravar()
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = _fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        string semIdentificacao = tokens.GerarToken(PerfilUsuario.FUNCIONARIO, 0, "Teste", CargoFuncionario.ATENDENTE, "versao-de-teste", false, 1).Token;

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, semIdentificacao, CorpoValido);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
    }

    [Theory]
    [InlineData(CargoFuncionario.VENDEDOR)]
    [InlineData(CargoFuncionario.GESTOR_FROTA)]
    [InlineData(CargoFuncionario.FINANCEIRO)]
    [InlineData(CargoFuncionario.ADMINISTRATIVO)]
    public async Task CargoForaDeAtendenteEAdministrador403SemLerOCorpoNemChamarOProvedor(CargoFuncionario cargo)
    {
        _fabrica.Sessao.Situacao.Cargo = cargo;

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(cargo: cargo), CorpoValido);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal(0, _fabrica.Locacoes.ChamadasEncerrarAtomicamente);
        Assert.Equal(0, _fabrica.Gateway.TotalDeChamadas);
    }

    [Fact]
    public async Task AdministradorPodeEncerrar200()
    {
        _fabrica.Sessao.Situacao.Cargo = CargoFuncionario.ADMINISTRADOR;

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(cargo: CargoFuncionario.ADMINISTRADOR), CorpoValido);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task GetCobrancasSemApuracaoDevolve200ComListaVazia()
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, Cobrancas, Token());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement corpo = await Ler(resposta);
        Assert.Equal(0, corpo.GetProperty("cobrancas").GetArrayLength());
        Assert.False(corpo.ToString().Contains("idTransacao", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCobrancasSemTokenDevolve401ECliente403ELocacaoAusente404()
    {
        using HttpResponseMessage anonima = await Enviar(HttpMethod.Get, Cobrancas, null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);

        using HttpResponseMessage cliente = await Enviar(HttpMethod.Get, Cobrancas, Token(PerfilUsuario.CLIENTE));
        Assert.Equal(HttpStatusCode.Forbidden, cliente.StatusCode);

        using HttpResponseMessage ausente = await Enviar(HttpMethod.Get, "/api/locacoes/999/cobrancas", Token());
        Assert.Equal(HttpStatusCode.NotFound, ausente.StatusCode);
    }

    private sealed class EncerramentoWebFactory : VeiculoWebFactory
    {
        public static readonly DateTime Agora = new DateTime(2026, 11, 26, 10, 0, 0);
        public FakeVistoriaSaidaRepository Saidas { get; } = new FakeVistoriaSaidaRepository();
        public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();
        public FakeCaucaoGateway Gateway { get; } = new FakeCaucaoGateway();

        public EncerramentoWebFactory()
        {
            Veiculo veiculo = new Veiculo { IdVeiculo = 5, IdCategoria = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", ValorDiaria = 100m, KmAtual = 100, Status = StatusVeiculo.LOCADO };
            Locacao locacao = new Locacao
            {
                IdLocacao = 10, KmSaida = 100, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = new DateTime(2026, 11, 18, 10, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 20, 0, 0, 0), ValorDiaria = 100m, ValorCaucao = 0m, Veiculo = veiculo, IdVeiculo = 5
            };
            locacao.Pagamentos.Add(new Pagamento
            {
                IdPagamento = 1, IdLocacao = 10, Tipo = TipoPagamento.CAUCAO, Status = StatusPagamento.PENDENTE, Valor = 0m,
                IdTransacao = "FAKE-CAU-HTTP", DataVencimento = new DateOnly(2026, 11, 18), CreatedAt = Agora, UpdatedAt = Agora
            });
            Locacoes.Adicionar(locacao);
            Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
            Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, KmRegistrado = 150, DataHora = Agora.AddHours(-1), ComparacaoRealizada = true });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILocacaoRepository>();
                services.RemoveAll<IVistoriaSaidaRepository>();
                services.RemoveAll<IVistoriaRepository>();
                services.RemoveAll<IApuracaoCobrancasExtrasService>();
                services.RemoveAll<ICaucaoGateway>();
                services.RemoveAll<IRelogio>();
                services.AddSingleton<ILocacaoRepository>(Locacoes);
                services.AddSingleton<IVistoriaSaidaRepository>(Saidas);
                services.AddSingleton<IVistoriaRepository>(Saidas.Vistorias);
                services.AddSingleton<IApuracaoCobrancasExtrasService>(new FakeApuracaoCobrancasExtrasService { Pre = new PreValidacaoEncerramentoRegistro { Total = 0m, ApuracaoExistente = true } });
                services.AddSingleton<ICaucaoGateway>(Gateway);
                services.AddSingleton<IRelogio>(new FakeRelogio(Agora));
            });
        }
    }
}
