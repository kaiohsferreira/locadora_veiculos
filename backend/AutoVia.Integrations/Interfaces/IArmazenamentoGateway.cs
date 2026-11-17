using AutoVia.Integrations.Communication.Armazenamento;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Operacoes privadas de storage usadas por avarias de vistoria.</summary>
    public interface IArmazenamentoGateway
    {
        Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default);
        Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default);
        Task RemoverAsync(string chave, CancellationToken cancellationToken = default);

        /// <summary>Assina em lote as URLs de leitura das chaves, com uma integracao resolvida e sem conferir a existencia de cada objeto.</summary>
        Task<IReadOnlyDictionary<string, ArquivoArmazenado>> AssinarUrlsAsync(IReadOnlyCollection<string> chaves, CancellationToken cancellationToken = default);
    }
}
