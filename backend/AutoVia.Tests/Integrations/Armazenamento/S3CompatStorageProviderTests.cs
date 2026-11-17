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
    /// <summary>
    /// Testes do provedor S3 SEM REDE: cada caso para em uma conferencia antes de montar a chamada ao storage
    /// (base_url, chave ausente, chave nao configurada, chave nao protegida, cancelamento) ou em uma falha local
    /// (conteudo nulo), entao nenhuma requisicao sai da maquina de teste.
    /// </summary>
    public sealed class S3CompatStorageProviderTests
    {
        private const string BaseUrlHttps = "https://storage.example";
        private const string ValorAccessKey = "AKIA-TESTE-SEGREDO";
        private const string ValorSecretKey = "SEGREDO-TESTE-VALOR";
        private const string BucketTeste = "bucket-teste";
        private const string ChaveObjeto = "vistorias/1/avarias/2/foto";

        [Fact]
        public async Task BaseUrlEmHttp_RecusadaEmTodasAsOperacoes_SemChamadaDeRede()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao("http://storage.example", CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(ConstantsIntegration.Armazenamento, ex.CodigoIntegracao);
                Assert.Equal(ConstantsMessage.ErroArmazenamentoBaseUrlSemHttps, ex.Message);
            }

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task AccessKeyAusente_LancaChaveObrigatoriaAusente_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings.Remove(ConstantsArmazenamento.ChaveAccessKey);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessageIntegration.ErroChaveObrigatoriaAusente, ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveAccessKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveAccessKey, ex.Chave);
            }
        }

        [Fact]
        public async Task AccessKeyComDefinirNoBanco_LancaChaveNaoConfigurada_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings[ConstantsArmazenamento.ChaveAccessKey] = new IntegrationSettingValue(ConstantsValues.MarcadorValorNaoDefinido, true);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessageIntegration.ErroChaveNaoConfigurada, ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveAccessKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveAccessKey, ex.Chave);
            }
        }

        [Fact]
        public async Task AccessKeyNaoProtegida_LancaChaveNaoProtegida_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings[ConstantsArmazenamento.ChaveAccessKey] = new IntegrationSettingValue(ValorAccessKey, false);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, ConstantsArmazenamento.ChaveAccessKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveAccessKey, ex.Chave);
            }
        }

        [Fact]
        public async Task SecretKeyAusente_LancaChaveObrigatoriaAusente_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings.Remove(ConstantsArmazenamento.ChaveSecretKey);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessageIntegration.ErroChaveObrigatoriaAusente, ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveSecretKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveSecretKey, ex.Chave);
            }
        }

        [Fact]
        public async Task SecretKeyComDefinirNoBanco_LancaChaveNaoConfigurada_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings[ConstantsArmazenamento.ChaveSecretKey] = new IntegrationSettingValue(ConstantsValues.MarcadorValorNaoDefinido, true);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessageIntegration.ErroChaveNaoConfigurada, ConstantsIntegration.Armazenamento, ConstantsArmazenamento.ChaveSecretKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveSecretKey, ex.Chave);
            }
        }

        [Fact]
        public async Task SecretKeyNaoProtegida_LancaChaveNaoProtegida_SemChamadaDeRede()
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            settings[ConstantsArmazenamento.ChaveSecretKey] = new IntegrationSettingValue(ValorSecretKey, false);
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, settings);
            S3CompatStorageProvider provider = new S3CompatStorageProvider(new FakeLogService());
            string esperada = string.Format(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, ConstantsArmazenamento.ChaveSecretKey);

            foreach (Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task> operacao in ObterOperacoes())
            {
                IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => operacao(provider, configuracao));
                Assert.Equal(esperada, ex.Message);
                Assert.Equal(ConstantsArmazenamento.ChaveSecretKey, ex.Chave);
            }
        }

        [Fact]
        public async Task TokenJaCancelado_LancaOperationCanceledSemChamadaDeRede()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            // base_url em http: se a conferencia do token viesse depois de CriarCliente, sairia IntegrationException e o teste falharia.
            ConfiguracaoIntegracao configuracao = CriarConfiguracao("http://storage.example", CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.EnviarAsync(configuracao, BucketTeste, ChaveObjeto, new byte[] { 1 }, "image/jpeg", 900, cancelamento.Token));
            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.ObterUrlAsync(configuracao, BucketTeste, ChaveObjeto, 900, cancelamento.Token));
            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.RemoverAsync(configuracao, BucketTeste, ChaveObjeto, cancelamento.Token));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task ConteudoNulo_LancaErroDeEnvioSemInner_ELogTrazSoONomeDoTipo()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provider.EnviarAsync(configuracao, BucketTeste, ChaveObjeto, null!, "image/jpeg", 900));

            Assert.Equal(ConstantsIntegration.Armazenamento, ex.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.Null(ex.InnerException);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.Contains("ArgumentNullException", entradas[0]);
            Assert.DoesNotContain(ValorAccessKey, entradas[0]);
            Assert.DoesNotContain(ValorSecretKey, entradas[0]);
        }

        [Fact]
        public async Task BaseUrlEmHttp_PassandoPeloGateway_MensagemEspecificaChegaAoChamador()
        {
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(
                new FakeIntegrationResolver(CriarConfiguracao("http://storage.example", CriarSettingsValidos())),
                new IArmazenamentoProvider[] { new S3CompatStorageProvider(log) },
                log);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ObterUrlAsync(ChaveObjeto));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoBaseUrlSemHttps, ex.Message);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.Contains(ConstantsMessage.ErroArmazenamentoBaseUrlSemHttps, entradas[0]);
            Assert.DoesNotContain(ValorAccessKey, entradas[0]);
            Assert.DoesNotContain(ValorSecretKey, entradas[0]);
        }

        private const string FalhaAusente = "ausente";
        private const string FalhaDefinirNoBanco = "definir_no_banco";
        private const string FalhaNaoProtegida = "nao_protegida";

        [Theory]
        [InlineData(ConstantsArmazenamento.ChaveAccessKey, FalhaAusente)]
        [InlineData(ConstantsArmazenamento.ChaveAccessKey, FalhaDefinirNoBanco)]
        [InlineData(ConstantsArmazenamento.ChaveAccessKey, FalhaNaoProtegida)]
        [InlineData(ConstantsArmazenamento.ChaveSecretKey, FalhaAusente)]
        [InlineData(ConstantsArmazenamento.ChaveSecretKey, FalhaDefinirNoBanco)]
        [InlineData(ConstantsArmazenamento.ChaveSecretKey, FalhaNaoProtegida)]
        public async Task ChaveInvalida_PassandoPeloGateway_MensagemEspecificaChegaAoChamador(string chave, string falha)
        {
            Dictionary<string, IntegrationSettingValue> settings = CriarSettingsValidos();
            string esperada;
            if (falha == FalhaAusente)
            {
                settings.Remove(chave);
                esperada = string.Format(ConstantsMessageIntegration.ErroChaveObrigatoriaAusente, ConstantsIntegration.Armazenamento, chave);
            }
            else if (falha == FalhaDefinirNoBanco)
            {
                settings[chave] = new IntegrationSettingValue(ConstantsValues.MarcadorValorNaoDefinido, true);
                esperada = string.Format(ConstantsMessageIntegration.ErroChaveNaoConfigurada, ConstantsIntegration.Armazenamento, chave);
            }
            else
            {
                settings[chave] = new IntegrationSettingValue(chave == ConstantsArmazenamento.ChaveAccessKey ? ValorAccessKey : ValorSecretKey, false);
                esperada = string.Format(ConstantsMessage.ErroArmazenamentoChaveNaoProtegida, chave);
            }
            FakeLogService log = new FakeLogService();
            ArmazenamentoGateway gateway = new ArmazenamentoGateway(
                new FakeIntegrationResolver(CriarConfiguracao(BaseUrlHttps, settings)),
                new IArmazenamentoProvider[] { new S3CompatStorageProvider(log) },
                log);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => gateway.RemoverAsync(ChaveObjeto));

            Assert.Equal(esperada, ex.Message);
            Assert.Equal(chave, ex.Chave);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.DoesNotContain(ValorAccessKey, entradas[0]);
            Assert.DoesNotContain(ValorSecretKey, entradas[0]);
        }

        [Fact]
        public void UrlAssinadaSemHttps_LancaErroDeUrlAssinadaInvalida_SemInner()
        {
            IntegrationException ex = Assert.Throws<IntegrationException>(() => S3CompatStorageProvider.MontarArquivoAssinado("http://storage.example/bucket/objeto", DateTimeOffset.UtcNow.AddSeconds(900)));

            Assert.Equal(ConstantsIntegration.Armazenamento, ex.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoUrlAssinadaInvalida, ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void UrlAssinadaEmHttps_DevolveArquivoComMesmaUrlEExpiracao()
        {
            const string urlHttps = "https://storage.example/bucket/objeto?X-Amz-Signature=FAKE";
            DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddSeconds(900);

            ArquivoArmazenado arquivo = S3CompatStorageProvider.MontarArquivoAssinado(urlHttps, expiraEm);

            Assert.Equal(urlHttps, arquivo.Url);
            Assert.Equal(expiraEm, arquivo.ExpiraEm);
        }

        [Fact]
        public async Task AssinarUrls_ListaVazia_DevolveVazioSemValidarConfiguracaoNemChamarRede()
        {
            // base_url em http: se a lista vazia chegasse a CriarCliente, sairia IntegrationException e o teste falharia.
            ConfiguracaoIntegracao configuracao = CriarConfiguracao("http://storage.example", CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            IReadOnlyDictionary<string, ArquivoArmazenado> arquivos = await provider.AssinarUrlsAsync(configuracao, BucketTeste, new List<string>(), 900);

            Assert.Empty(arquivos);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task AssinarUrls_VariasChavesComUmaRepetida_UmaUrlHttpsPorChaveDistintaComMesmaExpiracao()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);
            List<string> chaves = new List<string> { "veiculos/1/fotos/a.jpg", "veiculos/1/fotos/b.jpg", "veiculos/1/fotos/a.jpg" };

            IReadOnlyDictionary<string, ArquivoArmazenado> arquivos = await provider.AssinarUrlsAsync(configuracao, BucketTeste, chaves, 900);

            Assert.Equal(2, arquivos.Count);
            ArquivoArmazenado arquivoA = arquivos["veiculos/1/fotos/a.jpg"];
            ArquivoArmazenado arquivoB = arquivos["veiculos/1/fotos/b.jpg"];
            Assert.StartsWith("https://", arquivoA.Url);
            Assert.StartsWith("https://", arquivoB.Url);
            Assert.Contains(BucketTeste, arquivoA.Url);
            Assert.NotEqual(arquivoA.Url, arquivoB.Url);
            Assert.Equal(arquivoA.ExpiraEm, arquivoB.ExpiraEm);
            Assert.True(arquivoA.ExpiraEm > DateTimeOffset.UtcNow);
            Assert.True(arquivoA.ExpiraEm <= DateTimeOffset.UtcNow.AddSeconds(900));
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task AssinarUrls_ChavesNulas_LogTrazSoONomeDoTipoEFalhaViraIntegrationExceptionSemInner()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao(BaseUrlHttps, CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provider.AssinarUrlsAsync(configuracao, BucketTeste, null!, 900));

            Assert.Equal(ConstantsIntegration.Armazenamento, ex.CodigoIntegracao);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoObterUrl, ex.Message);
            Assert.Null(ex.InnerException);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.Contains("NullReferenceException", entradas[0]);
            Assert.DoesNotContain(ValorAccessKey, entradas[0]);
            Assert.DoesNotContain(ValorSecretKey, entradas[0]);
        }

        [Fact]
        public async Task AssinarUrls_TokenJaCancelado_LancaOperationCanceledSemChamadaDeRede()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            ConfiguracaoIntegracao configuracao = CriarConfiguracao("http://storage.example", CriarSettingsValidos());
            FakeLogService log = new FakeLogService();
            S3CompatStorageProvider provider = new S3CompatStorageProvider(log);

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.AssinarUrlsAsync(configuracao, BucketTeste, new List<string> { ChaveObjeto }, 900, cancelamento.Token));

            Assert.Empty(log.Entries);
        }

        private static IReadOnlyList<Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task>> ObterOperacoes()
        {
            List<Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task>> operacoes = new List<Func<S3CompatStorageProvider, ConfiguracaoIntegracao, Task>>();
            operacoes.Add((provider, configuracao) => provider.EnviarAsync(configuracao, BucketTeste, ChaveObjeto, new byte[] { 1 }, "image/jpeg", 900));
            operacoes.Add((provider, configuracao) => provider.ObterUrlAsync(configuracao, BucketTeste, ChaveObjeto, 900));
            operacoes.Add((provider, configuracao) => provider.RemoverAsync(configuracao, BucketTeste, ChaveObjeto));
            operacoes.Add((provider, configuracao) => provider.AssinarUrlsAsync(configuracao, BucketTeste, new List<string> { ChaveObjeto }, 900));
            return operacoes;
        }

        private static Dictionary<string, IntegrationSettingValue> CriarSettingsValidos()
        {
            return new Dictionary<string, IntegrationSettingValue>
            {
                [ConstantsArmazenamento.ChaveAccessKey] = new IntegrationSettingValue(ValorAccessKey, true),
                [ConstantsArmazenamento.ChaveSecretKey] = new IntegrationSettingValue(ValorSecretKey, true),
                [ConstantsArmazenamento.ChaveBucket] = new IntegrationSettingValue(BucketTeste, false),
                [ConstantsArmazenamento.ChaveRegiao] = new IntegrationSettingValue(ConstantsArmazenamento.RegiaoPadrao, false)
            };
        }

        private static ConfiguracaoIntegracao CriarConfiguracao(string baseUrl, IReadOnlyDictionary<string, IntegrationSettingValue> settings)
        {
            return new ConfiguracaoIntegracao(ConstantsIntegration.Armazenamento, ConstantsIntegration.Armazenamento, AmbienteIntegracao.SANDBOX, baseUrl, 1000, settings);
        }

        private sealed class FakeIntegrationResolver : IIntegrationResolver
        {
            private readonly ConfiguracaoIntegracao _configuracao;
            public FakeIntegrationResolver(ConfiguracaoIntegracao configuracao) { _configuracao = configuracao; }
            public Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult(_configuracao);
            public void Invalidate(string codigo) { }
        }
    }
}
