using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    public sealed class ArmazenamentoGatewayTests
    {
        private const string ChaveObjeto = "vistorias/1/avarias/2/foto";
        private const string SegredoFalso = "AKIA-FAKE-SEGREDO";
        private const string AssinaturaFalsa = "FAKE-ASSINATURA-123";
        private const string UrlFalsa = "https://storage.example/bucket/objeto?X-Amz-Signature=FAKE-ASSINATURA-123";

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

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync("key"));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoProvedorNaoSuportado, ex.Message);
            Assert.Equal(ConstantsArmazenamento.ChaveProvedor, ex.Chave);
            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task ProvedorS3AusenteNaDi_LancaFalhaDeProvedorNaoSuportado()
        {
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), Array.Empty<IArmazenamentoProvider>(), new FakeLogService());

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoProvedorNaoSuportado, ex.Message);
            Assert.Equal(ConstantsArmazenamento.ChaveProvedor, ex.Chave);
        }

        [Fact]
        public async Task Enviar_RepassaBucketChaveBytesContentTypeEPrazoEDevolveOResultadoDoProvedor()
        {
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, new FakeLogService());
            byte[] conteudo = new byte[] { 1, 2, 3 };

            ArquivoArmazenado resultado = await gateway.EnviarAsync(ChaveObjeto, conteudo, "image/png");

            Assert.Equal("autovia-arquivos", provider.Bucket);
            Assert.Equal(ChaveObjeto, provider.Chave);
            Assert.Equal(conteudo, provider.Conteudo);
            Assert.Equal("image/png", provider.ContentType);
            Assert.Equal(ConstantsArmazenamento.ExpiracaoPadraoSegundos, provider.ExpiracaoSegundos);
            Assert.Equal(provider.UrlDevolvida, resultado.Url);
        }

        [Fact]
        public async Task Remover_RepassaBucketEChave_MesmoComPrazoInvalido()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue("abc", false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            await gateway.RemoverAsync(ChaveObjeto);

            Assert.Equal(1, provider.RemoverCount);
            Assert.Equal("autovia-arquivos", provider.Bucket);
            Assert.Equal(ChaveObjeto, provider.Chave);
        }

        [Fact]
        public async Task ExpiracaoAusenteOuEmBranco_UsaPadraoDe900()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue("   ", false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            await gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg");

            Assert.Equal(900, provider.ExpiracaoSegundos);
        }

        [Fact]
        public async Task ExpiracaoDe600_UsaOValorDoBanco()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue("600", false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            ArquivoArmazenado? url = await gateway.ObterUrlAsync(ChaveObjeto);

            Assert.NotNull(url);
            Assert.Equal(600, provider.ExpiracaoSegundos);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("901")]
        [InlineData("abc")]
        public async Task ExpiracaoInvalidaOuAcimaDoTeto_LancaIntegrationExceptionSemChamarProvedor(string valor)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue(valor, false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg"));
            Assert.Equal(ConstantsArmazenamento.ChaveExpiracao, ex.Chave);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoExpiracaoInvalida, ex.Message);

            IntegrationException exUrl = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync(ChaveObjeto));
            Assert.Equal(ConstantsArmazenamento.ChaveExpiracao, exUrl.Chave);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoExpiracaoInvalida, exUrl.Message);

            Assert.Equal(0, provider.CallCount);
        }

        [Fact]
        public async Task Bucket_AusenteUsaPadrao_PresenteUsaValorDoBanco()
        {
            FakeStorageProvider providerPadrao = new FakeStorageProvider();
            ArmazenamentoGateway gatewayPadrao = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { providerPadrao }, new FakeLogService());
            await gatewayPadrao.RemoverAsync(ChaveObjeto);
            Assert.Equal("autovia-arquivos", providerPadrao.Bucket);

            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveBucket] = new IntegrationSettingValue("bucket-do-banco", false)
            };
            FakeStorageProvider providerBanco = new FakeStorageProvider();
            ArmazenamentoGateway gatewayBanco = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { providerBanco }, new FakeLogService());
            await gatewayBanco.RemoverAsync(ChaveObjeto);
            Assert.Equal("bucket-do-banco", providerBanco.Bucket);
        }

        [Fact]
        public async Task FalhaInesperadaDoProvedor_ViraIntegrationExceptionComMensagemDaOperacaoESemInner()
        {
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new InvalidOperationException("falha interna");
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);

            IntegrationException enviar = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg"));
            Assert.Equal(ConstantsIntegration.Armazenamento, enviar.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, enviar.Message);
            Assert.Null(enviar.InnerException);

            IntegrationException obter = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync(ChaveObjeto));
            Assert.Equal(ConstantsIntegration.Armazenamento, obter.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoObterUrl, obter.Message);
            Assert.Null(obter.InnerException);

            IntegrationException remover = await Assert.ThrowsAsync<IntegrationException>(() => gateway.RemoverAsync(ChaveObjeto));
            Assert.Equal(ConstantsIntegration.Armazenamento, remover.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoRemover, remover.Message);
            Assert.Null(remover.InnerException);
        }

        [Fact]
        public async Task IntegrationExceptionDoProvedor_ChegaAoChamadorComMesmaMensagemEMesmaChave()
        {
            string mensagem = string.Format(ConstantsMessageIntegration.ErroChaveObrigatoriaAusente, ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveAccessKey);
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveAccessKey, mensagem);
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.RemoverAsync(ChaveObjeto));

            Assert.Equal(mensagem, ex.Message);
            Assert.Equal(ConstantsArmazenamento.ChaveAccessKey, ex.Chave);
        }

        [Fact]
        public async Task Cancelamento_SaiComoOperationCanceledExceptionENaoViraIntegrationException()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new OperationCanceledException(cancelamento.Token);
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg", cancelamento.Token));
        }

        [Fact]
        public async Task Sucesso_NaoDeixaEntradaNoLog()
        {
            FakeLogService log = new FakeLogService();
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);

            await gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg");
            await gateway.ObterUrlAsync(ChaveObjeto);
            await gateway.RemoverAsync(ChaveObjeto);

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task FalhaComSegredoNaMensagem_LogNaoContemSegredoNemUrl()
        {
            FakeLogService log = new FakeLogService();
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new InvalidOperationException($"access_key={SegredoFalso} secret={SegredoFalso} {UrlFalsa} assinatura={AssinaturaFalsa}");
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(ChaveObjeto, new byte[] { 1 }, "image/jpeg"));

            List<string> entradas = log.Snapshot();
            Assert.NotEmpty(entradas);
            foreach (string entrada in entradas)
            {
                Assert.DoesNotContain(SegredoFalso, entrada);
                Assert.DoesNotContain(AssinaturaFalsa, entrada);
                Assert.DoesNotContain("https://", entrada);
            }
            Assert.Contains("InvalidOperationException", entradas[0]);
        }

        [Fact]
        public async Task IntegrationExceptionDoProvedor_LogTrazMensagemConstanteSemSegredo()
        {
            FakeLogService log = new FakeLogService();
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveSecretKey, ConstantsMessage.ErroArmazenamentoChaveNaoProtegida);
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);

            await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync(ChaveObjeto));

            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.Contains(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, entradas[0]);
            Assert.DoesNotContain(SegredoFalso, entradas[0]);
        }

        [Fact]
        public async Task AssinarUrls_VariasChavesComUmaRepetida_UmaChamadaAoProvedorEUmaEntradaPorChaveDistinta()
        {
            FakeStorageProvider provider = new FakeStorageProvider();
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);
            List<string> chaves = new List<string> { "veiculos/1/fotos/a.jpg", "veiculos/1/fotos/b.jpg", "veiculos/1/fotos/a.jpg" };

            IReadOnlyDictionary<string, ArquivoArmazenado> arquivos = await gateway.AssinarUrlsAsync(chaves);

            Assert.Equal(1, provider.LoteCount);
            Assert.Equal(chaves, provider.ChavesDoLote);
            Assert.Equal("autovia-arquivos", provider.Bucket);
            Assert.Equal(2, arquivos.Count);
            Assert.Equal(UrlFalsa, arquivos["veiculos/1/fotos/a.jpg"].Url);
            Assert.Equal(UrlFalsa, arquivos["veiculos/1/fotos/b.jpg"].Url);
            Assert.Equal(arquivos["veiculos/1/fotos/a.jpg"].ExpiraEm, arquivos["veiculos/1/fotos/b.jpg"].ExpiraEm);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task AssinarUrls_ListaVazia_NaoResolveIntegracaoNemChamaProvedor()
        {
            FakeIntegrationResolverContador resolver = new FakeIntegrationResolverContador(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>()));
            FakeStorageProvider provider = new FakeStorageProvider();
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(resolver, new IArmazenamentoProvider[] { provider }, log);

            IReadOnlyDictionary<string, ArquivoArmazenado> arquivos = await gateway.AssinarUrlsAsync(new List<string>());

            Assert.Empty(arquivos);
            Assert.Equal(0, resolver.ResolveCount);
            Assert.Equal(0, provider.LoteCount);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task AssinarUrls_ExpiracaoDe600_RepassaOValorDoBancoAoProvedor()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue("600", false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            IReadOnlyDictionary<string, ArquivoArmazenado> arquivos = await gateway.AssinarUrlsAsync(new List<string> { ChaveObjeto });

            Assert.Equal(600, provider.ExpiracaoSegundos);
            Assert.Single(arquivos);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("901")]
        [InlineData("abc")]
        public async Task AssinarUrls_ExpiracaoInvalidaOuAcimaDoTeto_RecusadaSemChamarProvedor(string valor)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveExpiracao] = new IntegrationSettingValue(valor, false)
            };
            FakeStorageProvider provider = new FakeStorageProvider();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(settings)), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.AssinarUrlsAsync(new List<string> { ChaveObjeto }));

            Assert.Equal(ConstantsArmazenamento.ChaveExpiracao, ex.Chave);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoExpiracaoInvalida, ex.Message);
            Assert.Equal(0, provider.LoteCount);
        }

        [Fact]
        public async Task AssinarUrls_FalhaDoProvedor_ViraIntegrationExceptionSemInnerESemSegredoNoLog()
        {
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new InvalidOperationException($"access_key={SegredoFalso} secret={SegredoFalso} {UrlFalsa} assinatura={AssinaturaFalsa}");
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, log);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.AssinarUrlsAsync(new List<string> { ChaveObjeto }));

            Assert.Equal(ConstantsIntegration.Armazenamento, ex.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoObterUrl, ex.Message);
            Assert.Null(ex.InnerException);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.Contains("InvalidOperationException", entradas[0]);
            Assert.DoesNotContain(SegredoFalso, entradas[0]);
            Assert.DoesNotContain(AssinaturaFalsa, entradas[0]);
            Assert.DoesNotContain("https://", entradas[0]);
        }

        [Fact]
        public async Task AssinarUrls_Cancelamento_SaiComoOperationCanceledENaoViraIntegrationException()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            FakeStorageProvider provider = new FakeStorageProvider();
            provider.ExcecaoASerLancada = new OperationCanceledException(cancelamento.Token);
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(new FakeIntegrationResolver(CriarConfiguracao(new Dictionary<string, IntegrationSettingValue>())), new IArmazenamentoProvider[] { provider }, new FakeLogService());

            await Assert.ThrowsAsync<OperationCanceledException>(() => gateway.AssinarUrlsAsync(new List<string> { ChaveObjeto }, cancelamento.Token));
        }

        private static ConfiguracaoIntegracao CriarConfiguracao(IReadOnlyDictionary<string, IntegrationSettingValue> settings)
        {
            return new ConfiguracaoIntegracao(ConstantsIntegration.Armazenamento, ConstantsIntegration.Armazenamento, AmbienteIntegracao.SANDBOX, "https://storage.example", 1000, settings);
        }

        private sealed class FakeIntegrationResolver : IIntegrationResolver
        {
            private readonly ConfiguracaoIntegracao _configuracao;
            public FakeIntegrationResolver(ConfiguracaoIntegracao configuracao) { _configuracao = configuracao; }
            public Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult(_configuracao);
            public void Invalidate(string codigo) { }
        }

        private sealed class FakeIntegrationResolverContador : IIntegrationResolver
        {
            private readonly ConfiguracaoIntegracao _configuracao;
            public int ResolveCount { get; private set; }
            public FakeIntegrationResolverContador(ConfiguracaoIntegracao configuracao) { _configuracao = configuracao; }
            public Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default)
            {
                ResolveCount++;
                return Task.FromResult(_configuracao);
            }
            public void Invalidate(string codigo) { }
        }

        private sealed class FakeStorageProvider : IArmazenamentoProvider
        {
            public string Nome => ConstantsArmazenamento.ProvedorS3Compativel;
            public string? Bucket { get; private set; }
            public string? Chave { get; private set; }
            public byte[]? Conteudo { get; private set; }
            public string? ContentType { get; private set; }
            public int ExpiracaoSegundos { get; private set; }
            public int CallCount { get; private set; }
            public int RemoverCount { get; private set; }
            public Exception? ExcecaoASerLancada { get; set; }
            public string UrlDevolvida => UrlFalsa;

            public Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                Registrar(bucket, chave, expiracaoSegundos);
                Conteudo = conteudo;
                ContentType = contentType;
                if (ExcecaoASerLancada != null) throw ExcecaoASerLancada;
                return Task.FromResult(new ArquivoArmazenado(UrlFalsa, DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos)));
            }

            public Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                Registrar(bucket, chave, expiracaoSegundos);
                if (ExcecaoASerLancada != null) throw ExcecaoASerLancada;
                return Task.FromResult<ArquivoArmazenado?>(new ArquivoArmazenado(UrlFalsa, DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos)));
            }

            public Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default)
            {
                Bucket = bucket;
                Chave = chave;
                RemoverCount++;
                if (ExcecaoASerLancada != null) throw ExcecaoASerLancada;
                return Task.CompletedTask;
            }

            public int LoteCount { get; private set; }
            public List<string> ChavesDoLote { get; } = new List<string>();

            public Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(ConfiguracaoIntegracao configuracao, string bucket, IReadOnlyCollection<string> chaves, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                Bucket = bucket;
                ExpiracaoSegundos = expiracaoSegundos;
                CallCount++;
                LoteCount++;
                ChavesDoLote.AddRange(chaves);
                if (ExcecaoASerLancada != null) throw ExcecaoASerLancada;
                DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddSeconds(expiracaoSegundos);
                Dictionary<string, ArquivoArmazenado> arquivos = new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
                foreach (string chave in chaves)
                {
                    arquivos[chave] = new ArquivoArmazenado(UrlFalsa, expiraEm);
                }
                return Task.FromResult<IReadOnlyDictionary<string, ArquivoArmazenado>>(arquivos);
            }

            private void Registrar(string bucket, string chave, int expiracaoSegundos)
            {
                Bucket = bucket;
                Chave = chave;
                ExpiracaoSegundos = expiracaoSegundos;
                CallCount++;
            }
        }
    }
}
