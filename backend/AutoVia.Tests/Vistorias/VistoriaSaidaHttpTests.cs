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

namespace AutoVia.Tests.Vistorias;

public sealed class VistoriaSaidaHttpTests : IDisposable
{
    private const string Rota = "/api/locacoes/10/vistorias/saida";
    private const string Payload = "{\"kmRegistrado\":150,\"nivelCombustivel\":0.5,\"idsAvariasEntrada\":[10],\"novasAvarias\":[{\"descricao\":\"Risco\",\"gravidade\":\"LEVE\",\"valorEstimado\":30}]}";
    private readonly SaidaWebFactory _fabrica = new SaidaWebFactory();
    private readonly HttpClient _cliente;

    public VistoriaSaidaHttpTests() { _cliente = _fabrica.CreateClient(); }

    public void Dispose() { _cliente.Dispose(); _fabrica.Dispose(); }

    private string Token(PerfilUsuario perfil = PerfilUsuario.FUNCIONARIO)
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = _fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        return tokens.GerarToken(perfil, 20, "Teste", perfil == PerfilUsuario.FUNCIONARIO ? CargoFuncionario.ATENDENTE : null,
            "versao-de-teste", false, perfil == PerfilUsuario.FUNCIONARIO ? 1 : null).Token;
    }

    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string rota, string? token, string? corpo = null)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(metodo, rota);
        if (token != null) pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        if (corpo != null) pedido.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        return await _cliente.SendAsync(pedido);
    }

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using JsonDocument doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task UmaEntradaDuasSaidaSomenteUmaNovaLocationEReabertura()
    {
        string corpo = Payload[..^1] + ",\"idFuncionario\":999,\"tipo\":\"ENTRADA\",\"comparacaoRealizada\":false}";
        using HttpResponseMessage post = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.Equal(Rota, post.Headers.Location!.AbsolutePath);
        JsonElement comparacao = await Ler(post);
        Assert.True(comparacao.GetProperty("comparacaoRealizada").GetBoolean());
        Assert.Equal(1, comparacao.GetProperty("avariasPreexistentes").GetArrayLength());
        Assert.Equal(1, comparacao.GetProperty("avariasNovas").GetArrayLength());
        Assert.Equal(0, comparacao.GetProperty("avariasIndeterminadas").GetArrayLength());
        Assert.Equal(10, comparacao.GetProperty("avariasPreexistentes")[0].GetProperty("idAvariaEntrada").GetInt32());
        JsonElement saida = comparacao.GetProperty("saida");
        Assert.Equal(20, saida.GetProperty("idFuncionario").GetInt32());
        Assert.Equal("SAIDA", saida.GetProperty("tipo").GetString());
        Assert.Equal("18/11/2026 12:00", saida.GetProperty("dataHoraStr").GetString());
        _fabrica.Saidas.Avarias[0].Descricao = "Alteração posterior na entrada";
        _fabrica.Saidas.Avarias[0].DisabledAt = SaidaWebFactory.Agora;
        using HttpResponseMessage get = await Enviar(HttpMethod.Get, post.Headers.Location.PathAndQuery, Token());
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        JsonElement reaberta = await Ler(get);
        Assert.Equal("Risco", reaberta.GetProperty("avariasPreexistentes")[0].GetProperty("descricao").GetString());
        Assert.Equal(1, reaberta.GetProperty("avariasNovas").GetArrayLength());
        using HttpResponseMessage duplicada = await Enviar(HttpMethod.Post, Rota, Token(), Payload);
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
        Assert.Equal(2, _fabrica.Saidas.Vistorias.Vistorias.Count);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":0.5}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1.5,\"idsAvariasEntrada\":[],\"novasAvarias\":[]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":0.001,\"idsAvariasEntrada\":[],\"novasAvarias\":[]}")]
    [InlineData("{\"kmRegistrado\":-1,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[10,10],\"novasAvarias\":[]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[0],\"novasAvarias\":[]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[null]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\"Dano\",\"gravidade\":\"INVALIDA\"}]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\" \" ,\"gravidade\":\"LEVE\"}]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\"Dano\",\"gravidade\":\"LEVE\",\"valorEstimado\":-1}]}")]
    [InlineData("{\"kmRegistrado\":150,\"nivelCombustivel\":1,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\"Dano\",\"gravidade\":\"LEVE\",\"valorEstimado\":0.001}]}")]
    public async Task EntradaInvalida400SemGravar(string corpo)
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType!.MediaType);
        Assert.Single(_fabrica.Saidas.Vistorias.Vistorias);
        Assert.Single(_fabrica.Saidas.Avarias);
    }

    [Theory]
    [InlineData("GET", "/api/locacoes/10/devolucao")]
    [InlineData("GET", Rota)]
    [InlineData("POST", Rota)]
    public async Task SemToken401Cliente403(string metodo, string rota)
    {
        HttpMethod http = new HttpMethod(metodo);
        using HttpResponseMessage anonima = await Enviar(http, rota, null, metodo == "POST" ? Payload : null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonima.StatusCode);
        using HttpResponseMessage cliente = await Enviar(http, rota, Token(PerfilUsuario.CLIENTE), metodo == "POST" ? Payload : null);
        Assert.Equal(HttpStatusCode.Forbidden, cliente.StatusCode);
    }

    [Fact]
    public async Task ContextoPreparacaoSemSaidaENaoEncerraLocacao()
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, "/api/locacoes/10/devolucao", Token());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement contexto = await Ler(resposta);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("comparacao").ValueKind);
        Assert.Equal(1, contexto.GetProperty("avariasEntrada").GetArrayLength());
        Assert.Equal("EM_ANDAMENTO", contexto.GetProperty("locacao").GetProperty("status").GetString());
        using HttpResponseMessage post = await Enviar(HttpMethod.Post, Rota, Token(), Payload);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Locacao locacao = (await _fabrica.Locacoes.FindByIdAsync(10))!;
        Assert.Equal(StatusLocacao.EM_ANDAMENTO, locacao.Status);
        Assert.Null(locacao.KmChegada);
        Assert.Null(locacao.DataDevolucaoEfetiva);
    }

    [Fact]
    public async Task OrigemAlheia404SemGravarEEntradaAusente409()
    {
        _fabrica.Saidas.Avarias.Add(new Avaria { IdAvaria = 99, IdVistoria = 999, Descricao = "Outra locação" });
        using HttpResponseMessage origem = await Enviar(HttpMethod.Post, Rota, Token(), Payload.Replace("[10]", "[99]"));
        Assert.Equal(HttpStatusCode.NotFound, origem.StatusCode);
        Assert.Single(_fabrica.Saidas.Vistorias.Vistorias);
        _fabrica.Saidas.Vistorias.Vistorias.Clear();
        using HttpResponseMessage entrada = await Enviar(HttpMethod.Post, Rota, Token(), Payload);
        Assert.Equal(HttpStatusCode.Conflict, entrada.StatusCode);
        Assert.Empty(_fabrica.Saidas.Vistorias.Vistorias);
    }

    [Fact]
    public async Task LocacaoAusente404ESaidaAusente404()
    {
        using HttpResponseMessage saida = await Enviar(HttpMethod.Get, Rota, Token());
        Assert.Equal(HttpStatusCode.NotFound, saida.StatusCode);
        Assert.Equal(ConstantsMessage.ErroVistoriaSaidaNaoEncontrada, (await Ler(saida)).GetProperty("detail").GetString());
        using HttpResponseMessage contexto = await Enviar(HttpMethod.Get, "/api/locacoes/999/devolucao", Token());
        Assert.Equal(HttpStatusCode.NotFound, contexto.StatusCode);
        using HttpResponseMessage post = await Enviar(HttpMethod.Post, "/api/locacoes/999/vistorias/saida", Token(), Payload);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task LegadoSemMarcadorSoTemIndeterminadas()
    {
        _fabrica.Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 2, IdLocacao = 10, Tipo = TipoVistoria.SAIDA });
        _fabrica.Saidas.Avarias.Add(new Avaria { IdAvaria = 12, IdVistoria = 2, IdAvariaEntrada = 10, Descricao = "Risco" });
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, Rota, Token());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement comparacao = await Ler(resposta);
        Assert.False(comparacao.GetProperty("comparacaoRealizada").GetBoolean());
        Assert.Equal(1, comparacao.GetProperty("avariasIndeterminadas").GetArrayLength());
        Assert.Equal(0, comparacao.GetProperty("avariasNovas").GetArrayLength());
        Assert.Equal(0, comparacao.GetProperty("avariasPreexistentes").GetArrayLength());
    }

    private sealed class SaidaWebFactory : VeiculoWebFactory
    {
        public static readonly DateTime Agora = new DateTime(2026, 11, 18, 12, 0, 0);
        public FakeVistoriaSaidaRepository Saidas { get; } = new FakeVistoriaSaidaRepository();
        public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();

        public SaidaWebFactory()
        {
            Locacoes.Adicionar(new Locacao { IdLocacao = 10, KmSaida = 100, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = Agora.AddDays(-1) });
            Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
            Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = 1, IdLocacao = 10, Tipo = TipoVistoria.ENTRADA, KmRegistrado = 110, DataHora = Agora.AddHours(-2) });
            Saidas.Avarias.Add(new Avaria { IdAvaria = 10, IdVistoria = 1, Descricao = "Risco", Gravidade = GravidadeAvaria.LEVE, ValorEstimado = 20m });
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILocacaoRepository>();
                services.RemoveAll<IVistoriaRepository>();
                services.RemoveAll<IVistoriaSaidaRepository>();
                services.RemoveAll<IRelogio>();
                services.AddSingleton<ILocacaoRepository>(Locacoes);
                services.AddSingleton<IVistoriaRepository>(Saidas.Vistorias);
                services.AddSingleton<IVistoriaSaidaRepository>(Saidas);
                services.AddSingleton<IRelogio>(new FakeRelogio(Agora));
            });
        }
    }
}
