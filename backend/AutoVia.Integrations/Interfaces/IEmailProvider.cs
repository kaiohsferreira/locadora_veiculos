using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Email;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor concreto de e-mail (FAKE nesta tarefa; o real entra na T-INT-01). Escolhido pelo
    /// EmailGateway pelo valor da chave 'provedor' da integracao EMAIL.
    /// </summary>
    public interface IEmailProvider
    {
        /// <summary>Nome do provedor (valor da chave 'provedor', ex.: FAKE).</summary>
        string Nome { get; }

        /// <summary>Entrega a mensagem ja validada. Assunto e corpo chegam prontos de quem chama.</summary>
        public Task EnviarAsync(ConfiguracaoIntegracao configuracao, EmailMensagem mensagem, string remetenteEmail, string remetenteNome, CancellationToken cancellationToken = default);
    }
}
