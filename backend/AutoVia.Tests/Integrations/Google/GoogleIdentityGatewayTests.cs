using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Google;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Extensions;
using AutoVia.Integrations.Helpers.Google;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Google;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Tests.Integrations.Google
{
    /// <summary>
    /// Gateway de identidade do Google: resolucao da integracao GOOGLE, escolha do provedor, configuracao publica
    /// nos tres estados (desabilitado, real, teste) e log sem dado pessoal. Sem rede: JWKS por IComunicacaoService falso.
    /// </summary>
    public class GoogleIdentityGatewayTests
    {
        private const string ProvedorDesconhecido = "PROVEDOR-DESCONHECIDO";
        private const string EmailTeste = "Maria.Teste@Exemplo.com";
        private const string EmailTesteMinusculo = "maria.teste@exemplo.com";
        private const string PadraoSubFake = "^fake-[0-9a-f]{16}$";

        private static GoogleIdentityGateway CriarGateway(FakeIntegrationRepository repositorio, string nomeAmbiente, FakeLogService log, GoogleProvaDeTeste.ComunicacaoGoogleFalsa comunicacao)
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            IHostEnvironment ambiente = new GoogleProvaDeTeste.AmbienteFalso(nomeAmbiente);
            IntegrationResolver resolver = new IntegrationResolver(repositorio, cache, log);

            IEnumerable<IGoogleIdentityProvider> provedores = new List<IGoogleIdentityProvider>
            {
                new GoogleIdentityProvider(comunicacao, cache, log),
                new FakeGoogleIdentityProvider(ambiente, log)
            };

            return new GoogleIdentityGateway(resolver, provedores, ambiente, log);
        }

        private static FakeIntegrationRepository RepositorioCom(params Integration[] integracoes)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            foreach (Integration integracao in integracoes)
            {
                repositorio.Add(integracao);
            }

            return repositorio;
        }

        private static GoogleProvaDeTeste.ComunicacaoGoogleFalsa ComunicacaoComChavePrincipal()
        {
            return new GoogleProvaDeTeste.ComunicacaoGoogleFalsa
            {
                Jwks = GoogleProvaDeTeste.MontarJwks((GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal))
            };
        }

        // ------------------------------------------------------------------
        // Configuracao publica: tres estados e falhas de integracao
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_ProvedorGoogleComClientIdReal_HabilitadaSemModoTeste()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.True(configuracao.Habilitado);
            Assert.Equal(GoogleProvaDeTeste.ClientId, configuracao.ClientId);
            Assert.False(configuracao.ModoTeste);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_FakeEmDevelopmentComSandbox_HabilitadaComModoTeste()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.True(configuracao.Habilitado);
            Assert.Equal(GoogleProvaDeTeste.ClientId, configuracao.ClientId);
            Assert.True(configuracao.ModoTeste);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_FakeForaDeDevelopment_Desabilitada()
        {
            FakeLogService log = new FakeLogService();
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, log, ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
            Assert.Null(configuracao.ClientId);
            Assert.False(configuracao.ModoTeste);
            Assert.NotEmpty(log.Entries);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_FakeEmDevelopmentComAmbienteProducao_Desabilitada()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId, ambiente: AmbienteIntegracao.PRODUCAO));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
            Assert.Null(configuracao.ClientId);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_SemClientId_Desabilitada()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, null));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
            Assert.Null(configuracao.ClientId);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_ClientIdDefinirNoBanco_Desabilitada()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, ConstantsValues.MarcadorValorNaoDefinido));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
            Assert.Null(configuracao.ClientId);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_IntegracaoInativa_Desabilitada()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId, ativo: false));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_IntegracaoInexistente_Desabilitada()
        {
            GoogleIdentityGateway gateway = CriarGateway(RepositorioCom(), Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
        }

        [Fact]
        public async Task ObterConfiguracaoPublicaAsync_ProvedorDesconhecido_Desabilitada()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ProvedorDesconhecido, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleConfiguracaoPublica configuracao = await gateway.ObterConfiguracaoPublicaAsync();

            Assert.False(configuracao.Habilitado);
            Assert.Null(configuracao.ClientId);
        }

        // ------------------------------------------------------------------
        // Validacao da credencial pelo gateway
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ValidarCredencialAsync_CredencialVazia_Unauthorized(string credencial)
        {
            GoogleIdentityGateway gateway = CriarGateway(RepositorioCom(), Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            await Assert.ThrowsAsync<UnauthorizedException>(() => gateway.ValidarCredencialAsync(credencial));
        }

        [Fact]
        public async Task ValidarCredencialAsync_CredencialAcimaDoLimite_Unauthorized()
        {
            GoogleIdentityGateway gateway = CriarGateway(RepositorioCom(), Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            await Assert.ThrowsAsync<UnauthorizedException>(() => gateway.ValidarCredencialAsync(new string('a', ConstantsGoogle.TamanhoMaximoCredencial + 1)));
        }

        [Fact]
        public async Task ValidarCredencialAsync_SemClientId_LancaIntegrationException()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, null));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ValidarCredencialAsync(GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao())));

            Assert.Equal(ConstantsMessage.ErroGoogleIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task ValidarCredencialAsync_ClientIdDefinirNoBanco_LancaIntegrationException()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, ConstantsValues.MarcadorValorNaoDefinido));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste));
        }

        [Fact]
        public async Task ValidarCredencialAsync_IntegracaoInativa_LancaIntegrationException()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId, ativo: false));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.ValidarCredencialAsync(GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao())));
        }

        [Fact]
        public async Task ValidarCredencialAsync_ProvedorDesconhecido_LancaIntegrationException()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ProvedorDesconhecido, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste));

            Assert.Equal(ConstantsMessage.ErroGoogleProvedorNaoSuportado, excecao.Message);
        }

        [Fact]
        public async Task ValidarCredencialAsync_FakeForaDeDevelopment_LancaIntegrationExceptionNaoAceita()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste));

            Assert.Equal(ConstantsMessage.ErroGoogleFakeForaDoDesenvolvimento, excecao.Message);
        }

        [Fact]
        public async Task ValidarCredencialAsync_FakeEmDevelopmentComSandbox_AceitaEEmailNaoVerificadoRecusa()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleIdentidade identidade = await gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste);

            Assert.Equal(EmailTesteMinusculo, identidade.Email);
            Assert.True(identidade.EmailVerificado);
            Assert.Matches(PadraoSubFake, identidade.Sub);

            await Assert.ThrowsAsync<UnauthorizedException>(() => gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste + ConstantsGoogle.SufixoNaoVerificadoFake));
        }

        [Fact]
        public async Task ValidarCredencialAsync_ProvedorGoogle_ProvaValidaDevolveIdentidade()
        {
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorGoogle, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Production, new FakeLogService(), ComunicacaoComChavePrincipal());

            GoogleIdentidade identidade = await gateway.ValidarCredencialAsync(GoogleProvaDeTeste.AssinarRs256(GoogleProvaDeTeste.ChavePrincipal, GoogleProvaDeTeste.KidPrincipal, GoogleProvaDeTeste.ClaimsPadrao()));

            Assert.Equal(GoogleProvaDeTeste.Sub, identidade.Sub);
            Assert.Equal(GoogleProvaDeTeste.EmailMinusculo, identidade.Email);
        }

        [Fact]
        public void AddGoogleIntegration_ResolveGatewayComOsDoisProvedores()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton<IComunicacaoService>(new GoogleProvaDeTeste.ComunicacaoGoogleFalsa());
            services.AddSingleton<ILogService>(new FakeLogService());
            services.AddSingleton<IHostEnvironment>(new GoogleProvaDeTeste.AmbienteFalso(Environments.Development));
            services.AddScoped<IIntegrationRepository, FakeIntegrationRepository>();
            services.AddScoped<IIntegrationResolver, IntegrationResolver>();
            services.AddGoogleIntegration();

            using ServiceProvider raiz = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
            using IServiceScope escopo = raiz.CreateScope();

            IGoogleIdentityGateway gateway = escopo.ServiceProvider.GetRequiredService<IGoogleIdentityGateway>();
            List<IGoogleIdentityProvider> provedores = escopo.ServiceProvider.GetServices<IGoogleIdentityProvider>().ToList();

            Assert.IsType<GoogleIdentityGateway>(gateway);
            Assert.Equal(2, provedores.Count);
            Assert.Contains(provedores, x => x is GoogleIdentityProvider);
            Assert.Contains(provedores, x => x is FakeGoogleIdentityProvider);
        }

        [Fact]
        public async Task ValidarCredencialAsync_SucessoELancamentosDeLogSemDadoPessoal()
        {
            FakeLogService log = new FakeLogService();
            FakeIntegrationRepository repositorio = RepositorioCom(GoogleProvaDeTeste.IntegracaoGoogle(ConstantsGoogle.ProvedorFake, GoogleProvaDeTeste.ClientId));
            GoogleIdentityGateway gateway = CriarGateway(repositorio, Environments.Development, log, ComunicacaoComChavePrincipal());

            GoogleIdentidade identidade = await gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste);
            await Assert.ThrowsAsync<UnauthorizedException>(() => gateway.ValidarCredencialAsync(ConstantsGoogle.PrefixoCredencialFake + EmailTeste + ConstantsGoogle.SufixoNaoVerificadoFake));

            Assert.NotEmpty(log.Entries);
            Assert.DoesNotContain(log.Entries, x => x.Contains(identidade.Sub, StringComparison.Ordinal));
            Assert.DoesNotContain(log.Entries, x => x.Contains(EmailTeste, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(log.Entries, x => x.Contains(EmailTesteMinusculo, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(log.Entries, x => x.Contains("maria.teste", StringComparison.OrdinalIgnoreCase));
        }
    }
}
