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

        /// <summary>Lado do carro: FRENTE, TRASEIRA, LATERAL_ESQUERDA ou LATERAL_DIREITA (maiusculas); vazio = sem angulo.</summary>
        public string? Angulo { get; set; }

        /// <summary>Posicao X no quadrante, em texto com ponto decimal (0 a 100); vazio = sem posicao. Tudo ou nada com angulo e posY.</summary>
        public string? PosX { get; set; }

        /// <summary>Posicao Y no quadrante, em texto com ponto decimal (0 a 100); vazio = sem posicao. Tudo ou nada com angulo e posX.</summary>
        public string? PosY { get; set; }

        public IFormFile? Foto { get; set; }
    }
}
