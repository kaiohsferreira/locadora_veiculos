using AutoVia.Integrations.Communication.Email;
using System.Threading.Channels;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Fila em memoria dos e-mails (singleton, limitada a ConstantsEmail.CapacidadeFila). Quem envia
    /// (ex.: RecuperacaoSenhaService) so enfileira: a requisicao nao espera o envio. Mensagens pendentes
    /// se perdem no restart da aplicacao.
    /// </summary>
    public interface IEmailFila
    {
        /// <summary>
        /// Enfileira a mensagem. Nunca lanca e nunca bloqueia. Devolve false se a fila estiver cheia
        /// (registrado em log) ou se a mensagem for nula.
        /// </summary>
        public bool Enfileirar(EmailMensagem mensagem);

        /// <summary>Leitor da fila, consumido pelo EmailEnvioHostedService.</summary>
        public ChannelReader<EmailMensagem> Leitor { get; }
    }
}
