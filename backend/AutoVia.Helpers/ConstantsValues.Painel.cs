namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da visao geral da locadora (multi-locadora, P11-BE-01 e P11-BE-02): tamanho dos periodos curtos e medios
    /// (em dias, incluindo hoje) e casas decimais dos percentuais. O periodo padrao (TRINTA_DIAS) e o membro do enum
    /// PeriodoVisaoGeral, definido no FiltroVisaoGeralVO, porque Helpers nao referencia o projeto Communication.
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Dias do periodo SETE_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoCurto = 7;

        /// <summary>Dias do periodo TRINTA_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoMedio = 30;

        /// <summary>Casas decimais de taxa de ocupacao e variacao de receita.</summary>
        public const int CasasDecimaisPercentualVisaoGeral = 1;
    }
}
