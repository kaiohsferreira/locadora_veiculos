using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Caucao
{
    /// <summary>
    /// Fluxo ponta a ponta com CaucaoGateway, FakeCaucaoProvider e CaucaoFakeStore REAIS. So a configuracao
    /// (integracao PAGAMENTO em SANDBOX com provedor FAKE) e o log sao de teste. Prova que o gateway aceita o
    /// identificador que o FAKE gera de verdade.
    /// </summary>
    public class CaucaoFakeFluxoTests
    {
        private const string TokenNormal = "tok_cartao_9876543210";

        private static CaucaoGateway CriarGateway(CaucaoFakeStore store)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = AmbienteIntegracao.SANDBOX,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 30000,
                Ativo = true
            };

            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsCaucao.ChaveProvedor, Valor = ConstantsCaucao.ProvedorFake, Protegido = false });

            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integration);

            FakeLogService log = new FakeLogService();
            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);
            ICaucaoProvider[] providers = { new FakeCaucaoProvider(store, log) };

            return new CaucaoGateway(resolver, providers, log);
        }

        private static CaucaoSolicitacao CriarSolicitacao(string referencia)
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = referencia,
                Valor = 500m,
                Descricao = "Caucao da locacao",
                TokenCartao = TokenNormal,
                MetodoPagamentoId = "visa",
                Pagador = new CaucaoPagador { Email = "maria@exemplo.com" }
            };
        }

        [Fact]
        public async Task PreAutorizarConsultarCapturarParcial_ComFakeReal_AceitaOIdentificadorGerado()
        {
            CaucaoGateway gateway = CriarGateway(new CaucaoFakeStore());

            CaucaoResultado reservada = await gateway.PreAutorizarAsync(CriarSolicitacao("CAUCAO-LOC-123"));
            string identificador = reservada.IdentificadorProvedor;

            Assert.Matches("^FAKE-CAU-[0-9a-f]{12}$", identificador);
            Assert.Equal(CaucaoStatus.RESERVADA, reservada.Status);
            Assert.NotNull(reservada.ReservaValidaAte);

            CaucaoResultado consultada = await gateway.ConsultarAsync(identificador);
            Assert.Equal(CaucaoStatus.RESERVADA, consultada.Status);

            CaucaoResultado capturada = await gateway.CapturarAsync(identificador, 200m);
            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Equal(200m, capturada.ValorCapturado);
            Assert.Null(capturada.ReservaValidaAte);
        }

        [Fact]
        public async Task PreAutorizarECancelar_ComFakeReal_OutraCaucaoFicaCancelada()
        {
            CaucaoGateway gateway = CriarGateway(new CaucaoFakeStore());

            CaucaoResultado reservada = await gateway.PreAutorizarAsync(CriarSolicitacao("CAUCAO-LOC-456"));

            CaucaoResultado cancelada = await gateway.CancelarAsync(reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, cancelada.Status);
            Assert.Null(cancelada.ReservaValidaAte);
        }
    }
}
