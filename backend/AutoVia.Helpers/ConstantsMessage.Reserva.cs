namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string ErroReservaVeiculoInvalido = "Informe um veículo válido.";
        public const string ErroReservaPeriodoInvalido = "A data de devolução deve ser posterior à data de retirada.";
        public const string ErroReservaClienteNaoEncontrado = "O cliente informado não foi encontrado.";
        public const string ErroReservaClienteNaoAprovado = "O cadastro do cliente precisa estar aprovado para reservar.";
        public const string ErroReservaVeiculoNaoEncontrado = "O veículo informado não foi encontrado ou está desativado.";
        public const string ErroReservaVeiculoIndisponivel = "O veículo não está disponível para reserva.";
        public const string ErroReservaConflitoPeriodo = "O veículo já possui uma reserva para o período solicitado.";
        public const string ErroCriarReserva = "Não foi possível criar a reserva.";
    }
}
