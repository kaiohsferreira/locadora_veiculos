namespace AutoVia.Integrations.Helpers.Google
{
    /// <summary>
    /// Constantes do login com Google (BE-5): chaves da integracao GOOGLE, nomes dos provedores,
    /// regras da prova (emissor, algoritmo, tolerancia de relogio, limites), cache das chaves publicas
    /// e regras do provedor FAKE. Mensagens ficam em ConstantsMessage (arquivo ConstantsMessage.Google.cs).
    /// </summary>
    public static class ConstantsGoogle
    {
        // --- Chaves de integration_setting (GOOGLE) ---
        public const string ChaveProvedor = "provedor";
        public const string ChaveClientId = "client_id";

        // --- Nomes dos provedores (valor da chave 'provedor' e Nome do IGoogleIdentityProvider) ---
        public const string ProvedorGoogle = "GOOGLE";
        public const string ProvedorFake = "FAKE";

        // --- Regras da prova (JWT do Google) ---
        public const string AlgoritmoAssinatura = "RS256";
        public const string CaminhoCertificados = "/oauth2/v3/certs";
        public static readonly string[] EmissoresValidos = { "https://accounts.google.com", "accounts.google.com" };
        public const int ToleranciaRelogioSegundos = 60;

        // --- Limites ---
        public const int TamanhoMaximoCredencial = 4096;
        public const int TamanhoMaximoSub = 255;
        public const int TamanhoMaximoEmail = 254;
        public const int TamanhoMaximoNome = 120;

        // --- Claims lidas da prova ---
        public const string ClaimSub = "sub";
        public const string ClaimEmail = "email";
        public const string ClaimEmailVerificado = "email_verified";
        public const string ClaimNome = "name";
        public const string ValorVerdadeiro = "true";

        // --- Cache das chaves publicas (IMemoryCache) ---
        public const int JwksCacheSegundos = 3600;
        public const int JwksIntervaloRefreshSegundos = 300;
        /// <summary>Cache negativo: depois de uma busca que falhou, nova busca so passados estes segundos (sem rede no intervalo).</summary>
        public const int JwksIntervaloFalhaSegundos = 60;
        /// <summary>Idade maxima da reserva (ultimo JWKS bom), contada desde a ultima busca bem-sucedida. Depois disso a reserva nao e mais usada.</summary>
        public const int JwksReservaMaximaHoras = 24;
        /// <summary>Unico host aceito para as chaves publicas (base_url da integracao GOOGLE, em https). Qualquer outro e recusado antes da chamada.</summary>
        public const string HostCertificados = "www.googleapis.com";
        /// <summary>Unico caminho aceito na base_url das chaves publicas (raiz, ou vazio, que o Uri normaliza para a raiz).</summary>
        public const string CaminhoRaizBaseUrl = "/";
        public const string ChaveCacheJwks = "autovia:google:jwks";
        public const string ChaveCacheEstadoJwks = "autovia:google:jwks:estado";

        // --- Provedor FAKE (so Development + SANDBOX) ---
        public const string PrefixoCredencialFake = "FAKE:";
        public const string SufixoNaoVerificadoFake = ":naoverificado";
        public const string PrefixoSubFake = "fake-";
        public const int TamanhoHashSubFake = 16;
        public const char SeparadorEmail = '@';

        // --- Motivos genericos para o log de recusa (nunca o conteudo da credencial) ---
        public const string MotivoCredencialAusente = "credencial-ausente";
        public const string MotivoCredencialMalformada = "credencial-malformada";
        public const string MotivoTamanho = "tamanho-excedido";
        public const string MotivoClaims = "claims-invalidas";
        public const string MotivoNaoVerificado = "email-nao-verificado";
        public const string MotivoValidacao = "validacao-recusada";
    }
}
