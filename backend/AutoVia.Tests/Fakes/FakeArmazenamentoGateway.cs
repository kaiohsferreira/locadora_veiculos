using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Tests.Fakes
{
    public sealed class FakeArmazenamentoGateway : IArmazenamentoGateway
    {
        private readonly HashSet<string> _objetos = new HashSet<string>(StringComparer.Ordinal);
        public int UploadCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int HeadCount { get; private set; }
        public bool FalharUpload { get; set; }
        public List<string> UploadKeys { get; } = new List<string>();
        public bool ObjetoExiste(string chave) => _objetos.Contains(chave);

        public Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default)
        {
            UploadCount++;
            UploadKeys.Add(chave);
            if (FalharUpload) throw new InvalidOperationException("Storage unavailable");
            _objetos.Add(chave);
            return Task.FromResult(CriarUrl());
        }

        public Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default)
        {
            HeadCount++;
            return Task.FromResult(_objetos.Contains(chave) ? CriarUrl() : null);
        }

        public Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
        {
            DeleteCount++;
            _objetos.Remove(chave);
            return Task.CompletedTask;
        }

        private static ArquivoArmazenado CriarUrl() => new ArquivoArmazenado("https://storage.example/foto", DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
