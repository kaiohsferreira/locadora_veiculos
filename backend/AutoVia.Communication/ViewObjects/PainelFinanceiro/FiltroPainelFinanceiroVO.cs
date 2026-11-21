using System;

namespace AutoVia.Communication.ViewObjects.PainelFinanceiro
{
    /// <summary>
    /// Filtro dos indicadores do painel financeiro (GET /api/painel/financeiro, query string): periodo de/ate em AAAA-MM-DD.
    /// Os dois ausentes valem o mes corrente; so um deles e 400. Validado por FiltroPainelFinanceiroVOValidator.
    /// </summary>
    public class FiltroPainelFinanceiroVO
    {
        /// <summary>Data inicial do periodo (inclusive), AAAA-MM-DD. Opcional.</summary>
        public DateOnly? De { get; set; }

        /// <summary>Data final do periodo (inclusive), AAAA-MM-DD. Opcional.</summary>
        public DateOnly? Ate { get; set; }
    }
}
