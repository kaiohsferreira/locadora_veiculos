namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores fixos do modulo de clientes (P2-BE-02): limites de tamanho dos campos,
    /// idade minima e faixas de digitos dos documentos e telefones.
    /// </summary>
    public static partial class ConstantsValues
    {
        // --- Cliente (P2-BE-02) ---
        public const int TamanhoMaximoNomeCliente = 120;
        public const int TamanhoMaximoEmailCliente = 120;
        public const int TamanhoMaximoLogradouroCliente = 120;
        public const int TamanhoMaximoNumeroEnderecoCliente = 10;
        public const int TamanhoMaximoComplementoCliente = 60;
        public const int TamanhoMaximoBairroCliente = 60;
        public const int TamanhoMaximoCidadeCliente = 60;
        public const int TamanhoUfCliente = 2;
        public const int TamanhoCepCliente = 8;
        public const int TamanhoMinimoTelefoneCliente = 10;
        public const int TamanhoMaximoTelefoneCliente = 11;
        public const int TamanhoCnhNumero = 11;
        public const int IdadeMinimaCliente = 18;
        public const int TamanhoMinimoSenhaCadastro = 8;
        public const int TamanhoMaximoSenhaCadastro = 64;
    }
}
