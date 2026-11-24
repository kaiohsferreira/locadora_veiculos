using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras das cobrancas extras. Sem banco, sem log e sem relogio: o servico chama estas funcoes dentro da trava da
    /// locacao, e os testes as conferem direto.
    /// </summary>
    public static class RegrasCobrancasExtras
    {
        /// <summary>
        /// Previsao efetiva da devolucao, usada na conta do atraso e das diarias: o dia combinado (a data gravada) na mesma hora
        /// da retirada, com os segundos. A hora gravada na previsao e descartada. Mudanca B1 (21/11/2026, item 6 do plano).
        /// </summary>
        public static DateTime DevolucaoPrevistaEfetiva(DateTime dataRetirada, DateTime dataDevolucaoPrevista)
        {
            return dataDevolucaoPrevista.Date + dataRetirada.TimeOfDay;
        }
    }
}
