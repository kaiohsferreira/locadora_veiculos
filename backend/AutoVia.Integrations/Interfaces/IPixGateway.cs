using AutoVia.Integrations.Communication.Pix;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway interno de cobranca PIX (RF-32, parte PIX). Valida a solicitacao, escolhe o
    /// provedor pela integracao PAGAMENTO e nao persiste nada: o status e guardado pelo
    /// modulo de pagamento (P7-BE-02).
    /// </summary>
    public interface IPixGateway
    {
        /// <summary>Cria a cobranca PIX. Mesma ReferenciaExterna devolve a mesma cobranca.</summary>
        public Task<PixCobrancaResultado> CriarCobrancaAsync(PixCobrancaSolicitacao solicitacao, CancellationToken cancellationToken = default);

        /// <summary>Consulta a cobranca pelo identificador do provedor.</summary>
        public Task<PixCobrancaResultado> ConsultarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default);

        /// <summary>Cancela a cobranca. Cobranca paga nao pode ser cancelada.</summary>
        public Task<PixCobrancaResultado> CancelarCobrancaAsync(string identificadorProvedor, CancellationToken cancellationToken = default);
    }
}
