using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Interfaces;
using System.Threading.Channels;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// IEmailFila em memoria, sem envio: guarda as mensagens enfileiradas para os testes conferirem.
    /// Aceitar = false simula a fila cheia (Enfileirar devolve false e nada e guardado).
    /// Enfileirar pode ser chamado pelo servico hospedado em outra thread: Snapshot devolve uma copia sob trava.
    /// </summary>
    public class FakeEmailFila : IEmailFila
    {
        private readonly Channel<EmailMensagem> _canal = Channel.CreateUnbounded<EmailMensagem>();
        private readonly object _trava = new object();

        /// <summary>Quando false, a fila esta cheia: Enfileirar devolve false.</summary>
        public bool Aceitar { get; set; } = true;

        /// <summary>Mensagens aceitas, na ordem de chegada. Para leitura de outra thread, use Snapshot.</summary>
        public List<EmailMensagem> Mensagens { get; } = new List<EmailMensagem>();

        /// <summary>Quantas vezes Enfileirar foi chamado (aceita ou nao), para os testes provarem que o e-mail nao saiu na requisicao.</summary>
        public int TotalChamadas { get; private set; }

        /// <summary>Leitor do canal. O fake nao escreve nele: o envio real fica fora dos testes.</summary>
        public ChannelReader<EmailMensagem> Leitor => _canal.Reader;

        /// <summary>Copia das mensagens aceitas, lida sob trava (segura mesmo com o servico hospedado escrevendo).</summary>
        public List<EmailMensagem> Snapshot()
        {
            lock (_trava)
            {
                return new List<EmailMensagem>(Mensagens);
            }
        }

        /// <summary>Guarda a mensagem se a fila aceitar; senao devolve false.</summary>
        public bool Enfileirar(EmailMensagem mensagem)
        {
            lock (_trava)
            {
                TotalChamadas++;

                if (!Aceitar)
                {
                    return false;
                }

                Mensagens.Add(mensagem);
            }

            return true;
        }
    }
}
