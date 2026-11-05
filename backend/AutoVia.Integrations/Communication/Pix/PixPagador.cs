namespace AutoVia.Integrations.Communication.Pix
{
    /// <summary>Dados do pagador enviados ao provedor. Nome e e-mail nunca vao para o log.</summary>
    public class PixPagador
    {
        /// <summary>Nome completo do pagador (ate 120 caracteres).</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>E-mail valido do pagador.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>CPF com 11 digitos (opcional). Nunca vai para o log.</summary>
        public string? Documento { get; set; }
    }
}
