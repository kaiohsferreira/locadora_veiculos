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
    internal const int IdVistoria = 14;
    private const int IdLocacao = 10;
    private const int IdVistoriaDeSaida = 15;
    /// <summary>Locadora do funcionario que faz o pedido (token de TokenDe).</summary>
    internal const int IdEmpresaDoToken = 1;
    /// <summary>Outra locadora: a vistoria semeada aqui nao pertence ao contexto do token.</summary>
    private const int IdEmpresaAlheia = 2;

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

    [Fact]
    public async Task PostComAnguloEPosicaoDevolve201ComOsTresCampos()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido(), angulo: "FRENTE", posX: "42.50", posY: "17");

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal("FRENTE", documento.RootElement.GetProperty("angulo").GetString());
        Assert.Equal(42.5m, documento.RootElement.GetProperty("posX").GetDecimal());
        Assert.Equal(17m, documento.RootElement.GetProperty("posY").GetDecimal());
        Assert.Equal(AnguloAvaria.FRENTE, fabrica.Avarias.UltimaCriada?.Angulo);
    }

    [Fact]
    public async Task PostSemAnguloNemPosicaoDevolve201ComNulos()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, documento.RootElement.GetProperty("angulo").ValueKind);
        Assert.Equal(JsonValueKind.Null, documento.RootElement.GetProperty("posX").ValueKind);
        Assert.Equal(JsonValueKind.Null, documento.RootElement.GetProperty("posY").ValueKind);
    }

    [Fact]
    public async Task PostComPosicaoParcialRecusa400EmErrorsAnguloSemGravar()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await PostarAvaria(cliente, TokenDe(fabrica), JpegValido(), posX: "42.50");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(ConstantsMessage.ErroAvariaPosicaoIncompleta, documento.RootElement.GetProperty("errors").GetProperty("angulo")[0].GetString());
        Assert.Equal(0, fabrica.Avarias.CriacaoCount);
        Assert.Equal(0, fabrica.Armazenamento.UploadCount);
    }

    [Fact]
    public async Task ListarSemAvariasDevolve200ComArrayVazio()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, TokenDe(fabrica));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, documento.RootElement.ValueKind);
        Assert.Equal(0, documento.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task ListarComFotoAusenteNaEntradaTrazErroFotoEFotoComUrl()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        fabrica.Avarias.Semear(new Avaria { IdAvaria = 1, IdVistoria = IdVistoria, Descricao = "Sem arquivo", Gravidade = GravidadeAvaria.LEVE, Angulo = AnguloAvaria.FRENTE, PosX = 10m, PosY = 20m });
        fabrica.Avarias.Semear(new Avaria { IdAvaria = 2, IdVistoria = IdVistoria, Descricao = "Com arquivo", Gravidade = GravidadeAvaria.MEDIA });
        fabrica.Armazenamento.RegistrarObjeto("vistorias/14/avarias/2/foto");
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, TokenDe(fabrica));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using JsonDocument documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        JsonElement semArquivo = documento.RootElement[0];
        Assert.Equal(1, semArquivo.GetProperty("idAvaria").GetInt32());
        Assert.Equal("FRENTE", semArquivo.GetProperty("angulo").GetString());
        Assert.Equal(JsonValueKind.Null, semArquivo.GetProperty("foto").ValueKind);
        Assert.Equal(ConstantsMessage.ErroAvariaFotoSemArquivo, semArquivo.GetProperty("erroFoto").GetString());
        JsonElement comArquivo = documento.RootElement[1];
        Assert.Equal(2, comArquivo.GetProperty("idAvaria").GetInt32());
        Assert.Equal(JsonValueKind.Object, comArquivo.GetProperty("foto").ValueKind);
        Assert.False(string.IsNullOrEmpty(comArquivo.GetProperty("foto").GetProperty("url").GetString()));
        Assert.Equal(JsonValueKind.Null, comArquivo.GetProperty("erroFoto").ValueKind);
    }

    [Fact]
    public async Task ListarSemTokenDevolve401()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, null);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarComTokenDeClienteDevolve403()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, TokenDeCliente(fabrica));

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task ListarDeVistoriaInexistenteDevolve404SemConsultarArmazenamento()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory();
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, TokenDe(fabrica), idVistoria: 999);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada, await LerDetail(resposta));
        Assert.Equal(0, fabrica.Armazenamento.HeadCount);
    }

    [Fact]
    public async Task ListarDeVistoriaDeOutraLocadoraDevolve404SemConsultarArmazenamento()
    {
        using AvariaWebFactory fabrica = new AvariaWebFactory { IdEmpresaDaVistoria = IdEmpresaAlheia };
        using HttpClient cliente = fabrica.CreateClient();

        using HttpResponseMessage resposta = await Listar(cliente, TokenDe(fabrica));

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada, await LerDetail(resposta));
        Assert.Equal(0, fabrica.Armazenamento.HeadCount);
    }

    private static async Task<HttpResponseMessage> Listar(HttpClient cliente, string? token, int idVistoria = IdVistoria)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Get, $"/api/vistorias/{idVistoria}/avarias");
        if (token != null) pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        return await cliente.SendAsync(pedido);
    }

    private static string TokenDeCliente(AvariaWebFactory fabrica)
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        return tokens.GerarToken(PerfilUsuario.CLIENTE, 20, "Teste", null, "versao-de-teste", false, null).Token;
    }

    internal static string TokenDe(AvariaWebFactory fabrica)
    {
        JwtOptions opcoes = new JwtOptions
        {
            Emissor = VeiculoWebFactory.Emissor, Audiencia = VeiculoWebFactory.Audiencia,
            ExpiracaoMinutos = 120, Chave = fabrica.ChaveJwt
        };
        TokenService tokens = new TokenService(opcoes, new FakeLogService());
        return tokens.GerarToken(PerfilUsuario.FUNCIONARIO, 20, "Teste", CargoFuncionario.ATENDENTE,
            "versao-de-teste", false, IdEmpresaDoToken).Token;
    }

    private static async Task<HttpResponseMessage> PostarAvaria(HttpClient cliente, string token, byte[] foto, string? angulo = null, string? posX = null, string? posY = null)
    {
        using HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Post, $"/api/vistorias/{IdVistoria}/avarias");
        pedido.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
        MultipartFormDataContent formulario = new MultipartFormDataContent();
        formulario.Add(new StringContent("Arranhão"), "descricao");
        formulario.Add(new StringContent("LEVE"), "gravidade");
        if (angulo != null) formulario.Add(new StringContent(angulo), "angulo");
        if (posX != null) formulario.Add(new StringContent(posX), "posX");
        if (posY != null) formulario.Add(new StringContent(posY), "posY");
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

    internal static byte[] JpegValido()
    {
        using SKBitmap bitmap = new SKBitmap(1, 1);
        bitmap.Erase(SKColors.White);
        using SKImage imagem = SKImage.FromBitmap(bitmap);
        using SKData dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 90);
        return dados.ToArray();
    }

    internal sealed class AvariaWebFactory : VeiculoWebFactory
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

        /// <summary>Locadora do veiculo da locacao semeada (padrao: a do token). Definido antes do primeiro CreateClient.</summary>
        public int IdEmpresaDaVistoria { get; set; } = IdEmpresaDoToken;

        public AvariaWebFactory()
        {
            Avarias = new FakeAvariaRepository(Saidas);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                Locacao locacao = new Locacao { IdLocacao = IdLocacao, Status = StatusDaLocacao, KmSaida = 100, Veiculo = new Veiculo { IdEmpresa = IdEmpresaDaVistoria } };
                Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = IdVistoria, IdLocacao = IdLocacao, Tipo = TipoDaVistoria, Locacao = locacao });
                if (ComSaidaAtiva)
                {
                    Saidas.Vistorias.Vistorias.Add(new Vistoria { IdVistoria = IdVistoriaDeSaida, IdLocacao = IdLocacao, Tipo = TipoVistoria.SAIDA, Locacao = locacao });
                }
                services.RemoveAll<IAvariaRepository>();
                services.RemoveAll<IVistoriaRepository>();
                services.RemoveAll<IVistoriaSaidaRepository>();
                services.RemoveAll<IArmazenamentoGateway>();
                services.AddSingleton<IAvariaRepository>(new RepositorioAvariaComEscopoEmpresa(Avarias, IdEmpresaDoToken));
                services.AddSingleton<IVistoriaRepository>(Saidas.Vistorias);
                services.AddSingleton<IVistoriaSaidaRepository>(Saidas);
                services.AddSingleton<IArmazenamentoGateway>(Armazenamento);
            });
        }
    }

    /// <summary>
    /// Repositorio de avarias com o filtro "Empresa" do EscopoEmpresa (vistoria -> Locacao -> Veiculo -> IdEmpresa), so para o teste HTTP.
    /// O FakeAvariaRepository nao filtra por locadora; este envoltorio faz o que o banco faz: vistoria de outra locadora nao e encontrada.
    /// </summary>
    internal sealed class RepositorioAvariaComEscopoEmpresa : IAvariaRepository
    {
        private readonly IAvariaRepository _interno;
        private readonly int _idEmpresaDoContexto;

        public RepositorioAvariaComEscopoEmpresa(IAvariaRepository interno, int idEmpresaDoContexto)
        {
            _interno = interno;
            _idEmpresaDoContexto = idEmpresaDoContexto;
        }

        public async Task<Vistoria?> FindAtivaComLocacaoAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            Vistoria? vistoria = await _interno.FindAtivaComLocacaoAsync(idVistoria, cancellationToken);
            return DaEmpresaDoContexto(vistoria) ? vistoria : null;
        }

        public async Task<Vistoria?> FindVistoriaAtivaAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            Vistoria? vistoria = await _interno.FindVistoriaAtivaAsync(idVistoria, cancellationToken);
            return DaEmpresaDoContexto(vistoria) ? vistoria : null;
        }

        public Task<Avaria?> FindAtivaAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default)
        {
            return _interno.FindAtivaAsync(idVistoria, idAvaria, cancellationToken);
        }

        public Task<List<Avaria>> FindAtivasByVistoriaAsync(int idVistoria, CancellationToken cancellationToken = default)
        {
            return _interno.FindAtivasByVistoriaAsync(idVistoria, cancellationToken);
        }

        public Task<Avaria> CriarAsync(Avaria avaria, CancellationToken cancellationToken = default)
        {
            return _interno.CriarAsync(avaria, cancellationToken);
        }

        private bool DaEmpresaDoContexto(Vistoria? vistoria)
        {
            return vistoria != null && vistoria.Locacao.Veiculo.IdEmpresa == _idEmpresaDoContexto;
        }
    }
}
