using AutoVia.Helpers;
using System;

namespace AutoVia.Communication.Regras
{
    /// <summary>
    /// Regra pura do periodo do painel financeiro (P7-BE-05): a data inicial e a final vem juntas ou nenhuma; a inicial nao
    /// passa da final; e a diferenca entre as duas nao passa de ConstantsValues.DiasLimitePeriodoFinanceiro dias. Fica na
    /// Communication porque o validador FluentValidation a usa antes da action (o Core referencia a Communication, nao o
    /// contrario). Sem acesso a dados e sem relogio.
    /// </summary>
    public static class RegrasPeriodoFinanceiro
    {
        /// <summary>Devolve a mensagem do primeiro erro do periodo, ou nulo quando o periodo e valido (ou ausente).</summary>
        public static string? ErroDoPeriodo(DateOnly? de, DateOnly? ate)
        {
            if (de.HasValue != ate.HasValue)
            {
                return ConstantsMessage.ErroPeriodoFinanceiroIncompleto;
            }

            if (!de.HasValue || !ate.HasValue)
            {
                return null;
            }

            if (de.Value > ate.Value)
            {
                return ConstantsMessage.ErroPeriodoFinanceiroIntervalo;
            }

            int diferencaEmDias = ate.Value.DayNumber - de.Value.DayNumber;

            if (diferencaEmDias > ConstantsValues.DiasLimitePeriodoFinanceiro)
            {
                return string.Format(ConstantsMessage.ErroPeriodoFinanceiroLimite, ConstantsValues.DiasLimitePeriodoFinanceiro);
            }

            return null;
        }
    }
}
