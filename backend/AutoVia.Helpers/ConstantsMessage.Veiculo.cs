namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de veiculos. Arquivo parcial (base comum SEAM): as
    /// tarefas P1-BE-02 e P1-BE-03 acrescentam suas constantes aqui.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Veiculo (P1-BE-02) ---
        public const string ErroVeiculoNaoEncontrado = "O veículo informado não foi encontrado.";
        public const string ErroVeiculoPlacaDuplicada = "Já existe um veículo ativo com esta placa.";
        public const string ErroVeiculoChassiDuplicado = "Já existe um veículo ativo com este chassi.";
        public const string ErroVeiculoRenavamDuplicado = "Já existe um veículo ativo com este renavam.";
        public const string ErroBuscarVeiculo = "Não foi possível consultar o veículo.";
        public const string ErroSalvarVeiculo = "Não foi possível salvar o veículo.";
        public const string ErroVeiculoCategoriaObrigatoria = "Informe a categoria do veículo.";
        public const string ErroVeiculoPlacaInvalida = "Informe uma placa válida, no padrão Mercosul (ABC1D23) ou antigo (ABC1234).";
        public const string ErroVeiculoChassiInvalido = "O chassi deve ter 17 caracteres alfanuméricos, sem as letras I, O e Q.";
        public const string ErroVeiculoRenavamInvalido = "O renavam deve ter 11 dígitos numéricos.";
        public const string ErroVeiculoMarcaObrigatoria = "Informe a marca do veículo.";
        public const string ErroVeiculoMarcaTamanho = "A marca deve ter no máximo {0} caracteres.";
        public const string ErroVeiculoModeloObrigatorio = "Informe o modelo do veículo.";
        public const string ErroVeiculoModeloTamanho = "O modelo deve ter no máximo {0} caracteres.";
        public const string ErroVeiculoCorTamanho = "A cor deve ter no máximo {0} caracteres.";
        public const string ErroVeiculoAnoFabricacaoIntervalo = "O ano de fabricação deve estar entre {0} e {1}.";
        public const string ErroVeiculoAnoModeloAnterior = "O ano do modelo não pode ser anterior ao ano de fabricação.";
        public const string ErroVeiculoAnoModeloMaximo = "O ano do modelo não pode ser posterior a {0}.";
        public const string ErroVeiculoKmNegativo = "A quilometragem não pode ser negativa.";
        public const string ErroVeiculoValorDiaria = "O valor da diária deve ser maior que zero.";
        public const string ErroVeiculoValorDiariaPrecisao = "O valor da diária deve ter no máximo 8 dígitos inteiros e 2 casas decimais.";
        public const string ErroVeiculoValorVenda = "O valor de venda deve ser maior que zero.";
        public const string ErroVeiculoValorVendaPrecisao = "O valor de venda deve ter no máximo 10 dígitos inteiros e 2 casas decimais.";
        public const string ErroVeiculoDataAquisicaoFutura = "A data de aquisição não pode ser posterior a hoje.";
    }
}
