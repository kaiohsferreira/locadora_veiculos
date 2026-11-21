using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Tests.Integrations.Pix
{
    /// <summary>
    /// Decisao 1 de 21/11/2026 ('Bloqueado, com chave'): o FAKE do PIX so responde com a chave
    /// Pagamentos:PermitirProvedorFake verdadeira, em Development e na integracao SANDBOX. Cobre a regra pura, a leitura
    /// da chave e a recusa do provedor, que nao grava nada no PixFakeStore.
    /// </summary>
    public class ChaveProvedorFakePixTests
    {
        private const string Referencia = "PED-2026-0100";

        private static ConfiguracaoIntegracao CriarConfiguracao(AmbienteIntegracao ambiente)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                ambiente,
                "https://sandbox.exemplo.com.br",
                15000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        private static PixCobrancaSolicitacao CriarSolicitacao()
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = 10.5m,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador { Nome = "Maria da Silva", Email = "maria@exemplo.com" }
            };
        }

        /// <summary>Configuracao em memoria com a chave no valor dado; null = chave ausente.</summary>
        private static IConfiguration CriarConfiguracaoDaChave(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valor != null)
            {
                valores.Add(ConstantsPix.ChavePermitirProvedorFake, valor);
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        private static FakePixProvider CriarProvider(PixFakeStore store, string ambienteDeHospedagem, string? valorDaChave)
        {
            return new FakePixProvider(store, new GoogleProvaDeTeste.AmbienteFalso(ambienteDeHospedagem), CriarConfiguracaoDaChave(valorDaChave), new FakeLogService());
        }

        [Theory]
        [InlineData(true, AmbienteIntegracao.SANDBOX, true, true)]
        [InlineData(true, AmbienteIntegracao.SANDBOX, false, false)]
        [InlineData(true, AmbienteIntegracao.HOMOLOGACAO, true, false)]
        [InlineData(true, AmbienteIntegracao.PRODUCAO, true, false)]
        [InlineData(true, AmbienteIntegracao.PRODUCAO, false, false)]
        [InlineData(false, AmbienteIntegracao.SANDBOX, true, false)]
        [InlineData(false, AmbienteIntegracao.SANDBOX, false, false)]
        [InlineData(false, AmbienteIntegracao.PRODUCAO, true, false)]
        [InlineData(false, AmbienteIntegracao.PRODUCAO, false, false)]
        public void PodeResponder_SoVerdadeiroComAsTresCondicoes(bool ehDesenvolvimento, AmbienteIntegracao ambiente, bool chavePermitida, bool esperado)
        {
            Assert.Equal(esperado, RegrasProvedorFakePix.PodeResponder(ehDesenvolvimento, ambiente, chavePermitida));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void LerChave_SoVerdadeiroComTextoVerdadeiro(string? valor, bool esperado)
        {
            Assert.Equal(esperado, RegrasProvedorFakePix.LerChave(CriarConfiguracaoDaChave(valor)));
        }

        [Fact]
        public void Padrao_DaChaveEhFalso()
        {
            Assert.False(ConstantsPix.PermitirProvedorFakePadrao);
            Assert.Equal("Pagamentos:PermitirProvedorFake", ConstantsPix.ChavePermitirProvedorFake);
        }

        [Fact]
        public void TextoDaRecusaDoFake_CitaAChaveEAsTresCondicoes()
        {
            Assert.Contains(ConstantsPix.ChavePermitirProvedorFake, ConstantsMessage.ErroPixFakeForaDoSandbox);
            Assert.Contains("SANDBOX", ConstantsMessage.ErroPixFakeForaDoSandbox);
            Assert.Contains("desenvolvimento", ConstantsMessage.ErroPixFakeForaDoSandbox);
        }

        [Fact]
        public async Task CriarAsync_ChaveAusente_RecusaMesmoEmDevelopmentESandbox()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Development, null);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarSolicitacao(), 900));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task CriarAsync_ChaveFalse_RecusaMesmoEmDevelopmentESandbox()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Development, "false");

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarSolicitacao(), 900));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task CriarAsync_ChaveVerdadeiraForaDeDevelopment_RecusaENaoGrava()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Production, "true");

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarSolicitacao(), 900));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task CriarAsync_ChaveVerdadeiraComAmbientePRODUCAO_RecusaENaoGrava()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Development, "true");

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(AmbienteIntegracao.PRODUCAO), CriarSolicitacao(), 900));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task ConsultarECancelar_ChaveAusente_RecusamAntesDeTocarNoEstado()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Development, null);

            IntegrationException consulta = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), "FAKE-000000000000"));
            IntegrationException cancelamento = await Assert.ThrowsAsync<IntegrationException>(() => provider.CancelarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), "FAKE-000000000000"));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, consulta.Message);
            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, cancelamento.Message);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task CriarAsync_ChaveVerdadeiraEmDevelopmentESandbox_Responde()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store, Environments.Development, "true");

            PixCobrancaResultado resultado = await provider.CriarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarSolicitacao(), 900);

            Assert.Equal(ConstantsPix.ProvedorFake, resultado.Provedor);
            Assert.Equal(1, store.Quantidade);
        }
    }
}
