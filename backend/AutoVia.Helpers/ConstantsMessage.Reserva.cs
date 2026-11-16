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
        public const string ErroConsultarReserva = "Não foi possível consultar a reserva.";
        public const string ErroListarReservas = "Não foi possível listar as reservas.";
        public const string ErroCancelarReserva = "Não foi possível cancelar a reserva.";
        public const string ErroReservaNaoEncontrada = "A reserva não foi encontrada.";
        public const string ErroReservaConcluidaNaoCancelavel = "Uma reserva concluída não pode ser cancelada.";
        public const string ErroReservaStatusInvalido = "Status inválido. Valores aceitos: {0}.";
    }
}
