using AutoVia.Communication.CustomExceptions;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers;
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

        /// <summary>Conteudo recebido em cada EnviarAsync, na ordem (prova que vai o arquivo recodificado, nao o original).</summary>
        public List<byte[]> UploadConteudos { get; } = new List<byte[]>();

        /// <summary>Content-Type recebido em cada EnviarAsync, na ordem.</summary>
        public List<string> UploadContentTypes { get; } = new List<string>();

        public bool ObjetoExiste(string chave) => _objetos.Contains(chave);

        /// <summary>Quantas vezes AssinarUrlsAsync foi chamado com pelo menos uma chave.</summary>
        public int AssinaturaLoteCount { get; private set; }

        /// <summary>Todas as chaves pedidas em AssinarUrlsAsync, na ordem.</summary>
        public List<string> ChavesAssinadas { get; } = new List<string>();

        /// <summary>Quando verdadeiro, AssinarUrlsAsync lanca a falha de integracao generica de assinatura.</summary>
        public bool FalharAssinatura { get; set; }

        /// <summary>Quando verdadeiro, RemoverAsync lanca a falha de integracao generica de remocao e o objeto continua existindo.</summary>
        public bool FalharRemocao { get; set; }

        /// <summary>As chaves pedidas em RemoverAsync, na ordem.</summary>
        public List<string> RemoveKeys { get; } = new List<string>();

        /// <summary>Quando verdadeiro, EnviarAsync lanca IntegrationException (falha do armazenamento, como o gateway real: vira 502).</summary>
        public bool FalharEnvioIntegracao { get; set; }

        public Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default)
        {
            UploadCount++;
            UploadKeys.Add(chave);
            UploadConteudos.Add(conteudo);
            UploadContentTypes.Add(contentType);
            if (FalharUpload) throw new InvalidOperationException("Storage unavailable");
            if (FalharEnvioIntegracao) throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoEnviar);
            _objetos.Add(chave);
            return Task.FromResult(CriarUrl());
        }

        public Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default)
        {
            HeadCount++;
            return Task.FromResult(_objetos.Contains(chave) ? CriarUrl() : null);
        }

        public Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default)
        {
            if (chaves.Count == 0)
            {
                return Task.FromResult<IReadOnlyDictionary<string, ArquivoArmazenado>>(new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal));
            }

            AssinaturaLoteCount++;
            ChavesAssinadas.AddRange(chaves);
            if (FalharAssinatura) throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoObterUrl);

            DateTimeOffset expiraEm = DateTimeOffset.UtcNow.AddMinutes(5);
            Dictionary<string, ArquivoArmazenado> arquivos = new Dictionary<string, ArquivoArmazenado>(StringComparer.Ordinal);
            foreach (string chave in chaves)
            {
                if (!arquivos.ContainsKey(chave))
                {
                    arquivos[chave] = new ArquivoArmazenado("https://storage.example/foto/" + Uri.EscapeDataString(chave), expiraEm);
                }
            }
            return Task.FromResult<IReadOnlyDictionary<string, ArquivoArmazenado>>(arquivos);
        }

        /// <summary>Token recebido na ultima chamada de RemoverAsync (a compensacao do envio deve receber CancellationToken.None).</summary>
        public CancellationToken UltimoTokenRemocao { get; private set; }

        public Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
        {
            DeleteCount++;
            RemoveKeys.Add(chave);
            UltimoTokenRemocao = cancellationToken;
            if (FalharRemocao) throw new IntegrationException(ConstantsIntegration.Armazenamento, ConstantsMessage.ErroArmazenamentoRemover);
            _objetos.Remove(chave);
            return Task.CompletedTask;
        }

        private static ArquivoArmazenado CriarUrl() => new ArquivoArmazenado("https://storage.example/foto", DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
