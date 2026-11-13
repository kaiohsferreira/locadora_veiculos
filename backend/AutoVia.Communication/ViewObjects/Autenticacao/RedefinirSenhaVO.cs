namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de entrada da redefinicao de senha (POST /api/auth/redefinicao-senha). Estrutura plana.
    /// Token e senhas vem so nesta requisicao: nunca sao gravados em claro, registrados ou devolvidos.
    /// </summary>
    public class RedefinirSenhaVO
    {
        /// <summary>Token do link (base64url), lido do fragmento #token= pelo front.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Nova senha: 8 a 64 caracteres, com ao menos uma letra e um numero.</summary>
        public string NovaSenha { get; set; } = string.Empty;

        /// <summary>Confirmacao: precisa ser igual a NovaSenha.</summary>
        public string ConfirmacaoSenha { get; set; } = string.Empty;
    }
}
