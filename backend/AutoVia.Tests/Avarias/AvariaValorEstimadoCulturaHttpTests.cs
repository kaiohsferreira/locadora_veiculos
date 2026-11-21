using AutoVia.Helpers;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.Avarias;

/// <summary>
/// Colecao xUnit sem paralelismo: a cultura padrao das threads (CultureInfo.DefaultThreadCurrentCulture) e estado GLOBAL do processo,
/// por isso os testes que a trocam ficam sozinhos, sem rodar ao mesmo tempo que os outros.
/// </summary>
[CollectionDefinition(CulturaGlobalCollection.Nome, DisableParallelization = true)]
public sealed class CulturaGlobalCollection
{
    public const string Nome = "CulturaGlobalDoProcesso";
}

/// <summary>
/// Valor estimado do formulario da avaria em HTTP (TestServer), com a cultura padrao do processo forcada em cada teste.
/// Reproduz o defeito: "150.50" em pt-BR era lido como 15050 e gravado com 201.
/// </summary>
[Collection(CulturaGlobalCollection.Nome)]
public sealed class AvariaValorEstimadoCulturaHttpTests
{
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("")]
    public async Task ValorComPonto_EGravadoComOValorExato_EmQualquerCultura(string nomeCultura)
    {
        await ExecutarComCulturaAsync(nomeCultura, async () =>
        {
            using AvariaHttpTests.AvariaWebFactory fabrica = new AvariaHttpTests.AvariaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await PostarAvariaComValor(cliente, AvariaHttpTests.TokenDe(fabrica), "150.50");

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Equal(150.50m, fabrica.Avarias.UltimaCriada?.ValorEstimado);
        });
    }

    [Theory]
    [InlineData("pt-BR")]
    [InlineData("en-US")]
    [InlineData("")]
    public async Task ValorComVirgula_ERecusadoComMensagemESemGravar_EmQualquerCultura(string nomeCultura)
    {
        await ExecutarComCulturaAsync(nomeCultura, async () =>
        {
            using AvariaHttpTests.AvariaWebFactory fabrica = new AvariaHttpTests.AvariaWebFactory();
            using HttpClient cliente = fabrica.CreateClient();

            using HttpResponseMessage resposta = await PostarAvariaComValor(cliente, AvariaHttpTests.TokenDe(fabrica), "150,50");

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Contains(ConstantsMessage.ErroAvariaValorEstimadoInvalido, await LerErrosDoValorEstimado(resposta));
            Assert.Equal(0, fabrica.Avarias.CriacaoCount);
            Assert.Equal(0, fabrica.Armazenamento.UploadCount);
        });
    }

    private static async Task ExecutarComCulturaAsync(string nomeCultura, Func<Task> teste)
    {
        CultureInfo? culturaPadraoAnterior = CultureInfo.DefaultThreadCurrentCulture;
        CultureInfo? culturaUiPadraoAnterior = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            CultureInfo cultura = string.IsNullOrEmpty(nomeCultura) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(nomeCultura);
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            Assert.Equal(cultura.Name, CultureInfo.CurrentCulture.Name);
            await teste();
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = culturaPadraoAnterior;
            CultureInfo.DefaultThreadCurrentUICulture = culturaUiPadraoAnterior;
        }
    }

    private static async Task<HttpResponseMessage> PostarAvariaComValor(HttpClient cliente, string token, string valorEstimado)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Post, $"/api/vistorias/{AvariaHttpTests.IdVistoria}/avarias");
        pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        MultipartFormDataContent formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent("Arranhão"), "descricao");
        formulario.Add(new StringContent("LEVE"), "gravidade");
        formulario.Add(new StringContent(valorEstimado), "valorEstimado");
        ByteArrayContent arquivo = new ByteArrayContent(AvariaHttpTests.JpegValido());
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formulario.Add(arquivo, "foto", "foto.jpg");
        pedido.Content = formulario;
        return await cliente.SendAsync(pedido);
    }

    private static async Task<List<string>> LerErrosDoValorEstimado(HttpResponseMessage resposta)
    {
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        List<string> mensagens = new List<string>();
        foreach (JsonElement item in documento.RootElement.GetProperty("errors").GetProperty("valorEstimado").EnumerateArray())
        {
            mensagens.Add(item.GetString() ?? string.Empty);
        }
        return mensagens;
    }
}
