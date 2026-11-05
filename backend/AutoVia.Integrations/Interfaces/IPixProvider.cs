using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor concreto de cobranca PIX (MERCADOPAGO, FAKE). Escolhido pelo PixGateway
    /// pelo valor da chave 'provedor' da integracao PAGAMENTO.
    /// </summary>
    public interface IPixProvider
    {
        /// <summary>Nome do provedor (ConstantsPix.ProvedorMercadoPago ou ProvedorFake).</summary>
        string Nome { get; }

        public Task<PixCobrancaResultado> CriarAsync(ConfiguracaoIntegracao configuracao, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, CancellationToken cancellationToken = default);

        public Task<PixCobrancaResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default);

        public Task<PixCobrancaResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default);
    }
}
