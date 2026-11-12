using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using System.Threading.Channels;

namespace AutoVia.Integrations.Services.Email
{
    /// <summary>
    /// Fila em memoria (singleton) com capacidade ConstantsEmail.CapacidadeFila. Enfileirar usa TryWrite:
    /// nunca bloqueia a requisicao e nunca lanca. Cheia ou nula devolve false e registra em log.
    /// </summary>
    public class EmailFila : IEmailFila
    {
        private readonly Channel<EmailMensagem> _canal;
        private readonly ILogService _logService;

        public EmailFila(ILogService logService)
        {
            _logService = logService;
            _canal = Channel.CreateBounded<EmailMensagem>(new BoundedChannelOptions(ConstantsEmail.CapacidadeFila)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
        }

        public ChannelReader<EmailMensagem> Leitor => _canal.Reader;

        /// <summary>Enfileira sem bloquear. false = fila cheia ou mensagem nula (registrado em log); nunca lanca.</summary>
        public bool Enfileirar(EmailMensagem mensagem)
        {
            try
            {
                if (mensagem == null)
                {
                    _logService.Write(ConstantsMessage.ErroEmailMensagemInvalida, ConstantsMessage.ErroEmailMensagemInvalida, GetType().ToString());
                    return false;
                }

                bool enfileirada = _canal.Writer.TryWrite(mensagem);

                if (!enfileirada)
                {
                    _logService.Write(ConstantsMessage.ErroEmailFilaCheia, ConstantsMessage.ErroEmailFilaCheia, GetType().ToString());
                }

                return enfileirada;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailProcessarFila, GetType().ToString());
                return false;
            }
        }
    }
}
