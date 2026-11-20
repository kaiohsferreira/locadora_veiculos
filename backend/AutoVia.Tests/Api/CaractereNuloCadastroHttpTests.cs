using AutoVia.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Caractere nulo (U+0000) no cadastro publico de cliente (POST /api/clientes) e de locadora (POST /api/empresas), pela
    /// rota HTTP (item 5 do plano de pendencias, 20/11/2026): a resposta e 400 com ErroCampoInvalido no campo, e nunca 500.
    /// Servidores em memoria, sem PostgreSQL. Antes da regra, o cadastro com nulo era aceito (201).
    /// </summary>
    public class CaractereNuloCadastroHttpTests : IDisposable
    {
        private const string RotaClientes = "/api/clientes";
        private const string RotaEmpresas = "/api/empresas";
        private const string MediaProblema = "application/problem+json";
        private const string CpfNovoCliente = "39053344705";
        private const string CnhNovoCliente = "20000000002";
        private const string NomeComNulo = "Maria\0 Teste Http";
        private const string RazaoSocialComNulo = "Locadora\0 Teste Ltda";

        private readonly AutenticacaoWebFactory _fabricaCliente;
        private readonly HttpClient _clienteCliente;
        private readonly EmpresaWebFactory _fabricaEmpresa;
        private readonly HttpClient _clienteEmpresa;

        public CaractereNuloCadastroHttpTests()
        {
            _fabricaCliente = new AutenticacaoWebFactory();
            _clienteCliente = _fabricaCliente.CreateClient();
            _fabricaEmpresa = new EmpresaWebFactory(habilitado: true);
            _clienteEmpresa = _fabricaEmpresa.CreateClient();
        }

        public void Dispose()
        {
            _clienteCliente.Dispose();
            _fabricaCliente.Dispose();
            _clienteEmpresa.Dispose();
            _fabricaEmpresa.Dispose();
        }

        [Fact]
        public async Task PostClientes_NomeComCaractereNulo_Devolve400ComErroDoCampoENaoServidor()
        {
            object corpo = new
            {
                nome = NomeComNulo,
                cpf = CpfNovoCliente,
                dataNascimento = new DateOnly(1990, 5, 20),
                email = "maria.nula.http@exemplo.com",
                telefone = "11987654321",
                cnhNumero = CnhNovoCliente,
                cnhCategoria = "B",
                cnhValidade = DateOnly.FromDateTime(DateTime.Now).AddYears(3),
                senha = "SenhaForte1"
            };

            HttpResponseMessage resposta = await _clienteCliente.SendAsync(Json(HttpMethod.Post, RotaClientes, JsonSerializer.Serialize(corpo)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement problema = await LerJsonAsync(resposta);
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "Nome"), problema.GetProperty("detail").GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "Nome"), problema.GetProperty("errors").GetProperty("nome")[0].GetString());
        }

        [Fact]
        public async Task PostEmpresas_RazaoSocialComCaractereNulo_Devolve400ComErroDoCampoENaoServidor()
        {
            object corpo = new { razaoSocial = RazaoSocialComNulo };

            HttpResponseMessage resposta = await _clienteEmpresa.SendAsync(Json(HttpMethod.Post, RotaEmpresas, JsonSerializer.Serialize(corpo)));

            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement problema = await LerJsonAsync(resposta);
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "RazaoSocial"), problema.GetProperty("detail").GetString());
            Assert.Equal(string.Format(ConstantsMessage.ErroCampoInvalido, "RazaoSocial"), problema.GetProperty("errors").GetProperty("razaoSocial")[0].GetString());
        }

        private static HttpRequestMessage Json(HttpMethod metodo, string rota, string json)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);
            requisicao.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return requisicao;
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }
    }
}
