using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Google;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Google
{
    /// <summary>
    /// Provedor GOOGLE com chave RSA de teste e JWKS servido por IComunicacaoService falso (sem rede).
    /// Cobre a validacao da prova, o cache das chaves e a atualizacao controlada por kid.
    /// </summary>
    public class GoogleIdentityProviderTests
    {
        private static GoogleIdentityProvider CriarProvider(IComunicacaoService comunicacao, FakeLogService log, Func<DateTime> relogio)
        {
            return new GoogleIdentityProvider(comunicacao, new MemoryCache(new MemoryCacheOptions()), log, relogio);
        }

        private static Func<DateTime> RelogioDoSistema()
        {
            return () => DateTime.Now;
        }

        private static Task<GoogleIdentidade> ValidarAsync(GoogleIdentityProvider provider, string credencial, string clientId = GoogleProvaDeTeste.ClientId)
        {
            return provider.ValidarAsync(GoogleProvaDeTeste.ConfiguracaoGoogle(), clientId, credencial);
        }

        private static GoogleProvaDeTeste.ComunicacaoGoogleFalsa ComunicacaoComChavePrincipal()
        {
            return new GoogleProvaDeTeste.ComunicacaoGoogleFalsa
            {
                Jwks = GoogleProvaDeTeste.MontarJwks((GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal))
            };
        }

        private static string AssinarValida(Dictionary<string, object> claims)
        {
            return GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, claims);
        }

        // ------------------------------------------------------------------
        // Prova valida
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarAsync_ProvaValida_DevolveSubEmailMinusculoENome()
        {
            FakeLogService log = new FakeLogService();
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), log, RelogioDoSistema());

            GoogleIdentidade identidade = await ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao()));

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
            Assert.Equal(GoogleProvaDeTeste.EmailMinusculo, identidade.Email);
            Assert.True(identidade.EmailVerificado);
            Assert.Equal(GoogleProvaDeTeste.NomeTeste, identidade.Nome);
        }

        [Fact]
        public async Task ValidarAsync_EmissorSemHttps_AceitaAmbosOsEmissores()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["iss"] = "accounts.google.com";

            GoogleIdentidade identidade = await ValidarAsync(provider, AssinarValida(claims));

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
        }

        [Fact]
        public async Task ValidarAsync_ExpiradaDentroDaTolerancia_Aceita()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["exp"] = DateTimeOffset.Now.ToUnixTimeSeconds() - 30;

            GoogleIdentidade identidade = await ValidarAsync(provider, AssinarValida(claims));

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
        }

        // ------------------------------------------------------------------
        // Prova recusada (UnauthorizedException, fail-closed)
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarAsync_AssinaturaDeOutraChave_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            string credencial = GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChaveSecundaria, GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));
        }

        [Fact]
        public async Task ValidarAsync_AudienciaDeOutroClientId_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["aud"] = "outro-cliente.apps.googleusercontent.com";

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));

            Assert.Equal(ConstantsMessage.ErroGoogleCredencialInvalida, excecao.Message);
        }

        [Fact]
        public async Task ValidarAsync_EmissorErrado_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["iss"] = "https://exemplo.com";

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_ExpiradaAlemDaTolerancia_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["exp"] = DateTimeOffset.Now.ToUnixTimeSeconds() - 120;

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_AindaNaoValida_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["nbf"] = DateTimeOffset.Now.ToUnixTimeSeconds() + 3600;

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_EmailNaoVerificadoBooleano_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["email_verified"] = false;

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_EmailVerificadoComoTextoTrue_Aceita()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["email_verified"] = "true";

            GoogleIdentidade identidade = await ValidarAsync(provider, AssinarValida(claims));

            Assert.True(identidade.EmailVerificado);
        }

        [Fact]
        public async Task ValidarAsync_EmailVerificadoComoTextoFalse_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["email_verified"] = "false";

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_SemEmailVerificado_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims.Remove("email_verified");

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_SemSub_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims.Remove("sub");

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_SemEmail_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims.Remove("email");

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, AssinarValida(claims)));
        }

        [Fact]
        public async Task ValidarAsync_AlgoritmoHs256ComChavePublicaComoSegredo_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            string credencial = GoogleProvaDeTeste.AssinarHs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));
        }

        [Fact]
        public async Task ValidarAsync_AlgoritmoNone_Recusa()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());
            string credencial = GoogleProvaDeTeste.MontarSemAssinatura(GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("nao-e-um-jwt")]
        [InlineData("a.b")]
        [InlineData("a.b.c")]
        public async Task ValidarAsync_CredencialVaziaOuMalformada_RecusaSemBuscarChaves(string credencial)
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));

            Assert.Equal(0, comunicacao.Chamadas);
        }

        [Fact]
        public async Task ValidarAsync_CredencialAcimaDoLimite_RecusaSemBuscarChaves()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());
            string credencial = new string('a', 4097);

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));

            Assert.Equal(0, comunicacao.Chamadas);
        }

        [Fact]
        public async Task ValidarAsync_RecusaNaoGravaCredencialNemIdentidadeNoLog()
        {
            FakeLogService log = new FakeLogService();
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), log, RelogioDoSistema());
            Dictionary<string, object> claims = GoogleProvaDeTeste.ClaimsPadrao();
            claims["email_verified"] = false;
            string credencial = AssinarValida(claims);

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));

            Assert.NotEmpty(log.Entries);
            Assert.DoesNotContain(log.Entries, x => x.Contains(credencial, StringComparison.Ordinal));
            Assert.DoesNotContain(log.Entries, x => x.Contains(GoogleProvaDeTeste.Sub, StringComparison.Ordinal));
            Assert.DoesNotContain(log.Entries, x => x.Contains(GoogleProvaDeTeste.EmailMinusculo, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(log.Entries, x => x.Contains(GoogleProvaDeTeste.NomeTeste, StringComparison.OrdinalIgnoreCase));
        }

        // ------------------------------------------------------------------
        // Client ID
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidarAsync_ClientIdVazio_LancaIntegrationException(string clientId)
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao()), clientId));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
        }

        // ------------------------------------------------------------------
        // Chaves publicas: cache, atualizacao por kid e falhas
        // ------------------------------------------------------------------

        [Fact]
        public async Task ValidarAsync_SegundaValidacao_UsaCacheSemBuscarDeNovo()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            await ValidarAsync(provider, credencial);
            await ValidarAsync(provider, credencial);

            Assert.Equal(1, comunicacao.Chamadas);
        }

        [Fact]
        public async Task ValidarAsync_KidDesconhecido_AtualizaNoMaximoUmaVezPorIntervalo()
        {
            DateTime agora = DateTime.Now;
            Func<DateTime> relogio = () => agora;
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), relogio);
            string credencialSecundaria = GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChaveSecundaria, GoogleProvaDeTeste.KidSecundaria, GoogleProvaDeTeste.ClaimsPadrao());

            // 1. Cache frio: uma busca e a prova do kid conhecido passa.
            await ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao()));
            Assert.Equal(1, comunicacao.Chamadas);

            // 2. Kid desconhecido logo depois da busca: nao atualiza.
            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencialSecundaria));
            Assert.Equal(1, comunicacao.Chamadas);

            // 3. Passados 301 s: uma atualizacao; o kid continua desconhecido e a prova e recusada.
            agora = agora.AddSeconds(301);
            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencialSecundaria));
            Assert.Equal(2, comunicacao.Chamadas);

            // 4. Na sequencia, sem novo intervalo: nao atualiza de novo.
            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencialSecundaria));
            Assert.Equal(2, comunicacao.Chamadas);

            // 5. O Google publica a chave nova; 301 s depois a atualizacao traz o kid e a prova passa.
            comunicacao.Jwks = GoogleProvaDeTeste.MontarJwks(
                (GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal),
                (GoogleProvaDeTeste.ChaveSecundaria, GoogleProvaDeTeste.KidSecundaria));
            agora = agora.AddSeconds(301);

            GoogleIdentidade identidade = await ValidarAsync(provider, credencialSecundaria);

            Assert.Equal(3, comunicacao.Chamadas);
            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
        }

        [Fact]
        public async Task ValidarAsync_JwksInacessivel_LancaIntegrationExceptionNaoUnauthorized()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            comunicacao.Falhar = true;
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao())));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task ValidarAsync_JwksSemChaves_LancaIntegrationException()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = new GoogleProvaDeTeste.ComunicacaoGoogleFalsa { Jwks = "{\"keys\":[]}" };
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao())));

            Assert.Equal(ConstantsMessage.ErroGoogleCarregarChaves, excecao.Message);
        }

        [Fact]
        public async Task ValidarAsync_JwksInvalido_LancaIntegrationException()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = new GoogleProvaDeTeste.ComunicacaoGoogleFalsa { Jwks = "isto nao e um conjunto de chaves" };
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao())));

            Assert.Equal(ConstantsMessage.ErroGoogleCarregarChaves, excecao.Message);
        }

        // ------------------------------------------------------------------
        // Cache negativo, reserva, origem das chaves, cancelamento e log (revisao de seguranca e engenharia)
        // ------------------------------------------------------------------

        private const int ValidacoesSeguidas = 5;
        private const int SegundosAlemDoIntervaloFalha = 1;
        private const int TimeoutTesteMs = 15000;
        private const string BaseUrlHttp = "http://www.googleapis.com";
        private const string BaseUrlOutroHost = "https://outro.exemplo.com";
        private const string BaseUrlHostParecido = "https://www.googleapis.com.evil.com";
        private const string BaseUrlMalformada = "nao e uma url";
        private const string BaseUrlPortaNaoPadrao = "https://www.googleapis.com:8443";
        private const string BaseUrlComUserInfo = "https://x@www.googleapis.com";
        private const string BaseUrlComCaminho = "https://www.googleapis.com/outro";
        private const string BaseUrlComConsulta = "https://www.googleapis.com/?x=1";
        private const string BaseUrlPadrao = "https://www.googleapis.com";
        private const string BaseUrlPadraoComBarra = "https://www.googleapis.com/";
        private const string BaseUrlPadraoPortaPadrao = "https://www.googleapis.com:443";
        private const int JwksReservaMinutosAntesDoLimite = 1;
        private const int JwksReservaSegundosDepoisDoLimite = 1;

        private static GoogleIdentityProvider CriarProviderComCache(IComunicacaoService comunicacao, IMemoryCache cache, FakeLogService log, Func<DateTime> relogio)
        {
            return new GoogleIdentityProvider(comunicacao, cache, log, relogio);
        }

        private static ConfiguracaoIntegracao ConfiguracaoComBaseUrl(string baseUrl)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Google,
                "Login com Google",
                AmbienteIntegracao.SANDBOX,
                baseUrl,
                TimeoutTesteMs,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>C1(a): com cache vazio e o Google fora, 5 validacoes seguidas geram 1 busca; depois do intervalo, nova busca.</summary>
        [Fact]
        public async Task JwksForaSemCache_CincoValidacoesSeguidasGeramUmaBusca_EDepoisDoIntervaloOutra()
        {
            DateTime agora = DateTime.Now;
            Func<DateTime> relogio = () => agora;
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            comunicacao.Falhar = true;
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), relogio);
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            for (int tentativa = 0; tentativa < ValidacoesSeguidas; tentativa++)
            {
                await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, credencial));
            }

            Assert.Equal(1, comunicacao.Chamadas);

            agora = agora.AddSeconds(ConstantsGoogle.JwksIntervaloFalhaSegundos + SegundosAlemDoIntervaloFalha);
            comunicacao.Falhar = false;

            GoogleIdentidade identidade = await ValidarAsync(provider, credencial);

            Assert.Equal(2, comunicacao.Chamadas);
            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
        }

        /// <summary>C1(b): falha da atualizacao com o cache expirado, mas com um JWKS anterior bom: a prova da chave antiga ainda vale.</summary>
        [Fact]
        public async Task FalhaDaAtualizacao_ComJwksAnterior_AindaValidaProvaDaChaveAntiga()
        {
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProviderComCache(comunicacao, cache, new FakeLogService(), RelogioDoSistema());
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            await ValidarAsync(provider, credencial);
            Assert.Equal(1, comunicacao.Chamadas);

            // Remove o cache do JWKS (o expirado); a reserva do ultimo conjunto bom fica no estado, que nao e removido.
            cache.Compact(1.0);
            comunicacao.Falhar = true;

            GoogleIdentidade identidade = await ValidarAsync(provider, credencial);

            Assert.Equal(2, comunicacao.Chamadas);
            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
        }

        /// <summary>C1(b): com JWKS anterior e atualizacao por kid desconhecido que falha, a prova do kid novo e recusada (401), nao 502.</summary>
        [Fact]
        public async Task FalhaDaAtualizacaoPorKidDesconhecido_RecusaOKidNovoComoNaoAutorizado()
        {
            DateTime agora = DateTime.Now;
            Func<DateTime> relogio = () => agora;
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), relogio);
            string credencialSecundaria = GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChaveSecundaria, GoogleProvaDeTeste.KidSecundaria, GoogleProvaDeTeste.ClaimsPadrao());

            await ValidarAsync(provider, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao()));
            agora = agora.AddSeconds(ConstantsGoogle.JwksIntervaloRefreshSegundos + SegundosAlemDoIntervaloFalha);
            comunicacao.Falhar = true;

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencialSecundaria));

            Assert.Equal(2, comunicacao.Chamadas);
        }

        /// <summary>C1(a): base_url fora do padrao (http, outro host, host parecido ou malformada) recusa antes de qualquer chamada de rede.</summary>
        [Theory]
        [InlineData(BaseUrlHttp)]
        [InlineData(BaseUrlOutroHost)]
        [InlineData(BaseUrlHostParecido)]
        [InlineData(BaseUrlMalformada)]
        [InlineData(BaseUrlPortaNaoPadrao)]
        [InlineData(BaseUrlComUserInfo)]
        [InlineData(BaseUrlComCaminho)]
        [InlineData(BaseUrlComConsulta)]
        public async Task BaseUrlForaDoPadraoGoogle_LancaIntegrationExceptionSemChamarARede(string baseUrl)
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());
            ConfiguracaoIntegracao configuracao = ConfiguracaoComBaseUrl(baseUrl);
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.ValidarAsync(configuracao, GoogleProvaDeTeste.ClientId, credencial));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
            Assert.Equal(0, comunicacao.Chamadas);
        }

        /// <summary>8a: as formas legitimas da base_url (raiz com ou sem barra, porta padrao explicita) continuam aceitas e chegam a buscar as chaves.</summary>
        [Theory]
        [InlineData(BaseUrlPadrao)]
        [InlineData(BaseUrlPadraoComBarra)]
        [InlineData(BaseUrlPadraoPortaPadrao)]
        public async Task BaseUrlPadraoGoogle_EhAceitaEBuscaAsChaves(string baseUrl)
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());
            ConfiguracaoIntegracao configuracao = ConfiguracaoComBaseUrl(baseUrl);

            GoogleIdentidade identidade = await provider.ValidarAsync(configuracao, GoogleProvaDeTeste.ClientId, AssinarValida(GoogleProvaDeTeste.ClaimsPadrao()));

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
            Assert.Equal(1, comunicacao.Chamadas);
        }

        /// <summary>
        /// 8b: a reserva (ultimo JWKS bom) vale ate 24 h desde a ultima busca bem-sucedida. Dentro do prazo, quando o
        /// Google falha e o cache sumiu, a prova da chave anterior ainda e aceita.
        /// </summary>
        [Fact]
        public async Task ReservaDentroDoPrazoDeVinteEQuatroHoras_AindaValidaAProvaQuandoOGoogleFalha()
        {
            DateTime agora = DateTime.Now;
            Func<DateTime> relogio = () => agora;
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProviderComCache(comunicacao, cache, new FakeLogService(), relogio);
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            await ValidarAsync(provider, credencial);
            Assert.Equal(1, comunicacao.Chamadas);

            cache.Compact(1.0);
            comunicacao.Falhar = true;
            agora = agora.Add(TimeSpan.FromHours(ConstantsGoogle.JwksReservaMaximaHoras)).Subtract(TimeSpan.FromMinutes(JwksReservaMinutosAntesDoLimite));

            GoogleIdentidade identidade = await ValidarAsync(provider, credencial);

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
            Assert.Equal(2, comunicacao.Chamadas);
        }

        /// <summary>
        /// 8b: passadas 24 h desde a ultima busca bem-sucedida, a reserva nao e mais usada. Com o Google fora, a falha
        /// vira IntegrationException (502), e nao a prova aceita com a chave antiga.
        /// </summary>
        [Fact]
        public async Task ReservaComMaisDeVinteEQuatroHoras_NaoEUsadaELancaIntegrationException()
        {
            DateTime agora = DateTime.Now;
            Func<DateTime> relogio = () => agora;
            MemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProviderComCache(comunicacao, cache, new FakeLogService(), relogio);
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());

            await ValidarAsync(provider, credencial);
            Assert.Equal(1, comunicacao.Chamadas);

            cache.Compact(1.0);
            comunicacao.Falhar = true;
            agora = agora.Add(TimeSpan.FromHours(ConstantsGoogle.JwksReservaMaximaHoras)).Add(TimeSpan.FromSeconds(JwksReservaSegundosDepoisDoLimite));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, credencial));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
            Assert.Equal(2, comunicacao.Chamadas);
        }

        /// <summary>C1(c): com o token da requisicao cancelado, o provedor nao espera a trava nem busca as chaves.</summary>
        [Fact]
        public async Task CancelamentoPeloChamador_NaoBuscaAsChaves()
        {
            GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao = ComunicacaoComChavePrincipal();
            GoogleIdentityProvider provider = CriarProvider(comunicacao, new FakeLogService(), RelogioDoSistema());
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ValidarAsync(GoogleProvaDeTeste.ConfiguracaoGoogle(), GoogleProvaDeTeste.ClientId, credencial, cancelamento.Token));

            Assert.Equal(0, comunicacao.Chamadas);
        }

        /// <summary>P6: a mensagem de uma excecao do transporte (aqui, com a prova) nao entra no log; so o tipo da excecao entra.</summary>
        [Fact]
        public async Task ExcecaoComTextoSensivel_NaoApareceEmNenhumaEntradaDoLog()
        {
            FakeLogService log = new FakeLogService();
            string credencial = AssinarValida(GoogleProvaDeTeste.ClaimsPadrao());
            GoogleIdentityProvider provider = CriarProvider(new ComunicacaoQueFalha(credencial), log, RelogioDoSistema());

            await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, credencial));

            Assert.NotEmpty(log.Entries);
            Assert.DoesNotContain(log.Entries, x => x.Contains(credencial, StringComparison.Ordinal));
            Assert.Contains(log.Entries, x => x.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
        }

        /// <summary>IComunicacaoService que falha com uma excecao cuja mensagem carrega trecho da prova.</summary>
        private sealed class ComunicacaoQueFalha : IComunicacaoService
        {
            private readonly string _trecho;

            public ComunicacaoQueFalha(string trecho)
            {
                _trecho = trecho;
            }

            public Task<RequestResponse<T>> SendAsync<T>(RequestInfo request, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Falha de transporte com trecho: " + _trecho);
            }
        }

        [Fact]
        public void Nome_EhGoogle()
        {
            GoogleIdentityProvider provider = CriarProvider(ComunicacaoComChavePrincipal(), new FakeLogService(), RelogioDoSistema());

            Assert.Equal(ConstantsGoogle.ProvedorGoogle, provider.Nome);
        }
    }
}
