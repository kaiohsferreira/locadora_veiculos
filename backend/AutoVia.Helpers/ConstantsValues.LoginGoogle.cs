namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do login de cliente com conta Google (N3, RF-51, BE-6): rotas do controller, limite da
    /// entrada e contagens usadas na regra de unicidade do vinculo. Mensagens ficam em ConstantsMessage.Google.cs.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Rotas (prefixo api/auth/google) ---
        public const string RotaGoogle = "api/auth/google";
        public const string RotaGoogleConfiguracao = "configuracao";
        public const string RotaGoogleLogin = "login";
        public const string RotaGoogleVinculo = "vinculo";

        // --- Entrada: credencial (ID token) do Google, ate 4096 caracteres ---
        public const int TamanhoMaximoCredencialGoogle = 4096;

        // --- Contagem de cadastros com o mesmo e-mail (busca limitada a 2 para detectar ambiguidade) ---
        public const int QuantidadeClientesPorEmailGoogle = 2;
        public const int NenhumClienteGoogle = 0;
        public const int ClienteUnicoGoogle = 1;
    }
}
