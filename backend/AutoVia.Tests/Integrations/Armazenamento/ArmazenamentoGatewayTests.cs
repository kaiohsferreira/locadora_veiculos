using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    public sealed class ArmazenamentoGatewayTests
    {
        [Fact]
        public async Task ProviderOmitido_UsaS3CompatívelEExpiracaoPadrao()
        {
            FakeStorageProvider provider = new FakeStorageProvider();
            FakeIntegrationResolver resolver = new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>()));
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(resolver, new IArmazenamentoProvider[] { provider }, new FakeLogService());

            ArquivoArmazenado? url = await gateway.ObterUrlAsync("vistorias/1/avarias/2/foto");

            Assert.NotNull(url);
            Assert.Equal(ConstantsArmazenamento.ExpiracaoPadraoSegundos, provider.ExpiracaoSegundos);
            Assert.Equal("autovia-arquivos", provider.Bucket);
        }

        [Fact]
        public async Task ProviderDiferenteDoSuportado_RetornaFalhaDeIntegracao()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveProvedor] = new IntegrationSettingValue("OUTRO", false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync("key"));
            Assert.Equal(0, provider.CallCount);
        }

        private static ConfiguracaoIntegracao CriarConfiguracao(IReadOnlyDictionary<string, IntegrationSettingValue> settings)
        {
            return new ConfiguracaoIntegracao(ConstantsArmazenamento.Codigo, ConstantsArmazenamento.Codigo, AmbienteIntegracao.SANDBOX, "https://storage.example", 1000, settings);
        }

        private sealed class FakeIntegrationResolver : IIntegrationResolver
        {
            private readonly ConfiguracaoIntegracao _configuracao;
            public FakeIntegrationResolver(ConfiguracaoIntegracao configuracao) { _configuracao = configuracao; }
            public Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult(_configuracao);
            public void Invalidate(string codigo) { }
        }

        private sealed class FakeStorageProvider : IArmazenamentoProvider
        {
            public string Nome => ConstantsArmazenamento.ProvedorS3Compativel;
            public string? Bucket { get; private set; }
            public int ExpiracaoSegundos { get; private set; }
            public int CallCount { get; private set; }

            public Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                Bucket = bucket;
                ExpiracaoSegundos = expiracaoSegundos;
                CallCount++;
                return Task.FromResult(new ArquivoArmazenado("https://storage.example/photo", DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos)));
            }

            public Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                Bucket = bucket;
                ExpiracaoSegundos = expiracaoSegundos;
                CallCount++;
                return Task.FromResult<ArquivoArmazenado?>(new ArquivoArmazenado("https://storage.example/photo", DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos)));
            }

            public Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
            {
                Bucket = bucket;
                CallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
