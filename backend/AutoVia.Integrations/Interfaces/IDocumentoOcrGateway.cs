using AutoVia.Integrations.Communication.Documento;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway interno de leitura da CNH (RF-09). Valida a imagem, escolhe o
    /// provedor pela configuracao DOCUMENTO e aplica a regra de confianca.
    /// Consumidor previsto: P2-BE-04.
    /// </summary>
    public interface IDocumentoOcrGateway
    {
        /// <summary>
        /// Le numero, categoria e validade da CNH. Lanca BusinessException para imagem
        /// invalida e IntegrationException para configuracao ou provedor invalidos.
        /// </summary>
        public Task<CnhLeituraResultado> LerCnhAsync(CnhImagem imagem, CancellationToken cancellationToken = default);
    }
}
