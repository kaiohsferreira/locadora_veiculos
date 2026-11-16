using AutoVia.Communication.ViewObjects.Avarias;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Casos de uso para avarias e armazenamento de suas fotos.</summary>
    public interface IAvariaService
    {
        Task<ReturnAvariaVO> CriarAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, byte[]? foto, string? contentType, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> GetByIdAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> RetryUploadAsync(int idVistoria, int idAvaria, byte[] foto, string contentType, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> GetFotoUrlAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
        Task RemoverFotoAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
    }
}
