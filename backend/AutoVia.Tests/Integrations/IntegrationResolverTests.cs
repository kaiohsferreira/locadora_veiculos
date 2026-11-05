using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations
{
    public class IntegrationResolverTests
    {
        private static Integration CriarIntegracao(string codigo, bool ativo = true, string baseUrl = "https://sandbox.exemplo.com.br")
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = codigo,
                Nome = "Integracao de teste",
                Ambiente = AmbienteIntegracao.SANDBOX,
                BaseUrl = baseUrl,
                TimeoutMs = 15000,
                Ativo = ativo
            };

            integration.Settings.Add(new IntegrationSetting { Chave = "client_id", Valor = "cliente-123", Protegido = false });
            integration.Settings.Add(new IntegrationSetting { Chave = "client_secret", Valor = "segredo-super-secreto", Protegido = true });

            return integration;
        }

        private static IntegrationResolver CriarResolver(FakeIntegrationRepository repository)
        {
            return new IntegrationResolver(repository, new MemoryCache(new MemoryCacheOptions()), new FakeLogService());
        }

        [Fact]
        public async Task ResolveAsync_IntegracaoAtiva_DevolveConfiguracaoDoBanco()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Pagamento));
            IntegrationResolver resolver = CriarResolver(repository);

            ConfiguracaoIntegracao configuracao = await resolver.ResolveAsync("pagamento");

            Assert.Equal(ConstantsIntegration.Pagamento, configuracao.Codigo);
            Assert.Equal("https://sandbox.exemplo.com.br", configuracao.BaseUrl);
            Assert.Equal(15000, configuracao.TimeoutMs);
            Assert.Equal("cliente-123", configuracao.GetRequired("client_id"));
            Assert.True(configuracao.IsProtected("client_secret"));
        }

        [Fact]
        public async Task ResolveAsync_SegundaChamada_UsaCacheSemConsultarOBanco()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Email));
            IntegrationResolver resolver = CriarResolver(repository);

            await resolver.ResolveAsync(ConstantsIntegration.Email);
            await resolver.ResolveAsync(ConstantsIntegration.Email);

            Assert.Equal(1, repository.QueryCount);
        }

        [Fact]
        public async Task Invalidate_ForcaNovaConsultaAoBanco()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Email));
            IntegrationResolver resolver = CriarResolver(repository);

            await resolver.ResolveAsync(ConstantsIntegration.Email);
            resolver.Invalidate(ConstantsIntegration.Email);
            await resolver.ResolveAsync(ConstantsIntegration.Email);

            Assert.Equal(2, repository.QueryCount);
        }

        [Fact]
        public async Task ResolveAsync_IntegracaoInativa_LancaExcecaoDizendoQualIntegracao()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Multas, ativo: false));
            IntegrationResolver resolver = CriarResolver(repository);

            IntegrationException exception = await Assert.ThrowsAsync<IntegrationException>(
                () => resolver.ResolveAsync(ConstantsIntegration.Multas));

            Assert.Equal(ConstantsIntegration.Multas, exception.CodigoIntegracao);
            Assert.Contains(ConstantsIntegration.Multas, exception.Message);
            Assert.Contains("inativa", exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_IntegracaoInexistente_LancaExcecao()
        {
            IntegrationResolver resolver = CriarResolver(new FakeIntegrationRepository());

            IntegrationException exception = await Assert.ThrowsAsync<IntegrationException>(
                () => resolver.ResolveAsync(ConstantsIntegration.Seguradora));

            Assert.Contains(ConstantsIntegration.Seguradora, exception.Message);
        }

        [Fact]
        public async Task ResolveAsync_BaseUrlInvalida_LancaExcecao()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Documento, baseUrl: "nao-e-uma-url"));
            IntegrationResolver resolver = CriarResolver(repository);

            await Assert.ThrowsAsync<IntegrationException>(() => resolver.ResolveAsync(ConstantsIntegration.Documento));
        }

        [Fact]
        public async Task ResolveAsync_IntegracaoInativa_NaoEntraNoCache()
        {
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(CriarIntegracao(ConstantsIntegration.Multas, ativo: false));
            IntegrationResolver resolver = CriarResolver(repository);

            await Assert.ThrowsAsync<IntegrationException>(() => resolver.ResolveAsync(ConstantsIntegration.Multas));
            await Assert.ThrowsAsync<IntegrationException>(() => resolver.ResolveAsync(ConstantsIntegration.Multas));

            Assert.Equal(2, repository.QueryCount);
        }
    }
}
