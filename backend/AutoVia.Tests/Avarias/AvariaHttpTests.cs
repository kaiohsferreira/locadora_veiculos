using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Api;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkiaSharp;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AutoVia.Tests.Avarias;

/// <summary>Teste HTTP da rota POST /api/vistorias/{id}/avarias: as tres recusas 409 da regra da vistoria e o caminho que segue.</summary>
public sealed class AvariaHttpTests
{
    private const int IdVistoria = 14;
    private const int IdLocacao = 10;
    private const int IdVistoriaDeSaida = 15;

    [Fact]
    public async Task VistoriaDeSaidaRecusa409ComMensagemESemGravar()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory { TipoDaVistoria = TipoVistoria.SAIDA };
        using HttpClient cliente = fabrica.CreateClient();

        // Foto valida: a ordem 409 antes da decodificacao da foto e provada no teste unitario do servico (AvariaServiceTests).
        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido());

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroAvariaVistoriaSaida, await LerDetail(resposta));
        Assert.Equal(0, fabrica.Avarias.CriacaoCount);
        Assert.Equal(0, fabrica.Armazenamento.UploadCount);
    }

    [Fact]
    public async Task EntradaComSaidaAtivaRecusa409ComMensagemESemGravar()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory { ComSaidaAtiva = true };
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido());

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroAvariaEntradaAposSaida, await LerDetail(resposta));
        Assert.Equal(0, fabrica.Avarias.CriacaoCount);
        Assert.Equal(0, fabrica.Armazenamento.UploadCount);
    }

    [Fact]
    public async Task LocacaoFinalizadaRecusa409ComMensagemESemGravar()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory { StatusDaLocacao = StatusLocacao.FINALIZADA };
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido());

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroAvariaLocacaoFechada, await LerDetail(resposta));
        Assert.Equal(0, fabrica.Avarias.CriacaoCount);
        Assert.Equal(0, fabrica.Armazenamento.UploadCount);
    }

    [Fact]
    public async Task EntradaSemSaidaComLocacaoAtrasadaSegueCriandoAvaria()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory { StatusDaLocacao = StatusLocacao.ATRASADA };
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal(1, fabrica.Avarias.CriacaoCount);
        Assert.Equal(1, fabrica.Armazenamento.UploadCount);
    }

    private static string TokenDe(AvariaWebFactory fabrica)
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, 20, "Teste", CargoFuncionario.ATENDENTE,
            "versao-de-teste", false, 1).Token;
    }

    private static async Task<HttpResponseMessage> PostarAvaria(HttpClient cliente, string token, byte[] foto)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Post, $"/api/vistorias/{IdVistoria}/avarias");
        pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        MultipartFormDataContent formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent("Arranhão"), "descricao");
        formulario.Add(new StringContent("LEVE"), "gravidade");
        ByteArrayContent arquivo = new ByteArrayContent(foto);
        arquivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formulario.Add(arquivo, "foto", "foto.jpg");
        pedido.Content = formulario;
        return await cliente.SendAsync(pedido);
    }

    private static async Task<string?> LerDetail(HttpResponseMessage resposta)
    {
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return documento.RootElement.GetProperty("detail").GetString();
    }

    private static byte[] JpegValido()
    {
        using SKBitmap bitmap = new SKBitmap(1, 1);
        bitmap.Erase(SKColors.White);
        using SKImage imagem = SKImage.FromBitmap(bitmap);
        using SKData dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 90);
        return dados.ToArray();
    }

    private sealed class AvariaWebFactory : VeiculoWebFactory
    {
        public FakeVistoriaSaidaRepository Saidas { get; } = new FakeVistoriaSaidaRepository();
        public FakeAvariaRepository Avarias { get; }
        public FakeArmazenamentoGateway Armazenamento { get; } = new FakeArmazenamentoGateway();

        /// <summary>Tipo da vistoria semeada (padrao ENTRADA). Definido antes do primeiro CreateClient.</summary>
        public TipoVistoria TipoDaVistoria { get; set; } = TipoVistoria.ENTRADA;

        /// <summary>Status da locacao semeada (padrao EM_ANDAMENTO). Definido antes do primeiro CreateClient.</summary>
        public StatusLocacao StatusDaLocacao { get; set; } = StatusLocacao.EM_ANDAMENTO;

        /// <summary>Quando verdadeiro, semeia uma vistoria de SAIDA ativa na mesma locacao.</summary>
        public bool ComSaidaAtiva { get; set; }

        public AvariaWebFactory()
        {
            Avarias = new FakeAvariaRepository(Saidas);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                Locacao locacao = new Locacao { IdLocacao = IdLocacao, Status = StatusDaLocacao, KmSaida = 100 };
                Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = IdVistoria, IdLocacao = IdLocacao, Tipo = TipoDaVistoria, Locacao = locacao });
                if (ComSaidaAtiva)
                {
                    Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = IdVistoriaDeSaida, IdLocacao = IdLocacao, Tipo = TipoVistoria.SAIDA, Locacao = locacao });
                }
                services.RemoveAll<IAvariaRepository>();
                services.RemoveAll<IVistoriaRepository>();
                services.RemoveAll<IVistoriaSaidaRepository>();
                services.RemoveAll<IArmazenamentoGateway>();
                services.AddSingleton<IAvariaRepository>(Avarias);
                services.AddSingleton<IVistoriaRepository>(Saidas.Vistorias);
                services.AddSingleton<IVistoriaSaidaRepository>(Saidas);
                services.AddSingleton<IArmazenamentoGateway>(Armazenamento);
            });
        }
    }
}
