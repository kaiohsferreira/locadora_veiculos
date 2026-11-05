namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da listagem de veiculos (P1-BE-03). Arquivo parcial: as mensagens do
    /// cadastro ficam em ConstantsMessage.Veiculo.cs.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Veiculo: listagem (P1-BE-03) ---
        public const string ErroListarVeiculos = "Não foi possível listar os veículos.";
        public const string ErroVeiculoStatusInvalido = "O status informado não é válido. Valores aceitos: {0}.";
        public const string ErroVeiculoFiltroCategoriaInvalida = "O filtro de categoria deve ser um número maior que zero.";
        public const string ErroVeiculoFiltroPlacaTamanho = "A busca por placa deve ter no máximo {0} caracteres.";
    }
}
