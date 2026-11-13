namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de entrada do login com Google (POST /api/auth/google/login). Estrutura plana.
    /// A credencial e a prova assinada pelo Google: validada no servidor e nunca registrada em log.
    /// </summary>
    public class GoogleLoginVO
    {
        /// <summary>Credencial (ID token) devolvida pelo botao do Google, ate 4096 caracteres.</summary>
        public string Credencial { get; set; } = string.Empty;

        /// <summary>Lembrar de mim: true emite sessao de Jwt:ExpiracaoLembrarDias dias; false (padrao) mantem Jwt:ExpiracaoMinutos. Sem regra de validacao.</summary>
        public bool Lembrar { get; set; }
    }
}
