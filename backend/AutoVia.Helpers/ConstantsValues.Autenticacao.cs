namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos do modulo de autenticacao: chaves de configuracao, nomes de claims,
    /// politica do limite publico e parametros do hash de senha (PBKDF2-SHA256).
    /// Senhas, chaves JWT e hashes NUNCA ficam aqui.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Chaves de configuracao ---
        public const string SecaoJwt = "Jwt";
        public const string SecaoAdminInicial = "Autenticacao:AdminInicial";
        public const string ChaveLimitePublico = "Autenticacao:LimitePublicoPorMinuto";

        // --- Token ---
        public const string TipoTokenBearer = "Bearer";
        public const string ClaimNome = "nome";
        public const string ClaimCargo = "cargo";
        public const string ClaimRole = "role";
        public const int TamanhoMinimoJwtChave = 32;
        public const int ToleranciaRelogioSegundos = 30;

        // --- Login e cadastro ---
        public const int TamanhoMaximoSenhaLogin = 128;
        public const int TamanhoCpf = 11;
        public const int TamanhoMinimoSenhaAdminInicial = 12;

        // --- Limite de requisicoes das rotas publicas ---
        public const string PoliticaLimitePublico = "AutoViaPublico";
        public const int LimitePublicoPorMinutoPadrao = 10;
        public const string ChaveIpDesconhecido = "desconhecido";

        // --- Hash de senha (formato v1.PBKDF2-SHA256.{iteracoes}.{salt}.{hash}) ---
        public const int HashIteracoesPbkdf2 = 600000;
        public const int HashTamanhoSalt = 16;
        public const int HashTamanhoChave = 32;
        public const string HashVersao = "v1";
        public const string HashAlgoritmo = "PBKDF2-SHA256";
    }
}
