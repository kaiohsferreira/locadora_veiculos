using System;

namespace AutoVia.Core.Regras
{
    /// <summary>Periodo do resumo da manutencao: primeiro e ultimo dia (inclusive).</summary>
    public sealed record PeriodoResumoManutencao(DateOnly Inicio, DateOnly Fim);

    /// <summary>
    /// Regra pura do resumo da manutencao (semana 4). O periodo do custo e sempre o mes corrente do relogio do projeto: primeiro
    /// e ultimo dia do mes de "hoje". Sem banco e sem relogio proprio: quem chama passa o instante (IRelogio.Agora). Testada
    /// direto em RegrasResumoManutencaoTests.
    /// </summary>
    public static class RegrasResumoManutencao
    {
        /// <summary>Devolve o primeiro e o ultimo dia do mes de "hoje" (inclusive em fevereiro bissexto e em dezembro).</summary>
        public static PeriodoResumoManutencao PeriodoPadrao(DateTime hoje)
        {
            DateOnly dia = DateOnly.FromDateTime(hoje);
            DateOnly inicio = new DateOnly(dia.Year, dia.Month, 1);
            DateOnly fim = new DateOnly(dia.Year, dia.Month, DateTime.DaysInMonth(dia.Year, dia.Month));

            return new PeriodoResumoManutencao(inicio, fim);
        }
    }
}
