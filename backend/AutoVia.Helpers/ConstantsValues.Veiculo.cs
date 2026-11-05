namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos do modulo de veiculos. Arquivo parcial (base comum SEAM): as
    /// tarefas P1-BE-02 e P1-BE-03 acrescentam suas constantes aqui.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Veiculo (P1-BE-02) ---
        public const int TamanhoMaximoMarcaVeiculo = 40;
        public const int TamanhoMaximoModeloVeiculo = 40;
        public const int TamanhoMaximoCorVeiculo = 30;
        public const int AnoMinimoFabricacaoVeiculo = 1980;

        /// <summary>Quantos anos alem do ano atual o veiculo pode ter (ano de fabricacao e ano do modelo).</summary>
        public const int AnosAdiantamentoVeiculo = 1;

        /// <summary>Chassi apos normalizar para maiusculas: 17 caracteres, sem I, O e Q.</summary>
        public const string PadraoChassiVeiculo = "^[A-HJ-NPR-Z0-9]{17}$";

        /// <summary>Renavam: exatamente 11 digitos.</summary>
        public const string PadraoRenavamVeiculo = "^[0-9]{11}$";
    }
}
