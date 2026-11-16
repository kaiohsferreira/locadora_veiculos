namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string ErroReservaVeiculoInvalido = "Informe um veículo válido.";
        public const string ErroReservaPeriodoInvalido = "A data de devolução deve ser posterior à data de retirada.";
        public const string ErroReservaVeiculoNaoEncontrado = "O veículo informado não foi encontrado ou está desativado.";
        public const string ErroReservaVeiculoIndisponivel = "O veículo não está disponível para reserva.";
        public const string ErroReservaConflitoPeriodo = "O veículo já possui uma reserva para o período solicitado.";
        public const string ErroCriarReserva = "Não foi possível criar a reserva.";
        public const string ErroConsultarReserva = "Não foi possível consultar a reserva.";
        public const string ErroListarReservas = "Não foi possível listar as reservas.";
        public const string ErroCancelarReserva = "Não foi possível cancelar a reserva.";
        public const string ErroBuscarVeiculoReserva = "Não foi possível consultar o veículo da reserva.";
        public const string ErroReservaNaoEncontrada = "A reserva não foi encontrada.";
        public const string ErroReservaConcluidaNaoCancelavel = "Uma reserva concluída não pode ser cancelada.";
        public const string ErroReservaStatusInvalido = "Status inválido. Valores aceitos: {0}.";
        public const string ErroReservaRetiradaNoPassado = "A data de retirada não pode ser anterior a hoje.";
        public const string ErroReservaDuracaoMaxima = "A reserva pode ter no máximo {0} diárias.";
        public const string ErroReservaLimiteAtivas = "Você atingiu o limite de {0} reservas ativas. Cancele uma reserva para fazer outra.";
        public const string ErroReservaValorEstimadoExcedido = "O valor estimado da reserva excede o limite permitido.";
        public const string ErroReservaSituacaoInvalida = "Situação inválida. Valores aceitos: {0}.";
        public const string ErroReservaSituacaoComStatus = "Informe apenas um dos filtros: situação ou status.";
        public const string ErroReservaBuscaTamanho = "A busca deve ter no máximo {0} caracteres.";
        public const string ErroReservaBuscaInvalida = "A busca contém caracteres inválidos.";
    }
}
