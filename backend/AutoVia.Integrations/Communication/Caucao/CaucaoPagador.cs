namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>Dados do pagador enviados ao provedor. E-mail e CPF nunca vao para o log.</summary>
    public class CaucaoPagador
    {
        /// <summary>E-mail valido do pagador (ate 254 caracteres).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>CPF com 11 digitos (opcional). Nunca vai para o log.</summary>
        public string? Documento { get; set; }
    }
}
