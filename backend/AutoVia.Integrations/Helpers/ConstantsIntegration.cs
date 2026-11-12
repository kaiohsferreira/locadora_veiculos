namespace AutoVia.Integrations.Helpers
{
    /// <summary>
    /// Constantes da biblioteca de integracoes. Aqui ficam apenas codigos e
    /// parametros de comportamento. URL, timeout real e credenciais de cada
    /// provedor vem SEMPRE do banco (integration / integration_setting).
    /// </summary>
    public static class ConstantsIntegration
    {
        // --- Codigos das integracoes previstas (coluna integration.codigo) ---
        public const string Pagamento = "PAGAMENTO";
        public const string Email = "EMAIL";
        public const string Documento = "DOCUMENTO";
        public const string Armazenamento = "ARMAZENAMENTO";
        public const string Seguradora = "SEGURADORA";
        public const string Multas = "MULTAS";
        public const string Google = "GOOGLE";

        // --- Cache do IIntegrationResolver (curto, para nao consultar o banco a cada chamada) ---
        public const string PrefixoCache = "autovia:integration:";
        public const int CacheSegundos = 300;

        // --- Timeout padrao da coluna timeout_ms (DEFAULT 30000) ---
        public const int TimeoutPadraoMs = 30000;

        // --- Nova tentativa: so falha de rede e 5xx, no maximo 3 tentativas, espera progressiva ---
        public const int MaximoTentativas = 3;
        public const int EsperaBaseMs = 500;

        // --- Cabecalhos e mascara ---
        public const string CabecalhoIdempotencia = "Idempotency-Key";
        public const string Mascara = "***";
    }
}
