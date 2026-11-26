namespace AutoVia.Helpers
{
    public static partial class ConstantsMessage
    {
        public const string EmailReservaCriadaAssunto = "Confirmação de reserva";
        public const string EmailReservaCriadaEtiqueta = "reserva-criada";
        public const string EmailReservaCriadaCorpo = "Reserva confirmada.\nCódigo da reserva: {0}\nRetirada prevista: {1}\nDevolução prevista: {2}";
        public const string EmailReservaCriadaValorEstimado = "\nValor estimado: {0}";

        public const string EmailLocacaoFinalizadaAssunto = "Recibo de devolução";
        public const string EmailLocacaoFinalizadaEtiqueta = "locacao-finalizada";
        public const string EmailLocacaoFinalizadaCorpo = "Recibo de devolução.\nLocação: {0}\nDevolução efetiva: {1}\nValor final: {2}";

        public const string EmailCobrancaLancadaAssunto = "Aviso de cobrança";
        public const string EmailCobrancaLancadaEtiqueta = "cobranca-lancada";
        public const string EmailCobrancaLancadaCorpo = "Cobrança lançada.\nPagamento: {0}\n{1}\nValor: {2}\nVencimento: {3}";
        public const string EmailCobrancaLancadaOrigemLocacao = "Locação: {0}";
        public const string EmailCobrancaLancadaOrigemVenda = "Venda: {0}";

        public const string ErroEmailBuscarTitularReserva = "Não foi possível consultar o destinatário da confirmação da reserva.";
        public const string ErroEmailConfirmarReserva = "Não foi possível preparar a confirmação da reserva.";
        public const string ErroEmailNotificarReservaCriada = "Não foi possível notificar a criação da reserva.";
        public const string ErroEmailEmitirReciboDevolucao = "Não foi possível preparar o recibo de devolução.";
        public const string ErroEmailAvisarCobranca = "Não foi possível preparar o aviso de cobrança.";
        public const string ErroEmailCobrancaOrigemInvalida = "A cobrança deve ter uma única origem válida, locação ou venda.";
        public const string ErroEmailTransacionalEnfileirar = "Não foi possível enfileirar o e-mail transacional.";
    }
}
