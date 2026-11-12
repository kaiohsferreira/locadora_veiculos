namespace AutoVia.Communication.ViewObjects.Autenticacao
{
    /// <summary>
    /// VO de entrada do vinculo da conta Google com o cadastro da AutoVia (POST /api/auth/google/vinculo).
    /// Estrutura plana. O vinculo so nasce depois de provar a senha da conta AutoVia.
    /// </summary>
    public class GoogleVinculoVO
    {
        /// <summary>Credencial (ID token) do Google, ate 4096 caracteres. E validada de novo no vinculo.</summary>
        public string Credencial { get; set; } = string.Empty;

        /// <summary>CPF com ou sem mascara; apos remover os nao digitos, 11 digitos.</summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>Senha da conta AutoVia, em texto, enviada so nesta requisicao. Nunca e gravada nem registrada.</summary>
        public string Senha { get; set; } = string.Empty;

        /// <summary>Lembrar de mim: true emite sessao de Jwt:ExpiracaoLembrarDias dias; false (padrao) mantem Jwt:ExpiracaoMinutos. Sem regra de validacao.</summary>
        public bool Lembrar { get; set; }
    }
}
