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

        /// <summary>400: a data inicial ou a final do periodo foi informada sem a outra.</summary>
        public const string ErroPeriodoFinanceiroIncompleto = "Informe a data inicial e a final do período, ou deixe as duas em branco.";

        /// <summary>400: a data inicial do periodo e maior que a final.</summary>
        public const string ErroPeriodoFinanceiroIntervalo = "A data inicial do período não pode ser maior que a final.";

        /// <summary>400: o periodo passa do limite de dias; {0} = ConstantsValues.DiasLimitePeriodoFinanceiro.</summary>
        public const string ErroPeriodoFinanceiroLimite = "O período informado passa do limite de {0} dias.";

        /// <summary>400: aba da lista de titulos fora de TODOS, EM_ATRASO, VENCE_HOJE e PAGOS_RECENTES.</summary>
        public const string ErroAbaTitulosFinanceirosInvalida = "Aba inválida. Use TODOS, EM_ATRASO, VENCE_HOJE ou PAGOS_RECENTES.";

        /// <summary>Log (so log): falha ao calcular os indicadores do painel financeiro.</summary>
        public const string ErroLogPainelFinanceiro = "Falha ao calcular os indicadores financeiros.";

        /// <summary>Log (so log): falha ao listar os titulos do painel financeiro.</summary>
        public const string ErroLogTitulosFinanceiros = "Falha ao listar os títulos financeiros.";

        /// <summary>Rotulo do tipo DIARIA na lista de titulos (aprovado pelo Kaio em 20/11/2026).</summary>
        public const string RotuloTipoDiaria = "Diária";

        /// <summary>Rotulo do tipo COBRANCA_EXTRA na lista de titulos (aprovado pelo Kaio em 20/11/2026).</summary>
        public const string RotuloTipoCobrancaExtra = "Cobrança extra";

        /// <summary>Rotulo do tipo VENDA na lista de titulos (aprovado pelo Kaio em 20/11/2026).</summary>
        public const string RotuloTipoVenda = "Venda";

        /// <summary>Texto da origem locacao na lista de titulos: {0} = id da locacao (aprovado pelo Kaio em 20/11/2026).</summary>
        public const string FormatoOrigemLocacaoTitulo = "Locação nº {0}";

        /// <summary>Texto da origem venda na lista de titulos: {0} = id da venda (aprovado pelo Kaio em 20/11/2026).</summary>
        public const string FormatoOrigemVendaTitulo = "Venda nº {0}";
    }
}
