using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Documento;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Documento;
using AutoVia.Integrations.Services.Documento;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Integrations.Documento
{
    public class FakeDocumentoOcrProviderTests
    {
        private static ConfiguracaoIntegracao CriarConfiguracao()
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Documento,
                "Documento",
                AmbienteIntegracao.SANDBOX,
                "https://sandbox.exemplo.com.br",
                30000,
                new Dictionary<string, IntegrationSettingValue>());
        }

        private static CnhImagem CriarImagem(string nomeArquivo)
        {
            return new CnhImagem
            {
                Conteudo = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                ContentType = ConstantsDocumento.TipoJpeg,
                NomeArquivo = nomeArquivo
            };
        }

        [Fact]
        public void Nome_EhFake()
        {
            FakeDocumentoOcrProvider provider = new FakeDocumentoOcrProvider(new FakeLogService());

            Assert.Equal("FAKE", provider.Nome);
        }

        [Fact]
        public async Task AnalisarCnhAsync_ImagemComum_DevolveCnhSinteticaComConfianca097()
        {
            FakeDocumentoOcrProvider provider = new FakeDocumentoOcrProvider(new FakeLogService());

            CnhOcrBruto bruto = await provider.AnalisarCnhAsync(CriarConfiguracao(), CriarImagem("cnh-frente.jpg"));

            Assert.Equal("12345678901", bruto.Numero);
            Assert.Equal(0.97m, bruto.ConfiancaNumero);
            Assert.Equal("B", bruto.Categoria);
            Assert.Equal(0.97m, bruto.ConfiancaCategoria);
            Assert.Equal("2030-12-31", bruto.Validade);
            Assert.Equal(0.97m, bruto.ConfiancaValidade);
        }

        [Fact]
        public async Task AnalisarCnhAsync_NomeComBaixaConfianca_DevolveConfianca060()
        {
            FakeDocumentoOcrProvider provider = new FakeDocumentoOcrProvider(new FakeLogService());

            CnhOcrBruto bruto = await provider.AnalisarCnhAsync(CriarConfiguracao(), CriarImagem("CNH-BAIXA-CONFIANCA.jpg"));

            Assert.Equal(0.60m, bruto.ConfiancaNumero);
            Assert.Equal(0.60m, bruto.ConfiancaCategoria);
            Assert.Equal(0.60m, bruto.ConfiancaValidade);
        }

        [Fact]
        public async Task AnalisarCnhAsync_CancelamentoSolicitado_LancaOperationCanceled()
        {
            FakeDocumentoOcrProvider provider = new FakeDocumentoOcrProvider(new FakeLogService());
            CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.AnalisarCnhAsync(CriarConfiguracao(), CriarImagem("cnh.jpg"), cancelamento.Token));
        }
    }
}
