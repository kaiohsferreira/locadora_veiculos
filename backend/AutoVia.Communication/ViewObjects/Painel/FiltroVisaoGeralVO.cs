using AutoVia.Communication.Enums;

namespace AutoVia.Communication.ViewObjects.Painel
{
    /// <summary>
    /// Filtro da visao geral (GET /api/painel/visao-geral, query string): so o periodo. Quando o parametro e omitido, vale
    /// TRINTA_DIAS. Validado por FiltroVisaoGeralVOValidator (valor fora do enum vira 400).
    /// </summary>
    public class FiltroVisaoGeralVO
    {
        /// <summary>Periodo dos indicadores: HOJE, SETE_DIAS, TRINTA_DIAS (padrao) ou MES_ATUAL.</summary>
        public PeriodoVisaoGeral Periodo { get; set; } = PeriodoVisaoGeral.TRINTA_DIAS;
    }
}
