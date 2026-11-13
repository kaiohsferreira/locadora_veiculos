using AutoVia.Integrations.Communication.Email;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway interno de e-mail transacional (BE-3, D2-B). Valida a mensagem, resolve a integracao EMAIL,
    /// escolhe o IEmailProvider pela chave 'provedor' e entrega. Nao persiste nada.
    /// </summary>
    public interface IEmailGateway
    {
        /// <summary>Envia a mensagem pelo provedor configurado. Mensagem invalida lanca BusinessException (409).</summary>
        public Task EnviarAsync(EmailMensagem mensagem, CancellationToken cancellationToken = default);
    }
}
