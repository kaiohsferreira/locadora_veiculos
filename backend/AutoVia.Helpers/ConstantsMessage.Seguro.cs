namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do modulo de seguros (P9-BE-01: apolices). Arquivo parcial da
    /// ConstantsMessage; o uso continua ConstantsMessage.ErroX.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Seguro: regras do servico ---
        public const string ErroSeguroNaoEncontrado = "A apólice informada não foi encontrada.";
        public const string ErroSeguroNumeroApoliceDuplicado = "Já existe uma apólice ativa com este número.";

        // --- Seguro: validacao de entrada ---
        public const string ErroSeguroVeiculoObrigatorio = "Informe o veículo da apólice.";
        public const string ErroSeguroSeguradoraObrigatoria = "Informe a seguradora.";
        public const string ErroSeguroSeguradoraTamanho = "A seguradora deve ter no máximo {0} caracteres.";
        public const string ErroSeguroNumeroApoliceObrigatorio = "Informe o número da apólice.";
        public const string ErroSeguroNumeroApoliceTamanho = "O número da apólice deve ter no máximo {0} caracteres.";
        public const string ErroSeguroDataInicioObrigatoria = "Informe a data de início da vigência.";
        public const string ErroSeguroDataFimObrigatoria = "Informe a data de fim da vigência.";
        public const string ErroSeguroVigenciaInvalida = "A data de fim da vigência deve ser posterior à data de início.";
        public const string ErroSeguroValorPremio = "O valor do prêmio deve ser maior que zero.";
        public const string ErroSeguroValorPremioPrecisao = "O valor do prêmio deve ter no máximo 8 dígitos inteiros e 2 casas decimais.";
        public const string ErroSeguroCoberturaTamanho = "A cobertura deve ter no máximo {0} caracteres.";
        public const string ErroSeguroFiltroVeiculoInvalido = "O filtro de veículo deve ser um número maior que zero.";

        // --- Seguro: falhas inesperadas (log) ---
        public const string ErroListarSeguros = "Não foi possível listar as apólices.";
        public const string ErroBuscarSeguro = "Não foi possível consultar a apólice.";
        public const string ErroSalvarSeguro = "Não foi possível salvar a apólice.";
    }
}
