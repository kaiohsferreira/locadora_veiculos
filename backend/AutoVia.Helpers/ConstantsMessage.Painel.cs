namespace AutoVia.Helpers
{
    /// <summary>
    /// Mensagens da visao geral da locadora (multi-locadora, P11-BE-01 e P11-BE-02). Nenhuma mensagem ecoa valor de
    /// receita, nome de cliente ou dado de pagamento: o detalhe tecnico vai so para o log.
    /// </summary>
    public static partial class ConstantsMessage
    {
        /// <summary>500: falha ao montar a visao geral (detalhe so no log).</summary>
        public const string ErroConsultarVisaoGeral = "Não foi possível carregar a visão geral.";

        /// <summary>500: falha em uma das consultas de indicadores do repositorio (detalhe so no log).</summary>
        public const string ErroCalcularIndicadores = "Não foi possível calcular os indicadores da locadora.";
    }
}
