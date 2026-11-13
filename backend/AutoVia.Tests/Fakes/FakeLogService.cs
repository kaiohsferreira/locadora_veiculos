using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>
    /// ILogService em memoria: guarda o que foi logado para os testes conferirem. Escrita sob trava, porque o servico
    /// hospedado da recuperacao registra em outra thread; Snapshot devolve uma copia segura para leitura nos testes.
    /// </summary>
    public class FakeLogService : ILogService
    {
        private readonly object _trava = new object();

        /// <summary>Entradas gravadas, na ordem. Para ler enquanto outra thread escreve, use Snapshot.</summary>
        public List<string> Entries { get; } = new List<string>();

        /// <summary>Copia das entradas, lida sob trava.</summary>
        public List<string> Snapshot()
        {
            lock (_trava)
            {
                return new List<string>(Entries);
            }
        }

        public void Write(string message, string friendlyMessage, string source)
        {
            Gravar($"{friendlyMessage} | {message}");
        }

        public void Write(Exception exception, string friendlyMessage, string source)
        {
            Gravar($"{friendlyMessage} | {exception.Message}");
        }

        public void WriteInfo(string message, string source)
        {
            Gravar(message);
        }

        private void Gravar(string entrada)
        {
            lock (_trava)
            {
                Entries.Add(entrada);
            }
        }
    }
}
