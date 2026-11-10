using AutoVia.Integrations.Communication.Caucao;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway interno de pre-autorizacao de caucao no cartao (P7-INT-01, RF-20). Valida a
    /// solicitacao, escolhe o provedor pela integracao PAGAMENTO, aplica as regras de estado e
    /// nao persiste nada: quem grava e a P4-BE-04 e quem decide baixar ou reter e a P5-BE-04.
    /// </summary>
    public interface ICaucaoGateway
    {
        /// <summary>Reserva o valor no cartao. Nunca captura. Cartao recusado lanca BusinessException.</summary>
        public Task<CaucaoResultado> PreAutorizarAsync(CaucaoSolicitacao solicitacao, CancellationToken cancellationToken = default);

        /// <summary>Consulta o estado atual no provedor, sem efeito colateral.</summary>
        public Task<CaucaoResultado> ConsultarAsync(string identificadorProvedor, CancellationToken cancellationToken = default);

        /// <summary>Captura a caucao reservada. Valor nulo = integral; valor menor = parcial; maior = erro.</summary>
        public Task<CaucaoResultado> CapturarAsync(string identificadorProvedor, decimal? valor = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Libera a reserva (baixa). RESERVADA e EM_ANALISE vao ao provedor (cancelamento); CAPTURADA e ESTORNADA
        /// lancam BusinessException; CANCELADA, EXPIRADA e RECUSADA devolvem o estado atual sem chamar o provedor.
        /// </summary>
        public Task<CaucaoResultado> CancelarAsync(string identificadorProvedor, CancellationToken cancellationToken = default);

        /// <summary>Provedor e chave publica para o front gerar o token do cartao.</summary>
        public Task<CaucaoConfiguracaoCartao> ObterConfiguracaoCartaoAsync(CancellationToken cancellationToken = default);
    }
}
