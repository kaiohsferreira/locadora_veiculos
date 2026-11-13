using AutoVia.Integrations.Communication.Seguradora;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway interno da integracao SEGURADORA (P9-INT-01). Valida os pedidos, escolhe o
    /// provedor pela configuracao SEGURADORA e nao persiste nada: quem grava o protocolo e
    /// a linha do tempo sao as tarefas P9-BE-02 e P9-BE-03.
    /// </summary>
    public interface ISeguradoraGateway
    {
        /// <summary>Informa se a apolice estava vigente na data. BusinessException para entrada invalida; IntegrationException para configuracao ou provedor invalidos.</summary>
        public Task<VigenciaApoliceResultado> ConsultarVigenciaAsync(string numeroApolice, DateOnly dataReferencia, CancellationToken cancellationToken = default);

        /// <summary>Abre o sinistro na seguradora e devolve o protocolo. Mesma ReferenciaExterna devolve o mesmo protocolo.</summary>
        public Task<SinistroSeguradoraResultado> AbrirSinistroAsync(SinistroAberturaSolicitacao solicitacao, CancellationToken cancellationToken = default);

        /// <summary>Consulta o status e a linha do tempo do sinistro pelo protocolo. NotFoundException se o protocolo nao existir.</summary>
        public Task<SinistroSeguradoraResultado> ConsultarSinistroAsync(string protocolo, CancellationToken cancellationToken = default);
    }
}
