using Microsoft.AspNetCore.Http;

namespace AutoVia.Api.Requests.Avaria
{
    /// <summary>Dados multipart para registrar avaria e foto de vistoria.</summary>
    public class CriarAvariaRequest
    {
        public string Descricao { get; set; } = string.Empty;
        public string Gravidade { get; set; } = string.Empty;
        public decimal? ValorEstimado { get; set; }
        public IFormFile? Foto { get; set; }
    }
}
