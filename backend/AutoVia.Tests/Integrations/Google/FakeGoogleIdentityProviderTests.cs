using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Services.Google;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Tests.Integrations.Google
{
    /// <summary>
    /// Provedor FAKE do Google: so em Development + SANDBOX; aceita FAKE:email e recusa :naoverificado.
    /// </summary>
    public class FakeGoogleIdentityProviderTests
    {
        private const string EmailTeste = "Maria.Teste@Exemplo.com";
        private const string EmailTesteMinusculo = "maria.teste@exemplo.com";
        private const string PadraoSubFake = "^fake-[0-9a-f]{16}$";

        // Mesmos nomes que Environments.Production e Environments.Staging (campos readonly, nao servem em atributo).
        private const string NomeAmbienteProducao = "Production";
        private const string NomeAmbienteStaging = "Staging";

        private static FakeGoogleIdentityProvider CriarProvider(string nomeAmbiente, FakeLogService log)
        {
            return new FakeGoogleIdentityProvider(new GoogleProvaDeTeste.AmbienteFalso(nomeAmbiente), log);
        }

        private static Task<GoogleIdentidade> ValidarAsync(FakeGoogleIdentityProvider provider, string credencial, AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX)
        {
            return provider.ValidarAsync(GoogleProvaDeTeste.ConfiguracaoGoogle(ambiente), GoogleProvaDeTeste.ClientId, credencial);
        }

        [Fact]
        public void Nome_EhFake()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            Assert.Equal(ConstantsGoogle.ProvedorFake, provider.Nome);
        }

        [Fact]
        public async Task ValidarAsync_DevelopmentComSandbox_AceitaEmailVerificado()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            GoogleIdentidade identidade = await ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste);

            Assert.Equal(EmailTesteMinusculo, identidade.Email);
            Assert.True(identidade.EmailVerificado);
            Assert.Equal("maria.teste", identidade.Nome);
        }

        [Fact]
        public async Task ValidarAsync_SubDeterministicoDeDezesseisHexDoEmail()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            GoogleIdentidade primeira = await ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste);
            GoogleIdentidade segunda = await ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTesteMinusculo);
            GoogleIdentidade outro = await ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + "outra.pessoa@exemplo.com");

            Assert.Matches(PadraoSubFake, primeira.Sub);
            Assert.Equal(primeira.Sub, segunda.Sub);
            Assert.NotEqual(primeira.Sub, outro.Sub);
        }

        [Fact]
        public async Task ValidarAsync_EmailNaoVerificado_Recusa()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste + ConstantsGoogle.SufixoNaoVerificadoFake));
        }

        [Fact]
        public async Task ValidarAsync_NaoVerificadoEmMaiusculas_Recusa()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste + ":NAOVERIFICADO"));
        }

        [Theory]
        [InlineData(NomeAmbienteProducao)]
        [InlineData(NomeAmbienteStaging)]
        public async Task ValidarAsync_ForaDeDevelopment_RecusaComIntegrationException(string nomeAmbiente)
        {
            FakeGoogleIdentityProvider provider = CriarProvider(nomeAmbiente, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste));

            Assert.Equal(ConstantsMessage.ErroGoogleFakeForaDoDesenvolvimento, excecao.Message);
        }

        [Theory]
        [InlineData(AmbienteIntegracao.PRODUCAO)]
        [InlineData(AmbienteIntegracao.HOMOLOGACAO)]
        public async Task ValidarAsync_DevelopmentComAmbienteDiferenteDeSandbox_RecusaComIntegrationException(AmbienteIntegracao ambiente)
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste, ambiente));

            Assert.Equal(ConstantsMessage.ErroGoogleFakeForaDoDesenvolvimento, excecao.Message);
        }

        [Theory]
        [InlineData("sem-prefixo@exemplo.com")]
        [InlineData("FAKE:sem-arroba")]
        [InlineData("FAKE:@exemplo.com")]
        [InlineData("FAKE:")]
        public async Task ValidarAsync_CredencialFormatoInvalido_Recusa(string credencial)
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, credencial));
        }

        [Fact]
        public async Task ValidarAsync_EmailAcimaDoLimite_Recusa()
        {
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, new FakeLogService());
            string emailLongo = new string('a', 250) + "@x.com";

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + emailLongo));
        }

        [Fact]
        public async Task ValidarAsync_RecusaNaoGravaEmailNoLog()
        {
            FakeLogService log = new FakeLogService();
            FakeGoogleIdentityProvider provider = CriarProvider(Environments.Development, log);

            await Assert.ThrowsAsync<UnauthorizedException>(() => ValidarAsync(provider, ConstantsGoogle.PrefixoCredencialFake + EmailTeste + ConstantsGoogle.SufixoNaoVerificadoFake));

            Assert.NotEmpty(log.Entries);
            Assert.DoesNotContain(log.Entries, x => x.Contains(EmailTeste, StringComparison.OrdinalIgnoreCase));
        }
    }
}
