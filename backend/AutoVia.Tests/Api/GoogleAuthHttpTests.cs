using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using AutoVia.Communication.Enums;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes de ponta a ponta do login com Google (N3, RF-51) com o servidor em memoria. O GATEWAY E REAL: a prova
    /// e assinada com as chaves de teste de GoogleProvaDeTeste e o JWKS vem de um IComunicacaoService falso (sem rede).
    /// Repositorios de login e de vinculo sao fakes; o SessaoService e o SenhaHasher sao os reais. Cada cenario de
    /// integracao (sem Client ID, JWKS fora, ambiente de teste, limite baixo) tem a sua fabrica, porque a configuracao
    /// da integracao fica em cache por 300 s. Nenhum PostgreSQL e acessado e nada chama o Google.
    /// </summary>
    public class GoogleAuthHttpTests : IClassFixture<GoogleAuthHttpTests.GoogleWebFactoryPadrao>
    {
        private const string RotaConfiguracao = "/api/auth/google/configuracao";
        private const string RotaLogin = "/api/auth/google/login";
        private const string RotaVinculo = "/api/auth/google/vinculo";
        private const string RotaIdentidade = "/api/teste-autorizacao/identidade";
        private const string MediaProblema = "application/problem+json";
        private const string SenhaPadrao = "SenhaHttp#2026";
        private const string SenhaErrada = "SenhaErrada#2026";
        private const string CampoSituacao = "situacao";
        private const string CampoSessao = "sessao";
        private const string CampoIdUsuario = "idUsuario";
        private const string CampoExpiraEm = "expiraEm";
        private const string CampoTitulo = "title";
        private const string CampoDetalhe = "detail";
        private const string CampoTipo = "type";
        private const string CampoClientId = "clientId";
        private const string CampoHabilitado = "habilitado";
        private const string CampoModoTeste = "modoTeste";
        private const string OutroClientId = "outro-cliente.apps.googleusercontent.com";
        private const string EmailNovo = "novo.email@exemplo.com";
        private const string SubOutraConta = "sub-outra-conta-de-teste";
        private const string CpfInexistente = "30000000098";
        private const string CredencialInvalidaCurta = "";
        private const int TamanhoCredencialAcimaDoLimite = ConstantsValues.TamanhoMaximoCredencialGoogle + 1;
        private const int TamanhoSenhaAcimaDoLimite = ConstantsValues.TamanhoMaximoSenhaLogin + 1;
        private const int ValidadeLembrarDias = 30;
        private const int ExpiracaoCurtaMinutos = 120;
        private const int ToleranciaMinutos = 1;

        /// <summary>Hash PBKDF2 real da senha padrao, calculado uma vez para toda a classe (a verificacao e a real).</summary>
        private static readonly string HashSenhaPadrao = new SenhaHasher(new FakeLogService()).Hash(SenhaPadrao);

        private readonly GoogleWebFactoryPadrao _fabrica;
        private readonly HttpClient _cliente;

        public GoogleAuthHttpTests(GoogleWebFactoryPadrao fabrica)
        {
            _fabrica = fabrica;
            _cliente = fabrica.CreateClient();
        }

        // ------------------------------------------------------------------
        // Fabricas: uma por cenario de integracao
        // ------------------------------------------------------------------

        /// <summary>Servidor padrao: integracao GOOGLE com Client ID real, ambiente Testing, limite alto.</summary>
        public class GoogleWebFactoryPadrao : GoogleWebFactory
        {
            public GoogleWebFactoryPadrao()
                : base(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId, AmbienteTeste, false, LimiteAlto)
            {
            }
        }

        /// <summary>Integracao GOOGLE sem Client ID: o botao fica desabilitado e o login devolve 502.</summary>
        public sealed class GoogleWebFactoryDesabilitada : GoogleWebFactory
        {
            public GoogleWebFactoryDesabilitada()
                : base(ConstantsGoogle.ProvedorGoogle, null, AmbienteTeste, false, LimiteAlto)
            {
            }
        }

        /// <summary>Google indisponivel: o JWKS nao pode ser obtido, entao a prova nao e validada e o login devolve 502.</summary>
        public sealed class GoogleWebFactoryJwksFalha : GoogleWebFactory
        {
            public GoogleWebFactoryJwksFalha()
                : base(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId, AmbienteTeste, true, LimiteAlto)
            {
            }
        }

        /// <summary>Provedor FAKE em Development (SANDBOX): a configuracao publica mostra o modo de teste.</summary>
        public sealed class GoogleWebFactoryTeste : GoogleWebFactory
        {
            public GoogleWebFactoryTeste()
                : base(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId, Environments.Development, false, LimiteAlto)
            {
            }
        }

        /// <summary>Servidor com limite de 2 requisicoes por minuto por IP, para provar o 429 nas rotas publicas.</summary>
        public sealed class GoogleWebFactoryLimitada : GoogleWebFactory
        {
            public const int LimiteDeTeste = 2;

            public GoogleWebFactoryLimitada()
                : base(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId, AmbienteTeste, false, LimiteDeTeste)
            {
            }
        }

        /// <summary>
        /// Servidor de teste do login com Google: integracao FALSA para a rede (IComunicacaoService com o JWKS da chave de teste),
        /// repositorios de login e de vinculo em memoria, log em memoria e a integracao GOOGLE em FakeIntegrationRepository.
        /// </summary>
        public abstract class GoogleWebFactory : WebApplicationFactory<Program>
        {
            public const string Emissor = "AutoVia.Api";
            public const string Audiencia = "AutoVia.Web";
            // Ambiente Testing nao e Development: a URL publica precisa ser https fora de loopback (regra da subida).
            public const string UrlFrontTeste = "https://front.exemplo.com";
            public const string AmbienteTeste = "Testing";
            public const int LimiteAlto = 1000;

            private readonly string _ambiente;
            private readonly int _limite;
            private readonly bool _jwksFalha;

            protected GoogleWebFactory(string provedor, string? clientId, string ambiente, bool jwksFalha, int limite)
            {
                _ambiente = ambiente;
                _jwksFalha = jwksFalha;
                _limite = limite;

                ChaveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
                Repositorio = new FakeAutenticacaoRepository();
                Vinculos = new FakeGoogleVinculoRepository(Repositorio);
                Integracoes = new FakeIntegrationRepository();
                Integracoes.Add(GoogleProvaDeTeste.IntegracaoGoogle(provedor, clientId));
                Comunicacao = new GoogleProvaDeTeste.ComunicacaoGoogleFalsa
                {
                    Jwks = GoogleProvaDeTeste.MontarJwks((GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal)),
                    Falhar = jwksFalha
                };
                Log = new FakeLogService();
            }

            public string ChaveJwt { get; }

            public FakeAutenticacaoRepository Repositorio { get; }

            public FakeGoogleVinculoRepository Vinculos { get; }

            public FakeIntegrationRepository Integracoes { get; }

            public GoogleProvaDeTeste.ComunicacaoGoogleFalsa Comunicacao { get; }

            public FakeLogService Log { get; }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment(_ambiente);

                builder.ConfigureAppConfiguration((contexto, configuracao) =>
                {
                    Dictionary<string, string?> valores = new Dictionary<string, string?>
                    {
                        { "Jwt:Emissor", Emissor },
                        { "Jwt:Audiencia", Audiencia },
                        { "Jwt:ExpiracaoMinutos", "120" },
                        { "Jwt:Chave", ChaveJwt },
                        { "Autenticacao:LimitePublicoPorMinuto", _limite.ToString(CultureInfo.InvariantCulture) },
                        { "Autenticacao:Recuperacao:UrlPublicaFront", UrlFrontTeste },
                        { "Autenticacao:Recuperacao:TempoMinimoRespostaMs", "0" },
                        { "ConnectionStrings:AutoViaConnection", "Host=localhost;Port=1;Database=teste_sem_banco;Username=teste;Password=teste" }
                    };

                    configuracao.AddInMemoryCollection(valores);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IAutenticacaoRepository>();
                    services.AddSingleton<IAutenticacaoRepository>(Repositorio);
                    services.RemoveAll<IGoogleVinculoRepository>();
                    services.AddSingleton<IGoogleVinculoRepository>(Vinculos);
                    services.RemoveAll<IIntegrationRepository>();
                    services.AddSingleton<IIntegrationRepository>(Integracoes);
                    services.RemoveAll<IComunicacaoService>();
                    services.AddSingleton<IComunicacaoService>(Comunicacao);
                    services.RemoveAll<ILogService>();
                    services.AddSingleton<ILogService>(Log);
                    services.AddControllers().AddApplicationPart(typeof(ProbeAutorizacaoController).Assembly);
                });
            }
        }

        // ------------------------------------------------------------------
        // Apoio
        // ------------------------------------------------------------------

        /// <summary>Resumo do ProblemDetails (sem traceId, que muda a cada requisicao).</summary>
        private sealed record ResumoProblema(int Status, string? Tipo, string? Titulo, string? Detalhe);

        private static StringContent Json(object corpo)
        {
            return new StringContent(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");
        }

        private static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta)
        {
            string texto = await resposta.Content.ReadAsStringAsync();
            using JsonDocument documento = JsonDocument.Parse(texto);

            return documento.RootElement.Clone();
        }

        private static string? Texto(JsonElement corpo, string propriedade)
        {
            if (corpo.TryGetProperty(propriedade, out JsonElement valor) && valor.ValueKind == JsonValueKind.String)
            {
                return valor.GetString();
            }

            return null;
        }

        /// <summary>Le o ProblemDetails e confirma o tipo de midia problem+json.</summary>
        private static async Task<ResumoProblema> ProblemaAsync(HttpResponseMessage resposta)
        {
            JsonElement corpo = await LerJsonAsync(resposta);

            Assert.Equal(MediaProblema, resposta.Content.Headers.ContentType?.MediaType);

            return new ResumoProblema((int)resposta.StatusCode, Texto(corpo, CampoTipo), Texto(corpo, CampoTitulo), Texto(corpo, CampoDetalhe));
        }

        /// <summary>Credencial (ID token) assinada pela chave de teste, com o sub e o e-mail informados.</summary>
        private static string CredencialDe(string email, string sub)
        {
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["sub"] = sub;
            claims["email"] = email;

            return GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claims);
        }

        private static Cliente Semear(FakeAutenticacaoRepository repositorio, string cpf, string email, string? googleSub = null, StatusCadastroCliente status = StatusCadastroCliente.APROVADO, bool comSenha = true)
        {
            Cliente cliente = new Cliente
            {
                Nome = "Cliente HTTP " + cpf,
                Cpf = cpf,
                Email = email,
                SenhaHash = comSenha ? HashSenhaPadrao : null,
                StatusCadastro = status,
                GoogleSub = googleSub
            };

            repositorio.AdicionarCliente(cliente);

            return cliente;
        }

        private static Task<HttpResponseMessage> LoginAsync(HttpClient cliente, string credencial, bool lembrar = false)
        {
            return cliente.PostAsync(RotaLogin, Json(new { credencial, lembrar }));
        }

        private static Task<HttpResponseMessage> VinculoAsync(HttpClient cliente, string credencial, string cpf, string senha, bool lembrar = false)
        {
            return cliente.PostAsync(RotaVinculo, Json(new { credencial, cpf, senha, lembrar }));
        }

        private static DateTime DataDoCampo(JsonElement corpo, string propriedade)
        {
            return DateTime.Parse(corpo.GetProperty(propriedade).GetString()!, CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------
        // Configuracao publica (3 estados) e limite
        // ------------------------------------------------------------------

        [Fact]
        public async Task Configuracao_ComClientIdReal_DevolveHabilitadoSemModoTeste()
        {
            HttpResponseMessage resposta = await _cliente.GetAsync(RotaConfiguracao);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty(CampoHabilitado).GetBoolean());
            Assert.Equal(GoogleProvaDeTeste.ClientId, corpo.GetProperty(CampoClientId).GetString());
            Assert.False(corpo.GetProperty(CampoModoTeste).GetBoolean());
        }

        [Fact]
        public async Task Configuracao_SemClientId_DevolveDesabilitadoSemClientId()
        {
            using GoogleWebFactoryDesabilitada fabrica = new GoogleWebFactoryDesabilitada();
            HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.GetAsync(RotaConfiguracao);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.False(corpo.GetProperty(CampoHabilitado).GetBoolean());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(CampoClientId).ValueKind);
        }

        [Fact]
        public async Task Configuracao_ProvedorFakeEmDevelopment_DevolveModoTeste()
        {
            using GoogleWebFactoryTeste fabrica = new GoogleWebFactoryTeste();
            HttpClient cliente = fabrica.CreateClient();

            HttpResponseMessage resposta = await cliente.GetAsync(RotaConfiguracao);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.True(corpo.GetProperty(CampoHabilitado).GetBoolean());
            Assert.True(corpo.GetProperty(CampoModoTeste).GetBoolean());
            Assert.Equal(GoogleProvaDeTeste.ClientId, corpo.GetProperty(CampoClientId).GetString());
        }

        [Fact]
        public async Task LimiteDoLoginEDoVinculo_Devolve429_MasAConfiguracaoNaoTemLimite()
        {
            using GoogleWebFactoryLimitada fabrica = new GoogleWebFactoryLimitada();
            HttpClient cliente = fabrica.CreateClient();
            string credencialInvalida = "credencial-que-nao-chega-a-ser-validada";

            for (int tentativa = 0; tentativa < GoogleWebFactoryLimitada.LimiteDeTeste; tentativa++)
            {
                await LoginAsync(cliente, credencialInvalida);
            }

            HttpResponseMessage login = await LoginAsync(cliente, credencialInvalida);
            HttpResponseMessage vinculo = await VinculoAsync(cliente, credencialInvalida, CpfInexistente, SenhaPadrao);

            Assert.Equal(HttpStatusCode.TooManyRequests, login.StatusCode);
            ResumoProblema problemaLogin = await ProblemaAsync(login);
            Assert.Equal((int)HttpStatusCode.TooManyRequests, problemaLogin.Status);
            Assert.Equal(HttpStatusCode.TooManyRequests, vinculo.StatusCode);

            for (int tentativa = 0; tentativa < GoogleWebFactoryLimitada.LimiteDeTeste + 1; tentativa++)
            {
                HttpResponseMessage configuracao = await cliente.GetAsync(RotaConfiguracao);

                Assert.Equal(HttpStatusCode.OK, configuracao.StatusCode);
            }
        }

        // ------------------------------------------------------------------
        // Fluxos do login e do vinculo (servidor padrao)
        // ------------------------------------------------------------------

        [Fact]
        public async Task PrimeiroAcesso_ConfirmaVinculaEDepoisEntraDireto()
        {
            const string email = "primeiro.acesso@exemplo.com";
            const string sub = "sub-primeiro-acesso";
            const string cpf = "30000000001";
            Cliente cliente = Semear(_fabrica.Repositorio, cpf, email);

            HttpResponseMessage primeiro = await LoginAsync(_cliente, CredencialDe(email, sub));

            Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
            JsonElement corpoPrimeiro = await LerJsonAsync(primeiro);
            Assert.Equal(SituacaoLoginGoogle.CONFIRMACAO_NECESSARIA.ToString(), corpoPrimeiro.GetProperty(CampoSituacao).GetString());
            Assert.Equal(JsonValueKind.Null, corpoPrimeiro.GetProperty(CampoSessao).ValueKind);
            Assert.Equal(GoogleProvaDeTeste.NomeTeste, corpoPrimeiro.GetProperty("nome").GetString());
            Assert.Equal(email, corpoPrimeiro.GetProperty("email").GetString());
            Assert.Null(cliente.GoogleSub);

            HttpResponseMessage vinculo = await VinculoAsync(_cliente, CredencialDe(email, sub), cpf, SenhaPadrao);

            Assert.Equal(HttpStatusCode.OK, vinculo.StatusCode);
            JsonElement corpoVinculo = await LerJsonAsync(vinculo);
            Assert.Equal(PerfilUsuario.CLIENTE.ToString(), corpoVinculo.GetProperty("perfil").GetString());
            Assert.Equal(cliente.IdCliente, corpoVinculo.GetProperty(CampoIdUsuario).GetInt32());
            Assert.Equal(sub, cliente.GoogleSub);

            HttpResponseMessage segundo = await LoginAsync(_cliente, CredencialDe(email, sub));

            Assert.Equal(HttpStatusCode.OK, segundo.StatusCode);
            JsonElement corpoSegundo = await LerJsonAsync(segundo);
            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO.ToString(), corpoSegundo.GetProperty(CampoSituacao).GetString());
            Assert.Equal(cliente.IdCliente, corpoSegundo.GetProperty(CampoSessao).GetProperty(CampoIdUsuario).GetInt32());
        }

        [Fact]
        public async Task ProvaInvalida_Devolve401ENaoGravaNada()
        {
            const string email = "prova.invalida@exemplo.com";
            const string sub = "sub-prova-invalida";
            Cliente cliente = Semear(_fabrica.Repositorio, "30000000002", email);
            int gravacoesAntes = _fabrica.Vinculos.Gravacoes;

            Dictionary<string, object> claimsOutroCliente = GoogleProvaDeTeste.ClaimsPadrao();
            claimsOutroCliente["aud"] = OutroClientId;
            Dictionary<string, object> claimsExpirada = GoogleProvaDeTeste.ClaimsPadrao();
            claimsExpirada["exp"] = DateTimeOffset.Now.AddHours(-2).ToUnixTimeSeconds();
            Dictionary<string, object> claimsNaoVerificado = GoogleProvaDeTeste.ClaimsPadrao();
            claimsNaoVerificado["email_verified"] = false;
            claimsNaoVerificado["email"] = email;
            claimsNaoVerificado["sub"] = sub;
            Dictionary<string, object> claimsAssinadas = GoogleProvaDeTeste.ClaimsPadrao();
            claimsAssinadas["sub"] = sub;
            claimsAssinadas["email"] = email;

            List<string> provasInvalidas = new List<string>
            {
                GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claimsOutroCliente),
                GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claimsExpirada),
                GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChaveSecundaria, GoogleProvaDeTeste.KidPrincipal, claimsAssinadas),
                GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claimsNaoVerificado),
                GoogleProvaDeTeste.AssinarHs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claimsAssinadas),
                GoogleProvaDeTeste.MontarSemAssinatura(GoogleProvaDeTeste.KidPrincipal, claimsAssinadas)
            };

            foreach (string prova in provasInvalidas)
            {
                ResumoProblema login = await ProblemaAsync(await LoginAsync(_cliente, prova));
                ResumoProblema vinculo = await ProblemaAsync(await VinculoAsync(_cliente, prova, "30000000002", SenhaPadrao));

                Assert.Equal((int)HttpStatusCode.Unauthorized, login.Status);
                Assert.Equal(ConstantsMessage.ErroGoogleCredencialInvalida, login.Detalhe);
                Assert.Equal((int)HttpStatusCode.Unauthorized, vinculo.Status);
                Assert.Equal(ConstantsMessage.ErroGoogleCredencialInvalida, vinculo.Detalhe);
            }

            Assert.Null(cliente.GoogleSub);
            Assert.Equal(gravacoesAntes, _fabrica.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task SemCadastro_DevolveCadastroNecessarioSemCriarCliente()
        {
            const string email = "sem.cadastro@exemplo.com";
            const string sub = "sub-sem-cadastro";
            int clientesAntes = _fabrica.Repositorio.Clientes.Count;

            HttpResponseMessage resposta = await LoginAsync(_cliente, CredencialDe(email, sub));

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(SituacaoLoginGoogle.CADASTRO_NECESSARIO.ToString(), corpo.GetProperty(CampoSituacao).GetString());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(CampoSessao).ValueKind);
            Assert.Equal(GoogleProvaDeTeste.NomeTeste, corpo.GetProperty("nome").GetString());
            Assert.Equal(email, corpo.GetProperty("email").GetString());
            List<string> campos = corpo.EnumerateObject().Select(propriedade => propriedade.Name).ToList();
            Assert.Equal(new List<string> { CampoSituacao, CampoSessao, "nome", "email" }, campos);
            Assert.Equal(clientesAntes, _fabrica.Repositorio.Clientes.Count);
        }

        [Fact]
        public async Task DoisCadastrosComOEmail_DevolveContaAmbiguaESemSessao()
        {
            const string email = "ambigua.http@exemplo.com";
            const string sub = "sub-ambigua-http";
            Cliente primeiro = Semear(_fabrica.Repositorio, "30000000003", email);
            Semear(_fabrica.Repositorio, "30000000004", email);

            HttpResponseMessage login = await LoginAsync(_cliente, CredencialDe(email, sub));

            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(SituacaoLoginGoogle.CONTA_AMBIGUA.ToString(), corpo.GetProperty(CampoSituacao).GetString());
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty(CampoSessao).ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("nome").ValueKind);
            Assert.Equal(JsonValueKind.Null, corpo.GetProperty("email").ValueKind);

            ResumoProblema vinculo = await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe(email, sub), "30000000003", SenhaPadrao));

            Assert.Equal((int)HttpStatusCode.Conflict, vinculo.Status);
            Assert.Equal(ConstantsMessage.ErroGoogleContaAmbigua, vinculo.Detalhe);
            Assert.Null(primeiro.GoogleSub);
        }

        [Fact]
        public async Task ClienteDesativado_NaoEntraNemVincula()
        {
            const string email = "desativado.http@exemplo.com";
            Cliente desativado = Semear(_fabrica.Repositorio, "30000000005", email);
            desativado.Disable();

            HttpResponseMessage login = await LoginAsync(_cliente, CredencialDe(email, "sub-desativado-http"));

            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(SituacaoLoginGoogle.CADASTRO_NECESSARIO.ToString(), corpo.GetProperty(CampoSituacao).GetString());

            ResumoProblema vinculo = await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe(email, "sub-desativado-http"), "30000000005", SenhaPadrao));

            Assert.Equal((int)HttpStatusCode.Unauthorized, vinculo.Status);
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, vinculo.Detalhe);
            Assert.Null(desativado.GoogleSub);
        }

        [Fact]
        public async Task PendenteERecusado_EntramPelaContaVinculada()
        {
            Semear(_fabrica.Repositorio, "30000000006", "pendente.http@exemplo.com", "sub-pendente-http", StatusCadastroCliente.PENDENTE);
            Semear(_fabrica.Repositorio, "30000000007", "recusado.http@exemplo.com", "sub-recusado-http", StatusCadastroCliente.RECUSADO);

            HttpResponseMessage pendente = await LoginAsync(_cliente, CredencialDe("pendente.http@exemplo.com", "sub-pendente-http"));
            HttpResponseMessage recusado = await LoginAsync(_cliente, CredencialDe("recusado.http@exemplo.com", "sub-recusado-http"));

            JsonElement corpoPendente = await LerJsonAsync(pendente);
            JsonElement corpoRecusado = await LerJsonAsync(recusado);
            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO.ToString(), corpoPendente.GetProperty(CampoSituacao).GetString());
            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO.ToString(), corpoRecusado.GetProperty(CampoSituacao).GetString());
        }

        [Fact]
        public async Task RecusasDoVinculo_TemMesmoStatusECorpo()
        {
            Cliente comSenhaErrada = Semear(_fabrica.Repositorio, "30000000008", "recusas.http@exemplo.com");
            Cliente comEmailDiferente = Semear(_fabrica.Repositorio, "30000000009", "recusas.outro@exemplo.com");
            Cliente semSenha = Semear(_fabrica.Repositorio, "30000000010", "recusas.sem.senha@exemplo.com", comSenha: false);

            List<ResumoProblema> recusas = new List<ResumoProblema>
            {
                await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("recusas.http@exemplo.com", "sub-recusa-1"), "30000000008", SenhaErrada)),
                await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("recusas.http@exemplo.com", "sub-recusa-1"), CpfInexistente, SenhaPadrao)),
                await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("recusas.sem.senha@exemplo.com", "sub-recusa-3"), "30000000010", SenhaPadrao)),
                await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("recusas.http@exemplo.com", "sub-recusa-1"), "30000000009", SenhaPadrao))
            };

            ResumoProblema esperada = recusas[0];
            Assert.Equal((int)HttpStatusCode.Unauthorized, esperada.Status);
            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, esperada.Detalhe);

            foreach (ResumoProblema recusa in recusas)
            {
                Assert.Equal(esperada, recusa);
            }

            Assert.Null(comSenhaErrada.GoogleSub);
            Assert.Null(comEmailDiferente.GoogleSub);
            Assert.Null(semSenha.GoogleSub);
        }

        [Fact]
        public async Task ContaGoogleJaVinculadaAOutroCliente_Devolve409()
        {
            Semear(_fabrica.Repositorio, "30000000011", "outro.vinculo@exemplo.com", "sub-ja-vinculada-http");
            Cliente alvo = Semear(_fabrica.Repositorio, "30000000012", "alvo.vinculo@exemplo.com");

            ResumoProblema resposta = await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("alvo.vinculo@exemplo.com", "sub-ja-vinculada-http"), "30000000012", SenhaPadrao));

            Assert.Equal((int)HttpStatusCode.Conflict, resposta.Status);
            Assert.Equal(ConstantsMessage.ErroGoogleContaJaVinculada, resposta.Detalhe);
            Assert.Null(alvo.GoogleSub);
        }

        [Fact]
        public async Task ClienteJaVinculadoOutraContaGoogle_Devolve409()
        {
            Cliente cliente = Semear(_fabrica.Repositorio, "30000000013", "cliente.outra.conta@exemplo.com", SubOutraConta);

            ResumoProblema resposta = await ProblemaAsync(await VinculoAsync(_cliente, CredencialDe("cliente.outra.conta@exemplo.com", "sub-nova-conta-z"), "30000000013", SenhaPadrao));

            Assert.Equal((int)HttpStatusCode.Conflict, resposta.Status);
            Assert.Equal(ConstantsMessage.ErroGoogleClienteJaVinculado, resposta.Detalhe);
            Assert.Equal(SubOutraConta, cliente.GoogleSub);
        }

        [Fact]
        public async Task MesmoSubJaGravado_EIdempotenteEAbreASessao()
        {
            Cliente cliente = Semear(_fabrica.Repositorio, "30000000014", "idempotente.http@exemplo.com", "sub-idempotente-http");
            int gravacoesAntes = _fabrica.Vinculos.Gravacoes;

            HttpResponseMessage resposta = await VinculoAsync(_cliente, CredencialDe("idempotente.http@exemplo.com", "sub-idempotente-http"), "30000000014", SenhaPadrao);

            Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
            JsonElement corpo = await LerJsonAsync(resposta);
            Assert.Equal(cliente.IdCliente, corpo.GetProperty(CampoIdUsuario).GetInt32());
            Assert.Equal(gravacoesAntes, _fabrica.Vinculos.Gravacoes);
        }

        /// <summary>
        /// Vinculo com lembrar = 30 dias (CPF e senha provados). Login direto com lembrar = sessao CURTA: decisao do Kaio
        /// (C4), o campo e aceito mas ignorado no login pelo Google. Antes este teste esperava 30 dias no login.
        /// </summary>
        [Fact]
        public async Task LembrarVerdadeiro_NoVinculoTrintaDias_NoLoginDiretoSessaoCurta_EAceitaNaRotaProtegida()
        {
            const string email = "lembrar.http@exemplo.com";
            const string sub = "sub-lembrar-http";
            Cliente cliente = Semear(_fabrica.Repositorio, "30000000015", email);
            DateTime antes = DateTime.Now;

            HttpResponseMessage vinculo = await VinculoAsync(_cliente, CredencialDe(email, sub), "30000000015", SenhaPadrao, lembrar: true);

            Assert.Equal(HttpStatusCode.OK, vinculo.StatusCode);
            JsonElement corpoVinculo = await LerJsonAsync(vinculo);
            Assert.InRange(DataDoCampo(corpoVinculo, CampoExpiraEm), antes.AddDays(ValidadeLembrarDias).AddMinutes(-ToleranciaMinutos), antes.AddDays(ValidadeLembrarDias).AddMinutes(ToleranciaMinutos));
            string tokenDoVinculo = corpoVinculo.GetProperty("token").GetString()!;

            HttpRequestMessage identidade = new HttpRequestMessage(HttpMethod.Get, RotaIdentidade);
            identidade.Headers.Authorization = new AuthenticationHeaderValue(ConstantsValues.TipoTokenBearer, tokenDoVinculo);
            HttpResponseMessage respostaIdentidade = await _cliente.SendAsync(identidade);

            Assert.Equal(HttpStatusCode.OK, respostaIdentidade.StatusCode);
            JsonElement corpoIdentidade = await LerJsonAsync(respostaIdentidade);
            Assert.Equal(cliente.IdCliente.ToString(CultureInfo.InvariantCulture), corpoIdentidade.GetProperty(CampoIdUsuario).GetString());

            HttpResponseMessage login = await LoginAsync(_cliente, CredencialDe(email, sub), lembrar: true);

            JsonElement corpoLogin = await LerJsonAsync(login);
            JsonElement sessao = corpoLogin.GetProperty(CampoSessao);
            // Decisao do Kaio (C4): lembrar e ignorado no login direto; a sessao e a curta (Jwt:ExpiracaoMinutos = 120).
            Assert.InRange(DataDoCampo(sessao, CampoExpiraEm), antes.AddMinutes(ExpiracaoCurtaMinutos - ToleranciaMinutos), antes.AddMinutes(ExpiracaoCurtaMinutos + ToleranciaMinutos));
        }

        [Fact]
        public async Task DepoisDeVinculado_MudarOEmailNaoImpedeOLoginPeloSub()
        {
            const string email = "mudou.email@exemplo.com";
            const string sub = "sub-mudou-email-http";
            Cliente cliente = Semear(_fabrica.Repositorio, "30000000016", email);
            await VinculoAsync(_cliente, CredencialDe(email, sub), "30000000016", SenhaPadrao);
            cliente.Email = EmailNovo;

            HttpResponseMessage login = await LoginAsync(_cliente, CredencialDe(email, sub));

            JsonElement corpo = await LerJsonAsync(login);
            Assert.Equal(SituacaoLoginGoogle.AUTENTICADO.ToString(), corpo.GetProperty(CampoSituacao).GetString());
            Assert.Equal(cliente.IdCliente, corpo.GetProperty(CampoSessao).GetProperty(CampoIdUsuario).GetInt32());
        }

        [Fact]
        public async Task Validacao_Devolve400ParaEntradaInvalida()
        {
            string credencialLonga = new string('a', TamanhoCredencialAcimaDoLimite);
            string senhaLonga = new string('s', TamanhoSenhaAcimaDoLimite);
            string credencialValida = CredencialDe("validacao.http@exemplo.com", "sub-validacao-http");

            List<HttpResponseMessage> respostas = new List<HttpResponseMessage>
            {
                await LoginAsync(_cliente, CredencialInvalidaCurta),
                await LoginAsync(_cliente, credencialLonga),
                await VinculoAsync(_cliente, credencialValida, "123", SenhaPadrao),
                await VinculoAsync(_cliente, credencialValida, "30000000017", string.Empty),
                await VinculoAsync(_cliente, credencialValida, "30000000017", senhaLonga)
            };

            foreach (HttpResponseMessage resposta in respostas)
            {
                ResumoProblema problema = await ProblemaAsync(resposta);

                Assert.Equal((int)HttpStatusCode.BadRequest, problema.Status);
            }
        }

        // ------------------------------------------------------------------
        // Cenarios com integracao diferente
        // ------------------------------------------------------------------

        [Fact]
        public async Task GoogleIndisponivel_Devolve502NosDoisPost()
        {
            using GoogleWebFactoryJwksFalha fabrica = new GoogleWebFactoryJwksFalha();
            HttpClient cliente = fabrica.CreateClient();
            Semear(fabrica.Repositorio, "30000000018", "indisponivel.http@exemplo.com");
            string credencial = CredencialDe("indisponivel.http@exemplo.com", "sub-indisponivel-http");

            ResumoProblema login = await ProblemaAsync(await LoginAsync(cliente, credencial));
            ResumoProblema vinculo = await ProblemaAsync(await VinculoAsync(cliente, credencial, "30000000018", SenhaPadrao));

            Assert.Equal((int)HttpStatusCode.BadGateway, login.Status);
            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, login.Detalhe);
            Assert.Equal((int)HttpStatusCode.BadGateway, vinculo.Status);
            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, vinculo.Detalhe);
            Assert.Equal(0, fabrica.Vinculos.Gravacoes);
        }

        [Fact]
        public async Task IntegracaoSemClientId_DesabilitaAConfiguracaoEDevolve502NosPost()
        {
            using GoogleWebFactoryDesabilitada fabrica = new GoogleWebFactoryDesabilitada();
            HttpClient cliente = fabrica.CreateClient();
            Semear(fabrica.Repositorio, "30000000019", "sem.clientid.http@exemplo.com");
            string credencial = CredencialDe("sem.clientid.http@exemplo.com", "sub-sem-clientid-http");

            HttpResponseMessage configuracao = await cliente.GetAsync(RotaConfiguracao);
            JsonElement corpoConfiguracao = await LerJsonAsync(configuracao);
            ResumoProblema login = await ProblemaAsync(await LoginAsync(cliente, credencial));
            ResumoProblema vinculo = await ProblemaAsync(await VinculoAsync(cliente, credencial, "30000000019", SenhaPadrao));

            Assert.False(corpoConfiguracao.GetProperty(CampoHabilitado).GetBoolean());
            Assert.Equal(JsonValueKind.Null, corpoConfiguracao.GetProperty(CampoClientId).ValueKind);
            Assert.Equal((int)HttpStatusCode.BadGateway, login.Status);
            Assert.Equal((int)HttpStatusCode.BadGateway, vinculo.Status);
        }

        // ------------------------------------------------------------------
        // Log: nenhum dado pessoal, nem nos caminhos de erro
        // ------------------------------------------------------------------

        [Fact]
        public async Task Log_NaoRegistraCredencialSubEmailNomeCpfOuSenha()
        {
            using GoogleWebFactoryPadrao fabrica = new GoogleWebFactoryPadrao();
            HttpClient cliente = fabrica.CreateClient();
            const string email = "log.http@exemplo.com";
            const string sub = "sub-log-http-unico";
            const string cpf = "30000000021";
            Semear(fabrica.Repositorio, cpf, email);
            string credencial = CredencialDe(email, sub);

            await LoginAsync(cliente, credencial);
            await VinculoAsync(cliente, credencial, cpf, SenhaPadrao);
            await LoginAsync(cliente, credencial);
            await VinculoAsync(cliente, credencial, cpf, SenhaErrada);
            await LoginAsync(cliente, CredencialDe(email, sub).Replace('.', 'x'));
            await VinculoAsync(cliente, credencial, CpfInexistente, SenhaPadrao);

            Assert.NotEmpty(fabrica.Log.Entries);
            string[] proibidos = { sub, email, GoogleProvaDeTeste.NomeTeste, cpf, SenhaPadrao, SenhaErrada, credencial, GoogleProvaDeTeste.EmailMisto };

            foreach (string dado in proibidos)
            {
                Assert.DoesNotContain(fabrica.Log.Entries, entrada => entrada.Contains(dado, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
