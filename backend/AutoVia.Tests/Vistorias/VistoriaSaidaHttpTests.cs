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
    public async Task CopiadaDaEntradaLevaAnguloEPosicaoNaComparacaoEANovaFicaNula()
    {
        _fabrica.Saidas.Avarias[0].Angulo = AnguloAvaria.FRENTE;
        _fabrica.Saidas.Avarias[0].PosX = 42.50m;
        _fabrica.Saidas.Avarias[0].PosY = 17m;

        using HttpResponseMessage post = await Enviar(HttpMethod.Post, Rota, Token(), Payload);

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        JsonElement comparacao = await Ler(post);
        JsonElement preexistente = comparacao.GetProperty("avariasPreexistentes")[0];
        Assert.Equal("FRENTE", preexistente.GetProperty("angulo").GetString());
        Assert.Equal(42.5m, preexistente.GetProperty("posX").GetDecimal());
        Assert.Equal(17m, preexistente.GetProperty("posY").GetDecimal());
        JsonElement nova = comparacao.GetProperty("avariasNovas")[0];
        Assert.Equal(JsonValueKind.Null, nova.GetProperty("angulo").ValueKind);
        Assert.Equal(JsonValueKind.Null, nova.GetProperty("posX").ValueKind);
        Assert.Equal(JsonValueKind.Null, nova.GetProperty("posY").ValueKind);
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

    [Fact]
    public async Task RegistroDaSaidaDevolveOOperadorAbreviadoNoCreated()
    {
        using HttpResponseMessage post = await Enviar(HttpMethod.Post, Rota, Token(), Payload);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        JsonElement saida = (await Ler(post)).GetProperty("saida");
        Assert.Equal("Ana S.", saida.GetProperty("operadorStr").GetString());
    }

    [Fact]
    public async Task ContextoDevolucaoTrazOsTotaisPrevistosComOsCamposAditivos()
    {
        _fabrica.Apuracao.Totais = new TotaisContextoApuracaoRegistro { TotalExtras = 150m, IdConfiguracao = 1, VersaoEsperada = 1 };

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, "/api/locacoes/10/devolucao", Token());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement contexto = await Ler(resposta);
        Assert.Equal(2, contexto.GetProperty("diariasPrevistas").GetInt32());
        Assert.Equal(100m, contexto.GetProperty("valorDiaria").GetDecimal());
        Assert.Equal(StaticMethods.FormateToMonetary(100m), contexto.GetProperty("valorDiariaStr").GetString());
        Assert.Equal(200m, contexto.GetProperty("totalDiarias").GetDecimal());
        Assert.Equal(StaticMethods.FormateToMonetary(200m), contexto.GetProperty("totalDiariasStr").GetString());
        Assert.Equal(150m, contexto.GetProperty("totalExtras").GetDecimal());
        Assert.Equal(StaticMethods.FormateToMonetary(150m), contexto.GetProperty("totalExtrasStr").GetString());
        Assert.Equal(350m, contexto.GetProperty("valorTotalPrevisto").GetDecimal());
        Assert.Equal(StaticMethods.FormateToMonetary(350m), contexto.GetProperty("valorTotalPrevistoStr").GetString());
        Assert.Equal(1, contexto.GetProperty("idConfiguracao").GetInt32());
        Assert.Equal(1, contexto.GetProperty("versaoEsperada").GetInt32());
    }

    [Fact]
    public async Task ContextoDevolucaoSemTotaisDaApuracaoDevolve200ComTotaisExtrasNulos()
    {
        _fabrica.Apuracao.Totais = new TotaisContextoApuracaoRegistro();

        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, "/api/locacoes/10/devolucao", Token());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement contexto = await Ler(resposta);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("totalExtras").ValueKind);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("totalExtrasStr").ValueKind);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("valorTotalPrevisto").ValueKind);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("valorTotalPrevistoStr").ValueKind);
        Assert.Equal(JsonValueKind.Null, contexto.GetProperty("idConfiguracao").ValueKind);
        Assert.Equal(200m, contexto.GetProperty("totalDiarias").GetDecimal());
    }

    [Fact]
    public async Task ContextoDevolucaoSemCaucaoTrazSemCaucaoESemAcao()
    {
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Get, "/api/locacoes/10/devolucao", Token());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        JsonElement caucao = (await Ler(resposta)).GetProperty("caucao");
        Assert.Equal(AcaoCaucaoDevolucao.SEM_CAUCAO.ToString(), caucao.GetProperty("situacao").GetString());
        Assert.Equal(JsonValueKind.Null, caucao.GetProperty("acao").ValueKind);
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

    [Fact]
    public async Task AvariaNovaSemValorEstimadoRecusa400ComMensagemESemGravar()
    {
        const string corpo = "{\"kmRegistrado\":150,\"nivelCombustivel\":0.5,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\"Dano\",\"gravidade\":\"LEVE\"}]}";
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType!.MediaType);
        JsonElement problema = await Ler(resposta);
        Assert.Equal(ConstantsMessage.ErroVistoriaSaidaAvariaValorObrigatorio, problema.GetProperty("detail").GetString());
        Assert.True(problema.GetProperty("errors").TryGetProperty("novasAvarias[0].ValorEstimado", out _));
        Assert.Single(_fabrica.Saidas.Vistorias.Vistorias);
        Assert.Single(_fabrica.Saidas.Avarias);
    }

    [Fact]
    public async Task AvariaNovaComValorZeroRegistra201()
    {
        const string corpo = "{\"kmRegistrado\":150,\"nivelCombustivel\":0.5,\"idsAvariasEntrada\":[],\"novasAvarias\":[{\"descricao\":\"Dano\",\"gravidade\":\"LEVE\",\"valorEstimado\":0}]}";
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task SomaDasNovasAvariasAcimaDoTetoRecusa400ComMensagemESemGravar()
    {
        const string corpo = "{\"kmRegistrado\":150,\"nivelCombustivel\":0.5,\"idsAvariasEntrada\":[],\"novasAvarias\":["
            + "{\"descricao\":\"Dano A\",\"gravidade\":\"GRAVE\",\"valorEstimado\":99999999.99},"
            + "{\"descricao\":\"Dano B\",\"gravidade\":\"GRAVE\",\"valorEstimado\":99999999.99}]}";
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        JsonElement problema = await Ler(resposta);
        Assert.Equal(ConstantsMessage.ErroVistoriaSaidaAvariasSomaExcedida, problema.GetProperty("detail").GetString());
        Assert.Single(_fabrica.Saidas.Vistorias.Vistorias);
    }

    [Fact]
    public async Task UmaNovaAvariaNoTetoDaSomaRegistra201()
    {
        const string corpo = "{\"kmRegistrado\":150,\"nivelCombustivel\":0.5,\"idsAvariasEntrada\":[],\"novasAvarias\":["
            + "{\"descricao\":\"Dano A\",\"gravidade\":\"GRAVE\",\"valorEstimado\":99999999.99}]}";
        using HttpResponseMessage resposta = await Enviar(HttpMethod.Post, Rota, Token(), corpo);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    private sealed class SaidaWebFactory : VeiculoWebFactory
    {
        public static readonly DateTime Agora = new DateTime(2026, 11, 18, 12, 0, 0);
        public FakeVistoriaSaidaRepository Saidas { get; } = new FakeVistoriaSaidaRepository();
        public FakeLocacaoRepository Locacoes { get; } = new FakeLocacaoRepository();
        public FakeApuracaoCobrancasExtrasService Apuracao { get; } = new FakeApuracaoCobrancasExtrasService();

        public SaidaWebFactory()
        {
            Veiculo veiculo = new Veiculo { IdVeiculo = 5, IdCategoria = 1, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno", ValorDiaria = 100m, KmAtual = 100 };
            Locacoes.Adicionar(new Locacao { IdLocacao = 10, KmSaida = 100, Status = StatusLocacao.EM_ANDAMENTO, DataRetirada = Agora.AddDays(-1), DataDevolucaoPrevista = Agora.AddDays(1), ValorDiaria = 100m, Veiculo = veiculo, IdVeiculo = 5 });
            Locacoes.Adicionar(new Funcionario { IdFuncionario = 20, Ativo = true });
            Saidas.Funcionarios.Add(new Funcionario { IdFuncionario = 20, Nome = "Ana Paula Souza", Ativo = true });
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
                services.RemoveAll<IApuracaoCobrancasExtrasService>();
                services.AddSingleton<IApuracaoCobrancasExtrasService>(Apuracao);
                services.AddSingleton<ILocacaoRepository>(Locacoes);
                services.AddSingleton<IVistoriaRepository>(Saidas.Vistorias);
                services.AddSingleton<IVistoriaSaidaRepository>(Saidas);
                services.AddSingleton<IRelogio>(new FakeRelogio(Agora));
            });
        }
    }
}
