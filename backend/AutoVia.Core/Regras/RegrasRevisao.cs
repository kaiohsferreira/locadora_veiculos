using AutoVia.Helpers;
using System;

namespace AutoVia.Core.Regras
{
    /// <summary>
    /// Regras puras dos alertas de revisao (P8-BE-03, RF-39). Sem banco e sem relogio: a base vem do repositorio (maior km
    /// registrado nas preventivas do veiculo) e a decisao sai daqui. Intervalo, margem e situacoes vem de ConstantsValues.
    /// Testada direto em RegrasRevisaoTests.
    /// </summary>
    public static class RegrasRevisao
    {
        /// <summary>Base da revisao: o maior km das preventivas; sem preventiva, zero.</summary>
        public static int KmBase(int? maiorKmPreventiva)
        {
            return maiorKmPreventiva ?? 0;
        }

        /// <summary>Km em que a proxima revisao vence: a base mais o intervalo fixo.</summary>
        public static int KmProximaRevisao(int kmBase)
        {
            return kmBase + ConstantsValues.IntervaloRevisaoKm;
        }

        /// <summary>Quanto falta, em km, para a proxima revisao (negativo quando ja passou).</summary>
        public static int FaltamKm(int kmProximaRevisao, int kmAtual)
        {
            return kmProximaRevisao - kmAtual;
        }

        /// <summary>
        /// Situacao do alerta: VENCIDA quando faltam zero km ou menos; PROXIMA quando faltam mais que zero e no maximo a
        /// margem; EM_DIA no restante.
        /// </summary>
        public static string Situacao(int faltamKm)
        {
            if (faltamKm <= 0)
            {
                return ConstantsValues.SituacaoRevisaoVencida;
            }

            if (faltamKm <= ConstantsValues.MargemRevisaoProximaKm)
            {
                return ConstantsValues.SituacaoRevisaoProxima;
            }

            return ConstantsValues.SituacaoRevisaoEmDia;
        }

        /// <summary>
        /// Percentual do ciclo ja percorrido: (km atual menos a base) vezes cem, sobre o intervalo, com uma casa decimal
        /// (arredondamento para longe de zero). Pode passar de cem.
        /// </summary>
        public static decimal PercentualCiclo(int kmAtual, int kmBase)
        {
            decimal percentual = (decimal)(kmAtual - kmBase) * ConstantsValues.PercentualBaseRevisao / ConstantsValues.IntervaloRevisaoKm;

            return Math.Round(percentual, ConstantsValues.CasasPercentualCicloRevisao, MidpointRounding.AwayFromZero);
        }
    }
}
