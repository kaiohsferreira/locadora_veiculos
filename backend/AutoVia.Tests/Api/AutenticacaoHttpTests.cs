using AutoVia.Api.Controllers;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes de ponta a ponta da autenticacao e do cadastro do cliente com o servidor em
    /// memoria (WebApplicationFactory sobre Program). Usa configuracao em memoria (chave JWT
    /// gerada no teste, conexao ficticia, origem do frontend liberada) e troca os repositorios
    /// de login e de cliente por fakes: nenhum PostgreSQL e acessado.
    /// Cobre 401 sem token, 403 por perfil, 200 com token valido, token expirado ou de outra
    /// chave, fallback, login, o limite de requisicoes (429), a rota real de clientes
    /// (GET com token de cliente e de funcionario, POST anonimo com 201, CPF repetido com 409),
    /// o cabecalho de CORS nas respostas de erro e a politica de limite do POST por reflexao.
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
        private const string SubNaoNumerico = "sub-nao-numerico";
        private const string VersaoSessaoQualquer = "versao-de-teste";
        private const string NomeDeTeste = "Usuario de teste";
        private const string RoleDesconhecida = "SUPERUSUARIO";
        private const string MensagemFalhaSessao = "Falha simulada na consulta da sessao.";
        private const int IdSessaoDeTeste = 777001;
        private const string RotaLogin = "/api/auth/login";
        private const string RotaClientes = "/api/clientes";
        private const string MediaProblema = "application/problem+json";
        private const string OrigemFrontend = ConstantsValues.OrigemFrontendLocal;
        private const string CpfNovoCliente = "39053344705";
        private const string CnhNovoCliente = "20000000001";
        private const string CpfDuplicado = "16899535009";
        private const string CnhDuplicado = "30000000001";
        private const string CpfNovoLimite1 = "86288366757";
        private const string CnhNovoLimite1 = "20000000002";
        private const string CpfNovoLimite2 = "12345678909";
        private const string CnhNovoLimite2 = "20000000003";
        private const string SenhaClienteNovo = "Senha1234Http";
        private const string CpfDesativadoHttp = "12312312312";
        private const string CpfSenhaTrocadaHttp = "45645645645";
        private const string SenhaAntigaHttp = "SenhaAntiga1Http";
        private const string SenhaNovaHttp = "SenhaNova2Http";

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
            string senhaHash = perfil == PerfilUsuario.CLIENTE ? _fabrica.HashSenhaCliente : _fabrica.HashSenhaFuncionario;
            string versaoSessao = tokens.CalcularVersaoSessao(senhaHash);
            string token = tokens.GerarToken(perfil, 1, "Usuario de teste", cargo, versaoSessao, false).Token;

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

        /// <summary>Token de um usuario com a versao da sessao calculada a partir do hash informado (o mesmo que o servidor confere).</summary>
        private string GerarTokenPara(PerfilUsuario perfil, int idUsuario, string senhaHash, CargoFuncionario? cargo, string chave, int expiracaoMinutos)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = AutenticacaoWebFactory.Emissor,
                Audiencia = AutenticacaoWebFactory.Audiencia,
                ExpiracaoMinutos = expiracaoMinutos,
                Chave = chave
            };

            TokenService tokens = new TokenService(opcoes, new FakeLogService());
            string versaoSessao = tokens.CalcularVersaoSessao(senhaHash);

            return tokens.GerarToken(perfil, idUsuario, "Usuario de teste", cargo, versaoSessao, false).Token;
        }

        /// <summary>Token assinado com a chave do servidor, mas sem a claim sv (emitido antes da versao da sessao).</summary>
        private string TokenSemVersaoDeSessao(PerfilUsuario perfil, int idUsuario)
        {
            Dictionary<string, object> claims = new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, idUsuario.ToString(CultureInfo.InvariantCulture) },
                { ConstantsValues.ClaimRole, perfil.ToString() },
                { ConstantsValues.ClaimNome, "Sem versao de sessao" },
                { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
            };

            SigningCredentials credenciais = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_fabrica.ChaveJwt)),
                SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descritor = new SecurityTokenDescriptor
            {
                Issuer = AutenticacaoWebFactory.Emissor,
                Audience = AutenticacaoWebFactory.Audiencia,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddMinutes(120),
                Claims = claims,
                SigningCredentials = credenciais
            };

            JsonWebTokenHandler handler = new JsonWebTokenHandler();

            return handler.CreateToken(descritor);
        }

        /// <summary>Descarta no servidor de teste o resultado em cache da sessao do usuario (o que a troca de senha e a desativacao fazem).</summary>
        private void InvalidarSessao(PerfilUsuario perfil, int idUsuario)
        {
            using IServiceScope escopo = _fabrica.Services.CreateScope();
            ISessaoService sessao = escopo.ServiceProvider.GetRequiredService<ISessaoService>();
            sessao.Invalidar(perfil, idUsuario);
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

        private static HttpRequestMessage RequisicaoComCorpo(string rota, object corpo)
        {
            HttpRequestMessage requisicao = new HttpRequestMessage(HttpMethod.Post, rota);
            requisicao.Content = CorpoJson(corpo);

            return requisicao;
        }

        /// <summary>Marca a requisicao com a origem do frontend, para o CORS responder com Access-Control-Allow-Origin.</summary>
        private static HttpRequestMessage ComOrigem(HttpRequestMessage requisicao)
        {
            requisicao.Headers.Add(HeaderNames.Origin, OrigemFrontend);

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

        /// <summary>Todos os valores de um cabecalho da resposta (vazio se o cabecalho nao veio).</summary>
        private static List<string> ValoresDoCabecalho(HttpResponseMessage resposta, string nome)
        {
            List<string> valores = new List<string>();

            if (resposta.Headers.TryGetValues(nome, out IEnumerable<string>? encontrados) && encontrados != null)
            {
                valores.AddRange(encontrados);
            }

            return valores;
        }

        /// <summary>Corpo do cliente valido para o cadastro publico, com o CPF e a CNH informados.</summary>
        private static object CorpoClienteValido(string cpf, string cnh)
        {
            return new
            {
                nome = "Maria Teste Http",
                cpf,
                dataNascimento = new DateOnly(1990, 5, 20),
                email = "maria.http@exemplo.com",
                telefone = "11987654321",
                cnhNumero = cnh,
                cnhCategoria = "B",
                cnhValidade = DateOnly.FromDateTime(DateTime.Now).AddYears(3),
                senha = SenhaClienteNovo
            };
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
        public async Task Login_ComLembrar_DevolveExpiracaoDe30DiasEOTokenFunciona()
        {
            DateTime antes = DateTime.Now;
            HttpResponseMessage login = await _cliente.PostAsync(RotaLogin, CorpoJson(new { perfil = "CLIENTE", cpf = CpfCliente, senha = SenhaCliente, lembrar = true }));
            DateTime depois = DateTime.Now;

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement retorno = await LerJsonAsync(login);
            DateTime expiraEm = retorno.GetProperty("expiraEm").GetDateTime();
            Assert.InRange(expiraEm, antes.AddDays(30).AddMinutes(-1), depois.AddDays(30).AddMinutes(1));

            string token = retorno.GetProperty("token").GetString()!;
            HttpResponseMessage identidade = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));
            Assert.Equal(HttpStatusCode.OK, identidade.StatusCode);
        }

        [Fact]
        public async Task ContaDesativada_DerrubaATokenComDesativacaoEInvalidacao()
        {
            Cliente cliente = new Cliente { Nome = "Cliente Desativado Http", Cpf = CpfDesativadoHttp, SenhaHash = _fabrica.HashSenhaCliente, StatusCadastro = StatusCadastroCliente.APROVADO };
            _fabrica.Repositorio.AdicionarCliente(cliente);
            string token = GerarTokenPara(PerfilUsuario.CLIENTE, cliente.IdCliente, _fabrica.HashSenhaCliente, null, _fabrica.ChaveJwt, 120);

            HttpResponseMessage antes = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));
            cliente.Disable();
            InvalidarSessao(PerfilUsuario.CLIENTE, cliente.IdCliente);
            HttpResponseMessage depois = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));

            Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
            Assert.Equal(MediaProblema, depois.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task SenhaTrocada_DerrubaOTokenAntigoAposInvalidacao()
        {
            string hashAntigo = new SenhaHasher(new FakeLogService()).Hash(SenhaAntigaHttp);
            Cliente cliente = new Cliente { Nome = "Cliente Senha Trocada Http", Cpf = CpfSenhaTrocadaHttp, SenhaHash = hashAntigo, StatusCadastro = StatusCadastroCliente.APROVADO };
            _fabrica.Repositorio.AdicionarCliente(cliente);
            string tokenAntigo = GerarTokenPara(PerfilUsuario.CLIENTE, cliente.IdCliente, hashAntigo, null, _fabrica.ChaveJwt, 120);

            HttpResponseMessage antes = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, tokenAntigo));
            cliente.SenhaHash = new SenhaHasher(new FakeLogService()).Hash(SenhaNovaHttp);
            InvalidarSessao(PerfilUsuario.CLIENTE, cliente.IdCliente);
            HttpResponseMessage depois = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, tokenAntigo));

            Assert.Equal(HttpStatusCode.OK, antes.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, depois.StatusCode);
            Assert.Equal(MediaProblema, depois.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task TokenSemVersaoDeSessao_Devolve401()
        {
            string token = TokenSemVersaoDeSessao(PerfilUsuario.CLIENTE, 1);

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>Fail-closed do evento de sessao: sub que nao e numero recusa com 401 (nunca 500 nem 200).</summary>
        [Fact]
        public async Task TokenComSubNaoNumerico_Devolve401()
        {
            string token = AssinarComClaims(new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, SubNaoNumerico },
                { ConstantsValues.ClaimRole, PerfilUsuario.CLIENTE.ToString() },
                { ConstantsValues.ClaimVersaoSessao, VersaoSessaoQualquer },
                { ConstantsValues.ClaimNome, NomeDeTeste },
                { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
            });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        /// <summary>Fail-closed do evento de sessao: role fora de CLIENTE e FUNCIONARIO recusa com 401.</summary>
        [Fact]
        public async Task TokenComRoleDesconhecida_Devolve401()
        {
            string token = AssinarComClaims(new Dictionary<string, object>
            {
                { JwtRegisteredClaimNames.Sub, IdSessaoDeTeste.ToString(CultureInfo.InvariantCulture) },
                { ConstantsValues.ClaimRole, RoleDesconhecida },
                { ConstantsValues.ClaimVersaoSessao, VersaoSessaoQualquer },
                { ConstantsValues.ClaimNome, NomeDeTeste },
                { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
            });

            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        /// <summary>
        /// Falha ao conferir a sessao (aqui, a consulta do banco por trás do ISessaoService real) recusa com 401 e nao
        /// deixa passar nem vira 500. Usa um id sem cadastro para o resultado nao vir do cache de outro teste.
        /// </summary>
        [Fact]
        public async Task FalhaAoConferirASessao_Devolve401_NaoServidorErroNemLibera()
        {
            string token = GerarTokenPara(PerfilUsuario.CLIENTE, IdSessaoDeTeste, _fabrica.HashSenhaCliente, null, _fabrica.ChaveJwt, 120);
            _fabrica.Repositorio.ExcecaoAoBuscar = new InvalidOperationException(MensagemFalhaSessao);

            HttpResponseMessage resposta;
            try
            {
                resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaIdentidade, token));
            }
            finally
            {
                _fabrica.Repositorio.ExcecaoAoBuscar = null;
            }

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        }

        /// <summary>Token assinado com a chave do servidor e as claims informadas (usado para claims invalidas).</summary>
        private string AssinarComClaims(Dictionary<string, object> claims)
        {
            SigningCredentials credenciais = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_fabrica.ChaveJwt)),
                SecurityAlgorithms.HmacSha256);

            SecurityTokenDescriptor descritor = new SecurityTokenDescriptor
            {
                Issuer = AutenticacaoWebFactory.Emissor,
                Audience = AutenticacaoWebFactory.Audiencia,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddMinutes(120),
                Claims = claims,
                SigningCredentials = credenciais
            };

            JsonWebTokenHandler handler = new JsonWebTokenHandler();

            return handler.CreateToken(descritor);
        }

        [Fact]
        public void LembrarDeMimForaDaFaixa_ImpedeASubidaDaApi()
        {
            using AutenticacaoWebFactoryLembrarInvalido fabricaInvalida = new AutenticacaoWebFactoryLembrarInvalido();

            Exception excecao = Assert.ThrowsAny<Exception>(() => fabricaInvalida.CreateClient());

            Assert.Contains(ConstantsMessage.ErroConfiguracaoSessao, excecao.ToString());
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

        [Fact]
        public async Task GetClientes_TokenDeCliente_Devolve403ProblemComCors()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(ComOrigem(Requisicao(HttpMethod.Get, RotaClientes, TokenDeCliente())));

            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.TituloAcessoNegado, corpo.GetProperty("title").GetString());
            Assert.Contains(OrigemFrontend, ValoresDoCabecalho(resposta, HeaderNames.AccessControlAllowOrigin));
        }

        [Fact]
        public async Task GetClientes_TokenDeFuncionario_Devolve200ComPagina()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(Requisicao(HttpMethod.Get, RotaClientes, TokenDeFuncionario()));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(JsonValueKind.Array, corpo.GetProperty("itens").ValueKind);
            Assert.Equal(JsonValueKind.Number, corpo.GetProperty("total").ValueKind);
        }

        [Fact]
        public async Task GetClientes_SemToken_Devolve401ComCorsEWwwAuthenticate()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(ComOrigem(Requisicao(HttpMethod.Get, RotaClientes, null)));

            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            Assert.Contains(resposta.Headers.WwwAuthenticate, cabecalho => cabecalho.Scheme == ConstantsValues.TipoTokenBearer);
            Assert.Contains(OrigemFrontend, ValoresDoCabecalho(resposta, HeaderNames.AccessControlAllowOrigin));
            Assert.Contains(HeaderNames.Location, ValoresDoCabecalho(resposta, HeaderNames.AccessControlExposeHeaders));
        }

        [Fact]
        public async Task PostClientes_Anonimo_Devolve201PendenteComLocation()
        {
            HttpResponseMessage resposta = await _cliente.SendAsync(RequisicaoComCorpo(RotaClientes, CorpoClienteValido(CpfNovoCliente, CnhNovoCliente)));

            Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal("PENDENTE", corpo.GetProperty("statusCadastro").GetString());
            Assert.False(corpo.TryGetProperty("senha", out _));
            Assert.False(corpo.TryGetProperty("senhaHash", out _));
            int idCliente = corpo.GetProperty("idCliente").GetInt32();
            Uri? location = resposta.Headers.Location;
            Assert.NotNull(location);
            Assert.Equal(RotaClientes + "/" + idCliente.ToString(CultureInfo.InvariantCulture), location.AbsolutePath);
        }

        [Fact]
        public async Task PostClientes_CpfDeClienteAtivo_Devolve409ComCors()
        {
            _fabrica.RepositorioClientes.Adicionar(new Cliente
            {
                Nome = "Cliente ja cadastrado",
                Cpf = CpfDuplicado,
                DataNascimento = new DateOnly(1980, 1, 1),
                Email = "ja.cadastrado@exemplo.com",
                Telefone = "11911112222",
                CnhNumero = CnhDuplicado,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = new DateOnly(2031, 1, 1),
                StatusCadastro = StatusCadastroCliente.APROVADO
            });

            HttpResponseMessage resposta = await _cliente.SendAsync(ComOrigem(RequisicaoComCorpo(RotaClientes, CorpoClienteValido(CpfDuplicado, CnhNovoCliente))));

            Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(ConstantsMessage.ErroClienteCpfDuplicado, corpo.GetProperty("detail").GetString());
            Assert.Contains(OrigemFrontend, ValoresDoCabecalho(resposta, HeaderNames.AccessControlAllowOrigin));
        }

        [Fact]
        public async Task PostClientes_AcimaDoLimitePorIp_Devolve429ComCors()
        {
            using AutenticacaoWebFactoryLimitado fabricaLimitada = new AutenticacaoWebFactoryLimitado();
            HttpClient cliente = fabricaLimitada.CreateClient();

            HttpResponseMessage primeira = await cliente.SendAsync(RequisicaoComCorpo(RotaClientes, CorpoClienteValido(CpfNovoLimite1, CnhNovoLimite1)));
            HttpResponseMessage segunda = await cliente.SendAsync(RequisicaoComCorpo(RotaClientes, CorpoClienteValido(CpfNovoLimite2, CnhNovoLimite2)));
            HttpResponseMessage terceira = await cliente.SendAsync(ComOrigem(RequisicaoComCorpo(RotaClientes, CorpoClienteValido(CpfNovoLimite2, CnhNovoLimite2))));

            Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
            Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, terceira.StatusCode);
            Assert.Equal(MediaProblema, terceira.Content.Headers.ContentType?.MediaType);
            JsonElement corpo = await LerJsonAsync(terceira);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, corpo.GetProperty("detail").GetString());
            Assert.Contains(OrigemFrontend, ValoresDoCabecalho(terceira, HeaderNames.AccessControlAllowOrigin));
        }

        [Fact]
        public void Insert_DeClienteTemRateLimitingComAPoliticaPublica()
        {
            MethodInfo? metodo = typeof(ClienteController).GetMethod(nameof(ClienteController.Insert), BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(metodo);
            EnableRateLimitingAttribute? limite = metodo.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimitePublico, limite.PolicyName);
        }
    }

    /// <summary>
    /// Servidor de teste: configuracao em memoria (chave JWT gerada no teste, limite
    /// configuravel, origem do frontend liberada), repositorios de login e de cliente
    /// trocados pelos fakes com as contas de teste e o controller de sondagem que expoe
    /// rotas com cada tipo de autorizacao.
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

        /// <summary>Hash PBKDF2 da senha do cliente semeado; a versao da sessao (sv) dos tokens de teste sai dele.</summary>
        public string HashSenhaCliente { get; private set; } = string.Empty;

        /// <summary>Hash PBKDF2 da senha da funcionaria ativa semeada.</summary>
        public string HashSenhaFuncionario { get; private set; } = string.Empty;

        /// <summary>Chave JWT usada pelo servidor de teste (gerada a cada execucao).</summary>
        public string ChaveJwt { get; }

        /// <summary>Repositorio de login em memoria usado pelo servidor de teste.</summary>
        public FakeAutenticacaoRepository Repositorio { get; }

        /// <summary>Repositorio de clientes em memoria usado pelo servidor de teste (POST e GET de /api/clientes).</summary>
        public FakeClienteRepository RepositorioClientes { get; } = new FakeClienteRepository();

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
                    // Ambiente Testing nao e Development: a URL publica do front precisa ser https fora de loopback.
                    { "Autenticacao:Recuperacao:UrlPublicaFront", "https://front.exemplo.com" },
                    { "Cors:Origens:0", ConstantsValues.OrigemFrontendLocal },
                    { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                };

                AjustarConfiguracao(valores);
                configuracao.AddInMemoryCollection(valores);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAutenticacaoRepository>();
                services.AddSingleton<IAutenticacaoRepository>(Repositorio);
                services.RemoveAll<IClienteRepository>();
                services.AddSingleton<IClienteRepository>(RepositorioClientes);
                services.AddControllers().AddApplicationPart(typeof(ProbeAutorizacaoController).Assembly);
            });
        }

        /// <summary>Ponto de extensao para os testes trocarem valores de configuracao antes de subir o servidor.</summary>
        protected virtual void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
        }

        private void SemearContas()
        {
            SenhaHasher hasher = new SenhaHasher(new FakeLogService());
            HashSenhaCliente = hasher.Hash(SenhaClienteDeTeste);
            HashSenhaFuncionario = hasher.Hash(SenhaFuncionarioDeTeste);

            Repositorio.AdicionarCliente(new Cliente
            {
                Nome = "Maria Cliente",
                Cpf = CpfClienteDeTeste,
                SenhaHash = HashSenhaCliente,
                StatusCadastro = StatusCadastroCliente.PENDENTE
            });

            Repositorio.AdicionarFuncionario(new Funcionario
            {
                Nome = "Ana Funcionaria",
                Cpf = CpfFuncionarioDeTeste,
                Matricula = "F-001",
                Cargo = CargoFuncionario.ATENDENTE,
                Ativo = true,
                SenhaHash = HashSenhaFuncionario
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

    /// <summary>Servidor de teste com Jwt:ExpiracaoLembrarDias igual a 0 (fora da faixa 1..90), para provar que a API nao sobe.</summary>
    public sealed class AutenticacaoWebFactoryLembrarInvalido : AutenticacaoWebFactory
    {
        public AutenticacaoWebFactoryLembrarInvalido()
            : base(1000, null)
        {
        }

        protected override void AjustarConfiguracao(Dictionary<string, string?> valores)
        {
            valores["Jwt:ExpiracaoLembrarDias"] = "0";
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
