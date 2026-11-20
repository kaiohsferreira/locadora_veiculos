using AutoVia.Core.Regras;
using System;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Periodo padrao do resumo da manutencao (funcao pura, sem banco): primeiro e ultimo dia do mes do relogio, inclusive
    /// em fevereiro (comum e bissexto) e em dezembro. A hora do relogio nao muda o periodo.
    /// </summary>
    public class RegrasResumoManutencaoTests
    {
        [Fact]
        public void PeriodoPadrao_MesDeTrintaDias_VaiDoPrimeiroAoUltimoDia()
        {
            PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 11, 20, 10, 0, 0));

            Assert.Equal(new DateOnly(2026, 11, 1), periodo.Inicio);
            Assert.Equal(new DateOnly(2026, 11, 30), periodo.Fim);
        }

        [Fact]
        public void PeriodoPadrao_MesDeTrintaEUmDias_VaiAteOTrintaEUm()
        {
            PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 1, 15, 8, 30, 0));

            Assert.Equal(new DateOnly(2026, 1, 1), periodo.Inicio);
            Assert.Equal(new DateOnly(2026, 1, 31), periodo.Fim);
        }

        [Fact]
        public void PeriodoPadrao_FevereiroNaoBissexto_TerminaNoDia28()
        {
            PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 2, 10, 10, 0, 0));

            Assert.Equal(new DateOnly(2026, 2, 1), periodo.Inicio);
            Assert.Equal(new DateOnly(2026, 2, 28), periodo.Fim);
        }

        [Fact]
        public void PeriodoPadrao_FevereiroBissexto_TerminaNoDia29()
        {
            PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2028, 2, 10, 10, 0, 0));

            Assert.Equal(new DateOnly(2028, 2, 1), periodo.Inicio);
            Assert.Equal(new DateOnly(2028, 2, 29), periodo.Fim);
        }

        [Fact]
        public void PeriodoPadrao_Dezembro_VaiAteOTrintaEUmDeDezembro()
        {
            PeriodoResumoManutencao periodo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 12, 5, 10, 0, 0));

            Assert.Equal(new DateOnly(2026, 12, 1), periodo.Inicio);
            Assert.Equal(new DateOnly(2026, 12, 31), periodo.Fim);
        }

        [Fact]
        public void PeriodoPadrao_NoPrimeiroESultimoInstanteDoMes_FicaNoMesCerto()
        {
            PeriodoResumoManutencao primeiro = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 11, 1, 0, 0, 0));
            PeriodoResumoManutencao ultimo = RegrasResumoManutencao.PeriodoPadrao(new DateTime(2026, 11, 30, 23, 59, 59));

            Assert.Equal(new DateOnly(2026, 11, 1), primeiro.Inicio);
            Assert.Equal(new DateOnly(2026, 11, 30), ultimo.Fim);
            Assert.Equal(primeiro.Fim, ultimo.Fim);
        }
    }
}
