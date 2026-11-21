using Microsoft.AspNetCore.Http;

namespace AutoVia.Api.Requests.Avaria
{
    /// <summary>Dados multipart para registrar avaria e foto de vistoria.</summary>
    public class CriarAvariaRequest
    {
        public string Descricao { get; set; } = string.Empty;
        public string Gravidade { get; set; } = string.Empty;
        /// <summary>Valor estimado como texto do formulario (ponto decimal); a conversao e de ConversorValorEstimadoFormulario, sem depender da cultura.</summary>
        public string? ValorEstimado { get; set; }
        public IFormFile? Foto { get; set; }
    }
}
