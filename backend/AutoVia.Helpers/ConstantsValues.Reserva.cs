namespace AutoVia.Helpers
{
    /// <summary>Valores do modulo de reserva (P3-BE-02 e P3-BE-03): tetos, codigo de exibicao e situacoes da listagem.</summary>
    public static partial class ConstantsValues
    {
        /// <summary>Duracao maxima de uma reserva, em diarias (exatamente este valor e aceito).</summary>
        public const int DuracaoMaximaReservaDiarias = 90;

        /// <summary>Reservas ATIVA ou CONFIRMADA, nao desativadas, que um cliente pode ter na plataforma inteira.</summary>
        public const int LimiteReservasAtivasPorCliente = 5;

        /// <summary>Maior valor estimado aceito: limite da coluna valor_estimado, NUMERIC(10,2).</summary>
        public const decimal ValorMaximoEstimadoReserva = 99999999.99m;

        /// <summary>Prefixo do codigo de exibicao da reserva (ex.: AV-00042). So para exibicao; nao e gravado.</summary>
        public const string PrefixoCodigoReserva = "AV-";

        /// <summary>Digitos minimos do codigo de exibicao da reserva.</summary>
        public const int DigitosCodigoReserva = 5;

        /// <summary>Tamanho maximo, em caracteres, do termo de busca da listagem de reservas.</summary>
        public const int TamanhoMaximoBuscaReserva = 60;

        /// <summary>Situacao da listagem: reservas ATIVA e CONFIRMADA, em ordem crescente de retirada.</summary>
        public const string SituacaoReservaAtivas = "ATIVAS";

        /// <summary>Situacao da listagem: reservas CANCELADA e CONCLUIDA, em ordem decrescente de retirada.</summary>
        public const string SituacaoReservaHistorico = "HISTORICO";
    }
}
