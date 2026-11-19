namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos da listagem de veiculos (P1-BE-03). Arquivo parcial: os valores do
    /// cadastro ficam em ConstantsValues.Veiculo.cs.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Veiculo: listagem (P1-BE-03) ---
        public const int TamanhoMaximoBuscaPlacaVeiculo = 8;
        /// <summary>Tamanho maximo da busca da listagem: o mesmo do modelo, que e o maior campo buscado.</summary>
        public const int TamanhoMaximoBuscaVeiculo = TamanhoMaximoModeloVeiculo;
    }
}
