namespace AutoVia.Integrations.Helpers.Seguradora
{
    /// <summary>
    /// Constantes da integracao SEGURADORA (P9-INT-01): chaves de integration_setting,
    /// nome do provedor FAKE, limites de validacao e dados do provedor de teste.
    /// Mensagens ficam em ConstantsMessage (AutoVia.Helpers, arquivo ConstantsMessage.Seguradora.cs).
    /// </summary>
    public static class ConstantsSeguradora
    {
        // --- Chaves de integration_setting (SEGURADORA) ---
        public const string ChaveProvedor = "provedor";

        // --- Nomes dos provedores (valor da chave 'provedor') ---
        public const string ProvedorFake = "FAKE";

        // --- Limites de validacao ---
        public const int ReferenciaTamanhoMaximo = 64;
        public const int NumeroApoliceTamanhoMaximo = 30;
        public const int PlacaTamanhoMaximo = 8;
        public const int DescricaoTamanhoMaximo = 1000;
        public const int ProtocoloTamanhoMaximo = 40;

        /// <summary>Referencia externa: letras, numeros, hifen e sublinhado (\z nao aceita quebra de linha no fim).</summary>
        public const string PadraoReferenciaExterna = @"^[A-Za-z0-9_-]+\z";

        // --- Provedor FAKE (somente SANDBOX) ---
        public const string PrefixoProtocoloFake = "FAKE-SIN-";
        public const int TamanhoHashProtocoloFake = 12;

        /// <summary>Se o numero da apolice contiver este texto, o FAKE responde que ela nao esta vigente.</summary>
        public const string FakeMarcadorApoliceVencida = "VENCIDA";
    }
}
