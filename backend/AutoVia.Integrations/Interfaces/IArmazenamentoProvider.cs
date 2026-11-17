using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Implementacao de protocolo para um provedor de storage.</summary>
    public interface IArmazenamentoProvider
    {
        string Nome { get; }
        Task<ArquivoArmazenado> EnviarAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, byte[] conteudo, string contentType, int expiracaoSegundos, CancellationToken cancellationToken = default);
        Task<ArquivoArmazenado?> ObterUrlAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, int expiracaoSegundos, CancellationToken cancellationToken = default);
        Task RemoverAsync(ConfiguracaoIntegracao configuracao, string bucket, string chave, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(ConfiguracaoIntegracao configuracao, string bucket, IReadOnlyCollection<string> chaves, int expiracaoSegundos, CancellationToken cancellationToken = default);
    }
}
