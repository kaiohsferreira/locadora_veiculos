namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores do encerramento da locacao (P5-BE-04, RF-25; devolucao-encerramento v4). Arquivo parcial de ConstantsValues.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Politica do encerramento: ATENDENTE ou ADMINISTRADOR da locadora do contexto (D6).</summary>
        public const string PoliticaEncerramentoLocacao = "EncerramentoLocacao";

        /// <summary>Teto do total confirmado do encerramento (9 digitos inteiros e 2 casas, NUMERIC(12,2)).</summary>
        public const decimal LimiteTotalEncerramento = 999999999.99m;
    }
}
