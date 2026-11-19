namespace AutoVia.Helpers
{
    /// <summary>
    /// Valores da visao geral da locadora (multi-locadora, P11-BE-01 e P11-BE-02): tamanho dos periodos curtos e medios
    /// (em dias, incluindo hoje) e casas decimais dos percentuais. O periodo padrao (TRINTA_DIAS) e o membro do enum
    /// PeriodoVisaoGeral, definido no FiltroVisaoGeralVO, porque Helpers nao referencia o projeto Communication.
    /// PrimeiroDiaDoMes e MesesDaJanelaMensal definem a janela de veiculos novos no mes (ObterVisaoGeralAsync) e o dia
    /// inicial do periodo MES_ATUAL (CalcularJanela).
    /// </summary>
    public static partial class ConstantsValues
    {
        /// <summary>Dias do periodo SETE_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoCurto = 7;

        /// <summary>Dias do periodo TRINTA_DIAS, incluindo hoje.</summary>
        public const int DiasPeriodoMedio = 30;

        /// <summary>Casas decimais de taxa de ocupacao e variacao de receita.</summary>
        public const int CasasDecimaisPercentualVisaoGeral = 1;

        /// <summary>Dia do mes em que comeca a janela de veiculos novos no mes (primeiro dia, a partir de 00:00).</summary>
        public const int PrimeiroDiaDoMes = 1;

        /// <summary>Meses de duracao da janela de veiculos novos no mes (um mes, ate o dia 1 do mes seguinte, excluido).</summary>
        public const int MesesDaJanelaMensal = 1;
    }
}
