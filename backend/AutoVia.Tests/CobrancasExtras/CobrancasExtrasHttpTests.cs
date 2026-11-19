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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.CobrancasExtras;

public sealed class CobrancasExtrasHttpTests : IDisposable
{
    private const string Base = "/api/locacoes/10/cobrancas-extras";
    private const string Cadastro = "/api/configuracoes-cobrancas-extras";
    private const string Pedido = "{\"idConfiguracao\":1,\"versaoEsperada\":1}";
    private readonly ApuracaoWebFactory _fabrica = new ApuracaoWebFactory();
    private readonly HttpClient _cliente;
    public CobrancasExtrasHttpTests() { _cliente = _fabrica.CreateClient(); }
    public void Dispose() { _cliente.Dispose(); _fabrica.Dispose(); }
    private string Token(PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO)
    {
        JwtOptions opcoes = new JwtOptions { Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia, ExpiracaoMinutos = 120, Chave = _fabrica.ChaveJwt };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        return tokens.GerarToken(perfil, 20, "Teste", perfil == PerfilUsuario.FUNCIONARIO ? _fabrica.Sessao.Situacao.Cargo : null,
            "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? _fabrica.Sessao.Situacao.IdEmpresa : null).Token;
    }
    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string rota, string? corpo, string? token)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(metodo, rota);
        if (token != null) pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        if (corpo != null) pedido.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        return await _cliente.SendAsync(pedido);
    }
    [Fact]
    public async Task Autenticacao401Funcionario403CadastroECliente403Simulacao()
    {
        using HttpResponseMessage anonima = await Enviar(HttpMethod.Post, Base + "/simulacao", "{}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
        using HttpResponseMessage cliente = await Enviar(HttpMethod.Post, Base + "/simulacao", "{}", Token(PerfilUsuario.CLIENTE));
        Assert.Equal(HttpStatusCode.Forbidden, cliente.StatusCode);
        using HttpResponseMessage funcionario = await Enviar(HttpMethod.Get, Cadastro, null, Token());
        Assert.Equal(HttpStatusCode.Forbidden, funcionario.StatusCode);
        _fabrica.Sessao.Situacao.Cargo = CargoFuncionario.ADMINISTRADOR;
        using HttpResponseMessage admin = await Enviar(HttpMethod.Get, Cadastro, null, Token());
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }
    [Fact]
    public async Task SimulacaoIgnoraTarifasDoClienteUsaCadastroENaoPersiste()
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Base + "/simulacao", "{\"valorKmExcedente\":999,\"total\":1}", Token());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode); Assert.True(resposta.Headers.CacheControl!.NoStore);
        using JsonDocument json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(230m, json.RootElement.GetProperty("total").GetDecimal());
        Assert.Equal(1, json.RootElement.GetProperty("configuracao").GetProperty("versao").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("diariasPrevistas").GetInt32());
        Assert.Empty(_fabrica.Repository.Apuracoes);
    }
    [Fact]
    public async Task Registro201LocationReplay200EGetImutavel()
    {
        using HttpResponseMessage primeira = await Enviar(HttpMethod.Post, Base + "/apuracao", Pedido, Token());
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode); Assert.NotNull(primeira.Headers.Location);
        string esperado = await primeira.Content.ReadAsStringAsync();
        _fabrica.Repository.Configuracoes[0].DisabledAt = ApuracaoWebFactory.Inicio;
        _fabrica.Repository.Locacoes[0].Status = StatusLocacao.FINALIZADA;
        using HttpResponseMessage repetida = await Enviar(HttpMethod.Post, Base + "/apuracao", Pedido, Token());
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode); Assert.Equal(esperado, await repetida.Content.ReadAsStringAsync());
        using HttpResponseMessage get = await Enviar(HttpMethod.Get, Base + "/apuracao", null, Token());
        Assert.Equal(HttpStatusCode.OK, get.StatusCode); Assert.Equal(esperado, await get.Content.ReadAsStringAsync());
        Assert.Single(_fabrica.Repository.Apuracoes);
    }
    [Fact]
    public async Task Entrada400Versao409SemApuracao404EAusente404()
    {
        using HttpResponseMessage invalida = await Enviar(HttpMethod.Post, Base + "/apuracao", "{}", Token());
        Assert.Equal(HttpStatusCode.BadRequest, invalida.StatusCode);
        using HttpResponseMessage versao = await Enviar(HttpMethod.Post, Base + "/apuracao", Pedido.Replace("\"versaoEsperada\":1", "\"versaoEsperada\":2"), Token());
        Assert.Equal(HttpStatusCode.Conflict, versao.StatusCode);
        using HttpResponseMessage anterior = await Enviar(HttpMethod.Get, Base + "/apuracao", null, Token());
        Assert.Equal(HttpStatusCode.NotFound, anterior.StatusCode);
        using HttpResponseMessage ausente = await Enviar(HttpMethod.Post, Base.Replace("/10/", "/999/") + "/simulacao", "{}", Token());
        Assert.Equal(HttpStatusCode.NotFound, ausente.StatusCode);
        Assert.Equal("application/problem+json", versao.Content.Headers.ContentType!.MediaType);
    }
    [Fact]
    public async Task EmpresaDiferente404EConfiguracaoAusente409()
    {
        _fabrica.Sessao.Situacao.IdEmpresa = 2;
        using HttpResponseMessage outra = await Enviar(HttpMethod.Post, Base + "/simulacao", "{}", Token());
        Assert.Equal(HttpStatusCode.NotFound, outra.StatusCode);
        _fabrica.Sessao.Situacao.IdEmpresa = 1; _fabrica.Repository.Configuracoes.Clear();
        using HttpResponseMessage sem = await Enviar(HttpMethod.Post, Base + "/simulacao", "{}", Token());
        Assert.Equal(HttpStatusCode.Conflict, sem.StatusCode);
    }
    private sealed class ApuracaoWebFactory : VeiculoWebFactory
    {
        public static readonly DateTime Inicio = new DateTime(2026, 11, 18, 8, 0, 0);
        public FakeCobrancasExtrasRepository Repository { get; }
        public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();
        public FakeSessaoService Sessao { get; } = new FakeSessaoService();
        public ApuracaoWebFactory()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService()); contexto.Definir(1, 20, CargoFuncionario.ATENDENTE, false);
            Repository = new FakeCobrancasExtrasRepository(contexto);
            Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, IdEmpresa = 1, Ativo = true });
            Locacao locacao = new Locacao { IdLocacao = 10, KmSaida = 100, DataRetirada = Inicio, DataDevolucaoPrevista = Inicio.AddHours(2), Veiculo = new Veiculo { IdEmpresa = 1, IdCategoria = 1 } };
            locacao.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, DataHora = Inicio, KmRegistrado = 100, NivelCombustivel = 1m });
            locacao.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA, DataHora = Inicio.AddHours(4), KmRegistrado = 200, NivelCombustivel = 0.5m, ComparacaoRealizada = true });
            Repository.Locacoes.Add(locacao);
            Repository.Configuracoes.Add(new ConfiguracaoCobrancaExtra { IdConfiguracao = 1, IdEmpresa = 1, IdCategoria = 1, Versao = 1,
                Categoria = new Categoria { IdCategoria = 1, Nome = "Compacto" }, BaseFranquia = ConstantsMessage.CobrancasExtrasTotal,
                FranquiaKm = 50, ValorKmExcedente = 2m, ValorTanqueCompleto = 200m, ToleranciaAtrasoMinutos = 30,
                UnidadeAtraso = ConstantsMessage.CobrancasExtrasHora, FracionamentoAtraso = ConstantsMessage.CobrancasExtrasProporcional,
                ValorUnidadeAtraso = 20m, BaseAvarias = ConstantsMessage.CobrancasExtrasEstimativaVistoria });
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICobrancasExtrasRepository>(); services.RemoveAll<ILocacaoRepository>();
                services.RemoveAll<ISessaoService>(); services.RemoveAll<IRelogio>();
                services.AddSingleton<ISessaoService>(Sessao); services.AddSingleton<ILocacaoRepository>(Locacoes);
                services.AddSingleton<IRelogio>(new FakeRelogio(Inicio.AddHours(5)));
                services.AddScoped<ICobrancasExtrasRepository>(provider => provider.GetRequiredService<IContextoEmpresa>().IdEmpresa == 1
                    ? Repository : new FakeCobrancasExtrasRepository(provider.GetRequiredService<IContextoEmpresa>()));
            });
        }
    }
}
