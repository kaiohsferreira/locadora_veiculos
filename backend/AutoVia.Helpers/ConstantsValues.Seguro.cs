namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos do modulo de seguros (P9-BE-01: apolices). Os tamanhos seguem as
    /// colunas da tabela seguro.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Seguro (P9-BE-01) ---
        public const int TamanhoMaximoSeguradora = 100;
        public const int TamanhoMaximoNumeroApolice = 30;
        public const int TamanhoMaximoCoberturaSeguro = 200;

        /// <summary>Janela do destaque do RF-41: apolice que vence em ate estes dias (contando hoje) aparece destacada.</summary>
        public const int DiasAlertaVencimentoSeguro = 60;
    }
}
