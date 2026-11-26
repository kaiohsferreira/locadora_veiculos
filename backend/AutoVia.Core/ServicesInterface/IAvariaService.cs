using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Avarias;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Casos de uso para avarias e armazenamento de suas fotos.</summary>
    public interface IAvariaService
    {
        /// <summary>Cria a avaria com angulo e posicao opcionais (tudo ou nada) e envia a foto.</summary>
        Task<ReturnAvariaVO> CriarAsync(int idVistoria, string descricao, string gravidade, decimal? valorEstimado, byte[]? foto, string? contentType, AnguloAvaria? angulo = null, decimal? posX = null, decimal? posY = null, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> GetByIdAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);

        /// <summary>Lista as avarias ativas da vistoria, em ordem de IdAvaria, cada uma com a foto quando o arquivo existe.</summary>
        Task<List<ReturnAvariaVO>> ListarAsync(int idVistoria, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> RetryUploadAsync(int idVistoria, int idAvaria, byte[] foto, string contentType, CancellationToken cancellationToken = default);
        Task<ReturnAvariaVO> GetFotoUrlAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
        Task RemoverFotoAsync(int idVistoria, int idAvaria, CancellationToken cancellationToken = default);
    }
}
