using AutoVia.Helpers;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Api;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// HTTP real do envio e da listagem de documentos do cliente, com o servidor de teste (AutenticacaoWebFactory) e o repositorio
    /// e o armazenamento trocados por fakes. Cobre 401 anonimo, 403 funcionario, 201 do dono com Location, 404 de outro cliente,
    /// 400 de tipo e de conteudo (nunca 502), a listagem, o limite por IP (429) e o corpo acima do teto no Kestrel (400 errors.corpo).
    /// </summary>
    public sealed class ClienteDocumentoHttpTests
    {
        private const int IdClienteDono = 1;
        private const int IdOutroCliente = 2;
        private const string CaminhoDocumentos = "/api/clientes/1/documentos";
        private const string CaminhoDocumentosOutro = "/api/clientes/2/documentos";
        private const string CaminhoLogin = "/api/auth/login";
        private const string PerfilCliente = "CLIENTE";
        private const string PerfilFuncionario = "FUNCIONARIO";
        private const string CpfCliente = "52998224725";
        private const string SenhaCliente = "SenhaCliente#2026";
        private const string CpfFuncionario = "11144477735";
        private const string SenhaFuncionario = "SenhaFunc#2026";
        private const string CampoToken = "token";
        private const string CampoErros = "errors";
        private const string CampoCorpo = "corpo";
        private const string CampoDetail = "detail";
        private const string CampoTipo = "tipo";
        private const string CampoUrl = "url";
        private const string TipoProblema = "application/problem+json";
        private const string TipoJson = "application/json";
        private const string CabecalhoLocation = "Location";
        private const string TextoDoFramework = "Request body";
        private const string NomeCnh = "cnh.pdf";
        private const string TipoCnh = "CNH";
        private const string TipoComprovante = "COMPROVANTE_RESIDENCIA";
        private const string TipoInvalido = "cnh";
        private const string PrefixoChave = "clientes/1/documentos/";
        private const int LimiteEnvioConfigurado = 2;

        /// <summary>Servidor de teste com os documentos em memoria; o cliente de teste (id 1) e o dono ativo.</summary>
        public class ClienteDocumentoWebFactory : AutenticacaoWebFactory
        {
            private readonly string? _limiteEnvio;

            public ClienteDocumentoWebFactory(string? limiteEnvio)
                : base(1000, null)
            {
                _limiteEnvio = limiteEnvio;
                DocumentosRepositorio.ClientesAtivos.Add(IdClienteDono);
            }

            /// <summary>Repositorio de documentos em memoria usado pelo servidor de teste.</summary>
            public FakeClienteDocumentoRepository DocumentosRepositorio { get; } = new FakeClienteDocumentoRepository();

            /// <summary>Armazenamento em memoria usado pelo servidor de teste (conta envios, remocoes e assinaturas).</summary>
            public FakeArmazenamentoGateway ArmazenamentoFalso { get; } = new FakeArmazenamentoGateway();

            protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
            {
                if (_limiteEnvio != null)
                {
                    valores[ConstantsValues.ChaveLimiteEnvioDocumentoCliente] = _limiteEnvio;
                }
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IClienteDocumentoRepository>();
                    services.AddSingleton<IClienteDocumentoRepository>(DocumentosRepositorio);
                    services.RemoveAll<IArmazenamentoGateway>();
                    services.AddSingleton<IArmazenamentoGateway>(ArmazenamentoFalso);
                });
            }
        }

        /// <summary>Servidor real (Kestrel em porta dinamica), porque o TestServer nao aplica o teto de corpo ([RequestSizeLimit]).</summary>
        private sealed class ClienteDocumentoKestrelWebFactory : ClienteDocumentoWebFactory
        {
            public ClienteDocumentoKestrelWebFactory()
                : base(null)
            {
                UseKestrel(0);
            }
        }

        private static byte[] Pdf(int tamanho)
        {
            byte[] conteudo = new byte[tamanho];
            ConstantsValues.AssinaturaPdf.CopyTo(conteudo, 0);

            return conteudo;
        }

        private static MultipartFormDataContent CriarFormulario(string? tipo, byte[] conteudo, string contentType)
        {
            MultipartFormDataContent formulario = new MultipartFormDataContent();

            if (tipo != null)
            {
                formulario.Add(new StringContent(tipo, Encoding.UTF8), ConstantsValues.CampoTipoDocumentoCliente);
            }

            ByteArrayContent arquivo = new ByteArrayContent(conteudo);
            arquivo.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            formulario.Add(arquivo, ConstantsValues.CampoArquivoDocumentoCliente, NomeCnh);

            return formulario;
        }

        private static async Task<HttpResponseMessage> EnviarComTokenAsync(HttpClient cliente, string caminho, string token, MultipartFormDataContent formulario)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, caminho) { Content = formulario };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);

            return await cliente.SendAsync(requisicao);
        }

        private static async Task<HttpResponseMessage> ListarComTokenAsync(HttpClient cliente, string caminho, string token)
        {
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);

            return await cliente.SendAsync(requisicao);
        }

        /// <summary>Login real: devolve o token do corpo da resposta.</summary>
        private static async Task<string> LogarAsync(HttpClient cliente, string perfil, string cpf, string senha)
        {
            string corpo = JsonSerializer.Serialize(new { perfil, cpf, senha });
            using StringContent conteudo = new StringContent(corpo, Encoding.UTF8, TipoJson);
            using HttpResponseMessage login = await cliente.PostAsync(CaminhoLogin, conteudo);
            string textoLogin = await login.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            using JsonDocument documento = JsonDocument.Parse(textoLogin);

            return documento.RootElement.GetProperty(CampoToken).GetString()!;
        }

        [Fact]
        public async Task SemToken_Da401ComProblema()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await cliente.PostAsync(CaminhoDocumentos, formulario);

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task TokenDeFuncionario_Da403SemArmazenamento()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilFuncionario, CpfFuncionario, SenhaFuncionario);

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task ClienteDono_EnviaPdf_Da201ComLocationEOVoCompleto()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);
            string texto = await resposta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            Assert.Contains(CaminhoDocumentos, resposta.Headers.Location?.ToString() ?? string.Empty, StringComparison.Ordinal);

            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(TipoCnh, documento.RootElement.GetProperty(CampoTipo).GetString());
            Assert.Equal(ConstantsValues.TipoConteudoPdf, documento.RootElement.GetProperty("tipoConteudo").GetString());
            Assert.False(string.IsNullOrEmpty(documento.RootElement.GetProperty(CampoUrl).GetString()));
            Assert.Equal(NomeCnh, documento.RootElement.GetProperty("nomeArquivo").GetString());

            Assert.Equal(1, fabrica.ArmazenamentoFalso.UploadCount);
            Assert.StartsWith(PrefixoChave, fabrica.ArmazenamentoFalso.UploadKeys[0], StringComparison.Ordinal);
            Assert.EndsWith(".pdf", fabrica.ArmazenamentoFalso.UploadKeys[0], StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdDeOutroCliente_Da404SemArmazenamento()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentosOutro, token, formulario);

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task TipoInvalido_Da400SemArmazenamentoENaoDa502()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using MultipartFormDataContent formulario = CriarFormulario(TipoInvalido, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task ArquivoComConteudoErrado_Da400NaoDa502()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            byte[] exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0 };

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, exe, ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task ArquivoAusente_Da400()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using MultipartFormDataContent formulario = new MultipartFormDataContent();
            formulario.Add(new StringContent(TipoCnh, Encoding.UTF8), ConstantsValues.CampoTipoDocumentoCliente);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task IdDeOutroClienteComArquivoInvalido_Da400ComOValidadorAntesDoDono()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            byte[] exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0, 0, 0, 0 };

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, exe, ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentosOutro, token, formulario);

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }

        [Fact]
        public async Task ArmazenamentoFora_Da502ENaoDa400NemGrava()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            fabrica.ArmazenamentoFalso.FalharEnvioIntegracao = true;

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.BadGateway, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Empty(fabrica.DocumentosRepositorio.Armazenados);
        }

        [Fact]
        public async Task ArmazenamentoComFalhaInesperada_Da500ComProblemaENaoGrava()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            fabrica.ArmazenamentoFalso.FalharUpload = true;

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);

            Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Empty(fabrica.DocumentosRepositorio.Armazenados);
        }

        [Fact]
        public async Task ListagemSemDocumentos_Da200ComListaVaziaSemArmazenamento()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using HttpResponseMessage resposta = await ListarComTokenAsync(cliente, CaminhoDocumentos, token);
            string texto = await resposta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            Assert.Equal("[]", texto);
            Assert.Equal(0, fabrica.ArmazenamentoFalso.AssinaturaLoteCount);
        }

        [Fact]
        public async Task ListagemComDocumentoEnviado_Da200ComUmItemComLink()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage envio = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);
            Assert.Equal(HttpStatusCode.Created, envio.StatusCode);

            using HttpResponseMessage resposta = await ListarComTokenAsync(cliente, CaminhoDocumentos, token);
            string texto = await resposta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(1, documento.RootElement.GetArrayLength());
            Assert.Equal(TipoCnh, documento.RootElement[0].GetProperty(CampoTipo).GetString());
            Assert.False(string.IsNullOrEmpty(documento.RootElement[0].GetProperty(CampoUrl).GetString()));
        }

        [Fact]
        public async Task ListagemDeOutroCliente_Da404()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using HttpResponseMessage resposta = await ListarComTokenAsync(cliente, CaminhoDocumentosOutro, token);

            Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        }

        [Fact]
        public async Task Substituicao_DeixaSoOAtivoEApagaOArquivoAntigo()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(null);
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);
            using (MultipartFormDataContent primeiro = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf))
            using (HttpResponseMessage envio = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, primeiro))
            {
                Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
            }

            using (MultipartFormDataContent segundo = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf))
            using (HttpResponseMessage envio = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, segundo))
            {
                Assert.Equal(HttpStatusCode.Created, envio.StatusCode);
            }

            using HttpResponseMessage lista = await ListarComTokenAsync(cliente, CaminhoDocumentos, token);
            using JsonDocument documento = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
            Assert.Equal(1, documento.RootElement.GetArrayLength());
            Assert.Single(fabrica.ArmazenamentoFalso.RemoveKeys);
            Assert.Equal(fabrica.ArmazenamentoFalso.UploadKeys[0], fabrica.ArmazenamentoFalso.RemoveKeys[0]);
        }

        [Fact]
        public async Task LimiteDeEnvioPorIp_TerceiraChamadaComLimiteDeDois_Da429()
        {
            using ClienteDocumentoWebFactory fabrica = new ClienteDocumentoWebFactory(LimiteEnvioConfigurado.ToString(CultureInfo.InvariantCulture));
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            for (int i = 0; i < LimiteEnvioConfigurado; i++)
            {
                using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
                using HttpResponseMessage dentro = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, formulario);
                Assert.NotEqual(HttpStatusCode.TooManyRequests, dentro.StatusCode);
            }

            using MultipartFormDataContent estourado = CriarFormulario(TipoCnh, Pdf(64), ConstantsValues.TipoConteudoPdf);
            using HttpResponseMessage resposta = await EnviarComTokenAsync(cliente, CaminhoDocumentos, token, estourado);

            Assert.Equal(HttpStatusCode.TooManyRequests, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task CorpoAcimaDoTetoNoKestrel_Da400ComErrosDoCorpo()
        {
            using ClienteDocumentoKestrelWebFactory fabrica = new ClienteDocumentoKestrelWebFactory();
            fabrica.StartServer();
            using HttpClient cliente = fabrica.CreateClient();
            string token = await LogarAsync(cliente, PerfilCliente, CpfCliente, SenhaCliente);

            using MultipartFormDataContent formulario = CriarFormulario(TipoCnh, Pdf(ConstantsValues.TamanhoMaximoCorpoDocumentoCliente + 1), ConstantsValues.TipoConteudoPdf);
            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, CaminhoDocumentos) { Content = formulario };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            requisicao.Headers.ExpectContinue = true;

            using HttpResponseMessage resposta = await cliente.SendAsync(requisicao);
            string texto = await resposta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(TextoDoFramework, texto, StringComparison.OrdinalIgnoreCase);

            using JsonDocument documento = JsonDocument.Parse(texto);
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, documento.RootElement.GetProperty(CampoDetail).GetString());
            Assert.Equal(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.ErroCampoInvalido, CampoCorpo), documento.RootElement.GetProperty(CampoErros).GetProperty(CampoCorpo)[0].GetString());
            Assert.Equal(0, fabrica.ArmazenamentoFalso.UploadCount);
        }
    }
}
