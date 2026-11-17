using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Teste HTTP REAL do teto de corpo ([RequestSizeLimit]): sobe o Kestrel em loopback com porta dinamica, porque o
    /// TestServer nao aplica o teto. Cobre uma rota JSON (verificacao de cadastro, teto de 1024 bytes) e a rota multipart
    /// de avarias (teto de 10.551.296 bytes). Prova que corpo acima do teto responde 400 em problem+json, com a mensagem
    /// de requisicao invalida do projeto, errors.corpo, sem texto do framework e sem log de erro.
    /// </summary>
    public class LimiteCorpoKestrelHttpTests
    {
        private const string RotaVerificacao = "/api/clientes/verificar-cadastro";
        private const string RotaLogin = "/api/auth/login";
        private const string RotaAvarias = "/api/vistorias/1/avarias";
        private const string RotaFotosVeiculo = "/api/veiculos/1/fotos";
        private const string PerfilFuncionario = "FUNCIONARIO";
        private const string CpfFuncionario = "11144477735";
        private const string SenhaFuncionario = "SenhaFunc#2026";
        private const string CampoErros = "errors";
        private const string CampoCorpo = "corpo";
        private const string CampoDetail = "detail";
        private const string TipoProblema = "application/problem+json";
        private const string TextoDoFramework = "Request body";
        private const string CampoJsonValor = "valor";
        private const string CampoJsonCampo = "campo";
        private const string CampoJsonCpf = "CPF";
        private const string FotoDeTeste = "foto.jpg";
        private const string TipoJpeg = "image/jpeg";
        private const string Descricao = "Arranhao na porta";
        private const string Gravidade = "LEVE";

        /// <summary>Servidor de teste REAL: Kestrel em porta dinamica, com o log trocado por um fake para contar as gravacoes.</summary>
        public sealed class LimiteCorpoKestrelWebFactory : AutenticacaoWebFactory
        {
            public LimiteCorpoKestrelWebFactory()
                : base(1000, null)
            {
                UseKestrel(0);
            }

            /// <summary>Log do servidor de teste: o teste conta as gravacoes feitas durante a requisicao.</summary>
            public FakeLogService Log { get; } = new FakeLogService();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ILogService>();
                    services.AddSingleton<ILogService>(Log);
                });
            }
        }

        /// <summary>Sobe o servidor real e devolve o cliente HTTP apontando para ele.</summary>
        private static HttpClient IniciarServidor(LimiteCorpoKestrelWebFactory fabrica)
        {
            fabrica.StartServer();

            return fabrica.CreateClient();
        }

        /// <summary>Confere o 400 do contrato: problem+json, detail = ErroRequisicaoInvalida, errors.corpo, sem texto do framework e sem log novo.</summary>
        private static async Task AssertErroDoCorpoAsync(HttpResponseMessage resposta, LimiteCorpoKestrelWebFactory fabrica, int registrosAntes)
        {
            string textoResposta = await resposta.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(TipoProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain(TextoDoFramework, textoResposta, StringComparison.OrdinalIgnoreCase);

            using JsonDocument documento = JsonDocument.Parse(textoResposta);
            JsonElement raiz = documento.RootElement;

            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, raiz.GetProperty(CampoDetail).GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, CampoCorpo), raiz.GetProperty(CampoErros).GetProperty(CampoCorpo)[0].GetString());
            Assert.Equal(registrosAntes, fabrica.Log.Snapshot().Count);
        }

        [Fact]
        public async Task VerificarCadastro_CorpoAcimaDoTetoNoKestrel_Devolve400ComErrosDoCorpo()
        {
            using LimiteCorpoKestrelWebFactory fabrica = new LimiteCorpoKestrelWebFactory();
            using HttpClient cliente = IniciarServidor(fabrica);
            string valorGrande = new string('1', ConstantsValues.TamanhoMaximoCorpoVerificacaoCadastro);
            string corpo = "{\"" + CampoJsonCampo + "\":\"" + CampoJsonCpf + "\",\"" + CampoJsonValor + "\":\"" + valorGrande + "\"}";
            int registrosAntes = fabrica.Log.Snapshot().Count;

            using StringContent conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");
            using HttpResponseMessage resposta = await cliente.PostAsync(RotaVerificacao, conteudo);

            await AssertErroDoCorpoAsync(resposta, fabrica, registrosAntes);
        }

        [Fact]
        public async Task CriarAvaria_CorpoMultipartAcimaDoTetoNoKestrel_Devolve400ComErrosDoCorpo()
        {
            using LimiteCorpoKestrelWebFactory fabrica = new LimiteCorpoKestrelWebFactory();
            using HttpClient cliente = IniciarServidor(fabrica);
            string token = await LogarFuncionarioAsync(cliente);
            int registrosAntes = fabrica.Log.Snapshot().Count;

            using MultipartFormDataContent formulario = new MultipartFormDataContent();
            formulario.Add(new StringContent(Descricao, Encoding.UTF8), "descricao");
            formulario.Add(new StringContent(Gravidade, Encoding.UTF8), "gravidade");
            ByteArrayContent foto = new ByteArrayContent(new byte[ConstantsValues.TamanhoMaximoCorpoFotoAvaria + 1]);
            foto.Headers.ContentType = new MediaTypeHeaderValue(TipoJpeg);
            formulario.Add(foto, "foto", FotoDeTeste);

            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaAvarias) { Content = formulario };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            requisicao.Headers.ExpectContinue = true;

            using HttpResponseMessage resposta = await cliente.SendAsync(requisicao);

            await AssertErroDoCorpoAsync(resposta, fabrica, registrosAntes);
        }

        [Fact]
        public async Task EnviarFotoVeiculo_CorpoMultipartAcimaDoTetoNoKestrel_Devolve400ComErrosDoCorpo()
        {
            using LimiteCorpoKestrelWebFactory fabrica = new LimiteCorpoKestrelWebFactory();
            using HttpClient cliente = IniciarServidor(fabrica);
            string token = await LogarFuncionarioAsync(cliente);
            int registrosAntes = fabrica.Log.Snapshot().Count;

            using MultipartFormDataContent formulario = new MultipartFormDataContent();
            ByteArrayContent foto = new ByteArrayContent(new byte[ConstantsValues.TamanhoMaximoCorpoFotoVeiculo + 1]);
            foto.Headers.ContentType = new MediaTypeHeaderValue(TipoJpeg);
            formulario.Add(foto, ConstantsValues.CampoFotoVeiculo, FotoDeTeste);

            using HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, RotaFotosVeiculo) { Content = formulario };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            requisicao.Headers.ExpectContinue = true;

            using HttpResponseMessage resposta = await cliente.SendAsync(requisicao);

            await AssertErroDoCorpoAsync(resposta, fabrica, registrosAntes);
        }

        /// <summary>Login real do funcionario de teste: devolve o token do cabecalho Authorization.</summary>
        private static async Task<string> LogarFuncionarioAsync(HttpClient cliente)
        {
            string corpo = JsonSerializer.Serialize(new { perfil = PerfilFuncionario, cpf = CpfFuncionario, senha = SenhaFuncionario });
            using StringContent conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");
            using HttpResponseMessage login = await cliente.PostAsync(RotaLogin, conteudo);
            string textoLogin = await login.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            using JsonDocument documento = JsonDocument.Parse(textoLogin);

            return documento.RootElement.GetProperty("token").GetString()!;
        }
    }
}
