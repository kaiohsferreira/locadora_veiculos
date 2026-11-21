namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da proposta de compra de seminovo (P6-BE-02, RF-27). Arquivo parcial da ConstantsMessage;
    /// o uso continua ConstantsMessage.ErroX.
    /// </summary>
    public static partial class ConstantsMessage
    {
        // --- Proposta de compra (P6-BE-02) ---
        public const string ErroPropostaVeiculoInvalido = "Informe um veículo válido.";
        public const string ErroPropostaEntradaNegativa = "A entrada não pode ser negativa.";
        public const string ErroPropostaEntradaPrecisao = "A entrada deve ter no máximo 2 casas decimais e até 10 dígitos inteiros.";
        public const string ErroPropostaParcelasForaDoLimite = "O número de parcelas deve ficar entre {0} e {1}.";
        public const string ErroPropostaEntradaMaiorQueValor = "A entrada deve ser menor que o valor de venda do veículo.";
        public const string ErroPropostaParcelaMinima = "Com esse valor financiado, cada parcela ficaria abaixo de R$ 0,01. Escolha menos parcelas ou uma entrada menor.";
        public const string ErroPropostaVeiculoNaoDisponivel = "Veículo não encontrado ou não está à venda.";
        public const string ErroPropostaNaoEncontrada = "Proposta de compra não encontrada.";
        public const string ErroSimularParcelamento = "Não foi possível simular o parcelamento.";
        public const string ErroRegistrarProposta = "Não foi possível registrar a proposta de compra.";
        public const string ErroBuscarProposta = "Não foi possível consultar a proposta de compra.";
        public const string ErroPropostaBuscarVeiculo = "Não foi possível consultar o veículo à venda.";
    }
}
