using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;

namespace AutoVia.Tests.Integrations
{
    public class ConfiguracaoIntegracaoTests
    {
        private const string Segredo = "segredo-super-secreto";

        private static ConfiguracaoIntegracao CriarConfiguracao()
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "merchant_id", new IntegrationSettingValue("loja-1", false) },
                { "client_secret", new IntegrationSettingValue(Segredo, true) },
                { "webhook_secret", new IntegrationSettingValue(ConstantsValues.MarcadorValorNaoDefinido, true) }
            };

            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Gateway de pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://sandbox.exemplo.com.br",
                30000,
                settings);
        }

        [Fact]
        public void GetRequired_ChaveAusente_LancaExcecaoComIntegracaoEChave()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao();

            IntegrationException exception = Assert.Throws<IntegrationException>(() => configuracao.GetRequired("chave_pix"));

            Assert.Equal("chave_pix", exception.Chave);
            Assert.Contains(ConstantsIntegration.Pagamento, exception.Message);
            Assert.Contains("chave_pix", exception.Message);
        }

        [Fact]
        public void GetRequired_ChaveComMarcadorDefinirNoBanco_LancaExcecaoDeChaveNaoConfigurada()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao();

            IntegrationException exception = Assert.Throws<IntegrationException>(() => configuracao.GetRequired("webhook_secret"));

            Assert.Contains("webhook_secret", exception.Message);
            Assert.Contains("ainda não foi configurada", exception.Message);
        }

        [Fact]
        public void ToString_NuncaExpoeValorProtegido()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao();

            string descricao = configuracao.ToString();

            Assert.DoesNotContain(Segredo, descricao);
            Assert.Contains("client_secret (protegida)", descricao);
        }

        [Fact]
        public void ProtectedValues_TrazSoOsValoresProtegidos()
        {
            ConfiguracaoIntegracao configuracao = CriarConfiguracao();

            Assert.Contains(Segredo, configuracao.ProtectedValues);
            Assert.DoesNotContain("loja-1", configuracao.ProtectedValues);
        }

        [Fact]
        public void SensitiveDataMasker_MascaraValorProtegidoNoTextoDeLog()
        {
            string log = $"POST https://sandbox.exemplo.com.br/oauth?secret={Segredo}";

            string mascarado = SensitiveDataMasker.Mask(log, CriarConfiguracao().ProtectedValues);

            Assert.DoesNotContain(Segredo, mascarado);
            Assert.Contains(ConstantsIntegration.Mascara, mascarado);
        }

        [Fact]
        public void HmacSignature_AssinaturaCorreta_EValidada_EAlterada_ERecusada()
        {
            string payload = "{\"cobranca\":\"123\",\"status\":\"PAGO\"}";
            string assinatura = HmacSignature.ComputeHex(payload, Segredo);

            Assert.True(HmacSignature.Verify(payload, Segredo, assinatura));
            Assert.False(HmacSignature.Verify(payload + " ", Segredo, assinatura));
            Assert.False(HmacSignature.Verify(payload, Segredo, null));
        }
    }
}
