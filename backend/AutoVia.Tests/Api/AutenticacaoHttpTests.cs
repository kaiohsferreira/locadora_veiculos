using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes de ponta a ponta da autenticacao com o servidor em memoria (WebApplicationFactory
    /// sobre Program). Usa configuracao em memoria (chave JWT gerada no teste, conexao fictícia)
    /// e troca o repositorio de login por um fake: nenhum PostgreSQL e acessado.
    /// Cobre 401 sem token, 403 por perfil, 200 com token valido, token expirado ou de outra
    /// chave, fallback, login e ida e volta do NameIdentifier, e o limite de requisicoes (429).
    /// </summary>
    public class AutenticacaoHttpTests : IClassFixture<AutenticacaoWebFactory>
    {
        private const string CpfCliente = "52998224725";
        private const string CpfCliente_Mascarado = "529.982.247-25";
        private const string SenhaCliente = "SenhaCliente#2026";
        private const string CpfFuncionario = "11144477735";
        private const string SenhaFuncionario = "SenhaFunc#2026";
        private const string CpfInativo = "98765432100";
        private const string RotaFuncionario = "/api/teste-autorizacao/funcionario";
        private const string RotaCliente = "/api/teste-autorizacao/cliente";
        private const string RotaSemAtributo = "/api/teste-autorizacao/sem-atributo";
        private const string RotaIdentidade = "/api/teste-autorizacao/identidade";
        private const string RotaLogin = "/api/auth/login";
        private const string MediaProblema = "application/problem+json";

        private readonly AutenticacaoWebFactory _fabrica;
        private readonly HttpClient _cliente;

        public AutenticacaoHttpTests(AutenticacaoWebFactory fabrica)
        {
            _fabrica = fabrica;
            _cliente = fabrica.CreateClient();
        }

        private string GerarToken(PerfilUsuario perfil, CargoFuncionario? cargo, int expiracaoMinutos, string chave)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = AutenticacaoWebFactory.Emissor,
                Audiencia = AutenticacaoWebFactory.Audiencia,
                ExpiracaoMinutos = expiracaoMinutos,
                Chave = chave
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            string token = tokens.GerarToken(perfil, 1, "Usuario de teste", cargo).Token;

            return token;
        }

        private string TokenDeCliente()
        {
            return GerarToken(PerfilUsuario.CLIENTE, null, 120, _fabrica.ChaveJwt);
        }

        private string TokenDeFuncionario()
        {
            return GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE, 120, _fabrica.ChaveJwt);
        }

        private static HttpRequestMessage Requisicao(HttpMethod metodo, string rota, string? token)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(metodo, rota);

            if (!string.IsNullOrEmpty(token))
            {
                requisicao.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, token);
            }

            return requisicao;
        }

        private static StringContent CorpoJson(object corpo)
        {
            return new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        [Fact]
        public async Task SemToken_RotaDeFuncionario_Devolve401ProblemComWwwAuthenticate()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaFuncionario, null));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Contains(resposta.Headers.WwwAuthenticate, cabecalho => cabecalho.Scheme == ConstantsValues.TipoTokenBearer);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloNaoAutenticado, corpo.GetProperty("title").GetString());
        }

        [Fact]
        public async Task TokenDeCliente_RotaDeFuncionario_Devolve403Problem()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaFuncionario, TokenDeCliente()));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
        }

        [Fact]
        public async Task TokenDeFuncionario_RotaDeFuncionario_Devolve200()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaFuncionario, TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task TokenDeCliente_RotaDeCliente_Devolve200()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaCliente, TokenDeCliente()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        }

        [Fact]
        public async Task TokenExpirado_Devolve401()
        {
            string expirado = GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE, -10, _fabrica.ChaveJwt);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaFuncionario, expirado));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task TokenDeOutraChave_Devolve401()
        {
            string outraChave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            string token = GerarToken(PerfilUsuario.FUNCIONARIO, CargoFuncionario.ATENDENTE, 120, outraChave);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaFuncionario, token));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        [Fact]
        public async Task RotaSemAtributo_CaiNaFallbackEExigeAutenticacao()
        {
            HttpResponseMessage semToken = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaSemAtributo, null));
            HttpResponseMessage comToken = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaSemAtributo, TokenDeCliente()));

            Assert.Equal(HttpStatusCode.Unauthorized, semToken.StatusCode);
            Assert.Equal(HttpStatusCode.OK, comToken.StatusCode);
        }

        [Fact]
        public async Task Login_ClienteValido_DevolveTokenQueIdentificaOUsuarioNoPipeline()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = CpfCliente_Mascarado, senha = SenhaCliente }));

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement retorno = await LerJsonAsync(login);
            Assert.Equal(ConstantsValues.TipoTokenBearer, retorno.GetProperty("tipoToken").GetString());
            Assert.Equal("CLIENTE", retorno.GetProperty("perfil").GetString());
            string token = retorno.GetProperty("token").GetString()!;
            int idUsuario = retorno.GetProperty("idUsuario").GetInt32();

            HttpResponseMessage identidade = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));

            Assert.Equal(HttpStatusCode.OK, identidade.StatusCode);
            JsonElement corpoIdentidade = await LerJsonAsync(identidade);
            Assert.Equal(idUsuario.ToString(), corpoIdentidade.GetProperty("idUsuario").GetString());
        }

        [Fact]
        public async Task Login_FuncionarioValido_DevolveCargo()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "FUNCIONARIO", cpf = CpfFuncionario, senha = SenhaFuncionario }));

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement retorno = await LerJsonAsync(login);
            Assert.Equal("ATENDENTE", retorno.GetProperty("cargo").GetString());
        }

        [Fact]
        public async Task Login_CpfInexistente_Devolve401ComMensagemPadrao()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = "11122233344", senha = SenhaCliente }));

            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
            Assert.Equal(MediaProblema, login.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Login_SenhaErrada_Devolve401ComAMesmaMensagem()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = CpfCliente, senha = "senha-errada" }));

            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Login_FuncionarioInativo_Devolve401()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "FUNCIONARIO", cpf = CpfInativo, senha = SenhaFuncionario }));

            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task Login_PerfilInvalido_Devolve400ProblemJson()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "OUTRO", cpf = CpfCliente, senha = SenhaCliente }));

            Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
            Assert.Equal(MediaProblema, login.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Login_CamposVaziosPelaValidacao_Devolve400ProblemJson()
        {
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = "123", senha = "" }));

            Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
            Assert.Equal(MediaProblema, login.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(ConstantsMessage.ErroLoginCpfFormato, corpo.GetProperty("detail").GetString());
        }

        [Fact]
        public void ChaveJwtCurta_ImpedeASubidaDaApi()
        {
            using AutenticacaoWebFactoryChaveCurta fabricaChaveCurta = new AutenticacaoWebFactoryChaveCurta();

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabricaChaveCurta.CreateClient());

            Assert.Contains(string.Format(ConstantsMessage.ErroConfiguracaoJwt, ConstantsValues.TamanhoMinimoJwtChave), excecao.ToString());
        }

        [Fact]
        public async Task Login_AcimaDoLimitePorIp_Devolve429ProblemJson()
        {
            using AutenticacaoWebFactoryLimitado fabricaLimitada = new AutenticacaoWebFactoryLimitado();
            HttpClient cliente = fabricaLimitada.CreateClient();
            HttpResponseMessage ultima = null!;

            for (int tentativa = 0; tentativa < 3; tentativa++)
            {
                ultima = await cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = "123", senha = "" }));
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, ultima.StatusCode);
            Assert.Equal(MediaProblema, ultima.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(ultima);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty("detail").GetString());
        }
    }

    /// <summary>
    /// Servidor de teste: configuracao em memoria (chave JWT gerada no teste, limite
    /// configuravel), repositorio de login trocado pelo fake com as contas de teste e o
    /// controller de sondagem que expoe rotas com cada tipo de autorizacao.
    /// </summary>
    public class AutenticacaoWebFactory : WebApplicationFactory<Program>
    {
        public const string Emissor = "AutoVia.Api";
        public const string Audiencia = "AutoVia.Web";

        private const string CpfClienteDeTeste = "52998224725";
        private const string SenhaClienteDeTeste = "SenhaCliente#2026";
        private const string CpfFuncionarioDeTeste = "11144477735";
        private const string SenhaFuncionarioDeTeste = "SenhaFunc#2026";
        private const string CpfInativoDeTeste = "98765432100";

        private readonly int _limitePublico;

        /// <summary>Servidor de teste com limite alto, para os testes funcionais nao baterem no 429.</summary>
        public AutenticacaoWebFactory()
            : this(1000, null)
        {
        }

        /// <summary>Servidor de teste com o limite por IP e a chave JWT informados (nula = chave gerada no teste).</summary>
        protected AutenticacaoWebFactory(int limitePublico, string? chaveJwt)
        {
            _limitePublico = limitePublico;
            ChaveJwt = chaveJwt ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            Repositorio = new FakeAutenticacaoRepository();
            SemearContas();
        }

        /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada execucao).</summary>
        public string ChaveJwt { get; }

        /// <summary>Repositorio de login em memoria usado pelo servidor de teste.</summary>
        public FakeAutenticacaoRepository Repositorio { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((contexto, configuracao) =>
            {
                Dictionary<string, string?> valores = new Dictionary<string, string?>
                {
                    { "Jwt:Emissor", Emissor },
                    { "Jwt:Audiencia", Audiencia },
                    { "Jwt:ExpiracaoMinutos", "120" },
                    { "Jwt:Chave", ChaveJwt },
                    { "Autenticacao:LimitePublicoPorMinuto", _limitePublico.ToString(CultureInfo.InvariantCulture) },
                    { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                };

                configuracao.AddInMemoryCollection(valores);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAutenticacaoRepository>();
                services.AddSingleton<IAutenticacaoRepository>(Repositorio);
                services.AddControllers().AddApplicationPart(typeof(ProbeAutorizacaoController).Assembly);
            });
        }

        private void SemearContas()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());

            Repositorio.AdicionarCliente(new Cliente
            {
                Nome = "Maria Cliente",
                Cpf = CpfClienteDeTeste,
                SenhaHash = hasher.Hash(SenhaClienteDeTeste),
                StatusCadastro = StatusCadastroCliente.PENDENTE
            });

            Repositorio.AdicionarFuncionario(new Funcionario
            {
                Nome = "Ana Funcionaria",
                Cpf = CpfFuncionarioDeTeste,
                Matricula = "F-001",
                Cargo = CargoFuncionario.ATENDENTE,
                Ativo = true,
                SenhaHash = hasher.Hash(SenhaFuncionarioDeTeste)
            });

            Repositorio.AdicionarFuncionario(new Funcionario
            {
                Nome = "Carlos Inativo",
                Cpf = CpfInativoDeTeste,
                Matricula = "F-002",
                Cargo = CargoFuncionario.FINANCEIRO,
                Ativo = false,
                SenhaHash = hasher.Hash(SenhaFuncionarioDeTeste)
            });
        }
    }

    /// <summary>Servidor de teste com limite de 2 requisicoes por minuto por IP, para testar o 429.</summary>
    public sealed class AutenticacaoWebFactoryLimitado : AutenticacaoWebFactory
    {
        public AutenticacaoWebFactoryLimitado()
            : base(2, null)
        {
        }
    }

    /// <summary>Servidor de teste com chave JWT curta (menos de 32 caracteres), para provar que a API nao sobe.</summary>
    public sealed class AutenticacaoWebFactoryChaveCurta : AutenticacaoWebFactory
    {
        public AutenticacaoWebFactoryChaveCurta()
            : base(1000, "chave-curta")
        {
        }
    }

    /// <summary>
    /// Controller de sondagem SO para os testes: uma rota por tipo de autorizacao (funcionario,
    /// cliente, sem atributo para testar a FallbackPolicy e identidade para ler o NameIdentifier).
    /// </summary>
    [ApiController]
    [Route("api/teste-autorizacao")]
    public class ProbeAutorizacaoController : ControllerBase
    {
        [HttpGet("funcionario")]
        [Authorize(Policy = ConstantsValues.PoliticaFuncionario)]
        public IActionResult Funcionario()
        {
            return Ok(new { ok = true });
        }

        [HttpGet("cliente")]
        [Authorize(Policy = ConstantsValues.PoliticaCliente)]
        public IActionResult Cliente()
        {
            return Ok(new { ok = true });
        }

        [HttpGet("sem-atributo")]
        public IActionResult SemAtributo()
        {
            return Ok(new { ok = true });
        }

        [HttpGet("identidade")]
        [Authorize]
        public IActionResult Identidade()
        {
            string? idUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Ok(new { idUsuario });
        }
    }
}
