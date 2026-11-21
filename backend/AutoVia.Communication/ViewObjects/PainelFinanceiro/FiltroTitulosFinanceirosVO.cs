using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Helpers;
using System;

namespace AutoVia.Communication.ViewObjects.PainelFinanceiro
{
    /// <summary>
    /// Filtro da lista de titulos do painel financeiro (GET /api/painel/financeiro/titulos, query string): aba (TODOS por padrao,
    /// EM_ATRASO, VENCE_HOJE ou PAGOS_RECENTES), periodo de/ate (mesma regra dos indicadores) e paginacao padrao do projeto.
    /// Validado por FiltroTitulosFinanceirosVOValidator.
    /// </summary>
    public class FiltroTitulosFinanceirosVO : FiltroPaginacaoVO
    {
        /// <summary>Aba da lista: TODOS (padrao), EM_ATRASO, VENCE_HOJE ou PAGOS_RECENTES.</summary>
        public string Aba { get; set; } = ConstantsValues.AbaTitulosTodos;

        /// <summary>Data inicial do periodo (inclusive). Opcional; PAGOS_RECENTES ignora o periodo.</summary>
        public DateOnly? De { get; set; }

        /// <summary>Data final do periodo (inclusive). Opcional; PAGOS_RECENTES ignora o periodo.</summary>
        public DateOnly? Ate { get; set; }
    }
}
