namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens do catalogo de seminovos (P6-BE-01). Arquivo parcial da ConstantsMessage;
    /// o uso continua ConstantsMessage.ErroX.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Catalogo de seminovos (P6-BE-01) ---
        public const string ErroListarCatalogo = "Não foi possível listar o catálogo de veículos à venda.";
        public const string ErroCatalogoMarcaTamanho = "A busca por marca deve ter no máximo {0} caracteres.";
        public const string ErroCatalogoPrecoNegativo = "O preço informado no filtro não pode ser negativo.";
        public const string ErroCatalogoFaixaPreco = "O preço mínimo não pode ser maior que o preço máximo.";
        public const string ErroCatalogoAnoInvalido = "O ano informado no filtro deve ser maior que zero.";
        public const string ErroCatalogoFaixaAno = "O ano mínimo não pode ser maior que o ano máximo.";
        public const string ErroCatalogoKmNegativo = "A quilometragem máxima não pode ser negativa.";
    }
}
