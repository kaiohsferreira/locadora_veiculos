namespace AutoVia.Integrations.Communication.Google
{
    /// <summary>
    /// Identidade provada pelo Google. So existe quando a prova e valida: EmailVerificado e sempre true
    /// no retorno (e-mail nao verificado e recusado antes). Nunca registrar Sub, Email ou Nome em log.
    /// </summary>
    public class GoogleIdentidade
    {
        /// <summary>Identificador estavel da conta Google (claim sub, 1 a 255 caracteres).</summary>
        public string Sub { get; set; } = string.Empty;

        /// <summary>E-mail verificado pelo Google, em minusculas (ate 254 caracteres).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Indica que o Google verificou o e-mail.</summary>
        public bool EmailVerificado { get; set; }

        /// <summary>Nome exibido pelo Google (opcional, ate 120 caracteres).</summary>
        public string? Nome { get; set; }
    }
}
