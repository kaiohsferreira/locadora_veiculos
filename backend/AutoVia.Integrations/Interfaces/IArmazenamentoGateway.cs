using AutoVia.Integrations.Communication.Armazenamento;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>Operacoes privadas de storage usadas por avarias de vistoria.</summary>
    public interface IArmazenamentoGateway
    {
        Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default);
        Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default);
        Task RemoverAsync(string chave, CancellationToken cancellationToken = default);
    }
}
