using AutoVia.Core.ServicesInterface;

namespace AutoVia.Tests.Fakes
{
    /// <summary>ILogService em memoria: guarda o que foi logado para os testes conferirem.</summary>
    public class FakeLogService : ILogService
    {
        public List<string> Entries { get; } = new List<string>();

        public void Write(string message, string friendlyMessage, string source)
        {
            Entries.Add($"{friendlyMessage} | {message}");
        }

        public void Write(Exception exception, string friendlyMessage, string source)
        {
            Entries.Add($"{friendlyMessage} | {exception.Message}");
        }

        public void WriteInfo(string message, string source)
        {
            Entries.Add(message);
        }
    }
}
