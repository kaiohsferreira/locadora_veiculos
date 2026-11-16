namespace AutoVia.Helpers
{
    /// <summary>Mensagens do fluxo de locacoes.</summary>
    public static partial class ConstantsMessage
    {
        public const string ErroLocacaoReservaObrigatoria = "Informe uma reserva válida.";
        public const string ErroLocacaoNaoEncontrada = "A locação informada não foi encontrada.";
        public const string ErroLocacaoReservaEstadoInvalido = "A reserva não está em um estado que permita iniciar a locação.";
        public const string ErroLocacaoReservaDuplicada = "Já existe uma locação ativa para esta reserva.";
        public const string ErroLocacaoVeiculoIndisponivel = "O veículo associado à reserva não está reservado para locação.";
        public const string ErroLocacaoFuncionarioNaoEncontrado = "O funcionário autenticado não está ativo.";
        public const string ErroBuscarLocacao = "Não foi possível consultar a locação.";
        public const string ErroSalvarLocacao = "Não foi possível iniciar a locação.";
    }
}
