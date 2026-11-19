using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Regra pura dos alertas de revisao (P8-BE-03, RF-39), sem banco: base (sem preventiva = zero), proxima revisao,
    /// faltam, situacao nos limites exatos (margem e intervalo) e percentual do ciclo com uma casa decimal.
    /// </summary>
    public class RegrasRevisaoTests
    {
        private const int KmBaseDoTeste = 4500;
        private const int KmPreventivaDoTeste = 4500;

        [Fact]
        public void SemPreventiva_BaseEZero()
        {
            Assert.Equal(0, RegrasRevisao.KmBase(null));
        }

        [Fact]
        public void ComPreventiva_BaseEOMaiorKmInformado()
        {
            Assert.Equal(KmPreventivaDoTeste, RegrasRevisao.KmBase(KmPreventivaDoTeste));
        }

        [Fact]
        public void ProximaRevisao_EBaseMaisOIntervaloFixo()
        {
            Assert.Equal(KmBaseDoTeste + ConstantsValues.IntervaloRevisaoKm, RegrasRevisao.KmProximaRevisao(KmBaseDoTeste));
            Assert.Equal(ConstantsValues.IntervaloRevisaoKm, RegrasRevisao.KmProximaRevisao(0));
        }

        [Fact]
        public void Faltam_EProximaMenosKmAtual()
        {
            Assert.Equal(1000, RegrasRevisao.FaltamKm(14500, 13500));
            Assert.Equal(-200, RegrasRevisao.FaltamKm(14500, 14700));
        }

        [Fact]
        public void ExatamenteNoLimiteDaMargem_EProxima()
        {
            Assert.Equal(ConstantsValues.SituacaoRevisaoProxima, RegrasRevisao.Situacao(ConstantsValues.MargemRevisaoProximaKm));
        }

        [Fact]
        public void UmKmAcimaDaMargem_EEmDia()
        {
            Assert.Equal(ConstantsValues.SituacaoRevisaoEmDia, RegrasRevisao.Situacao(ConstantsValues.MargemRevisaoProximaKm + 1));
        }

        [Fact]
        public void UmKmAbaixoDaMargem_EProxima()
        {
            Assert.Equal(ConstantsValues.SituacaoRevisaoProxima, RegrasRevisao.Situacao(1));
        }

        [Fact]
        public void ExatamenteNoIntervalo_FaltamZero_EVencida()
        {
            int faltam = RegrasRevisao.FaltamKm(RegrasRevisao.KmProximaRevisao(KmBaseDoTeste), KmBaseDoTeste + ConstantsValues.IntervaloRevisaoKm);

            Assert.Equal(0, faltam);
            Assert.Equal(ConstantsValues.SituacaoRevisaoVencida, RegrasRevisao.Situacao(faltam));
        }

        [Fact]
        public void AcimaDoIntervalo_FaltamNegativos_EVencida()
        {
            Assert.Equal(ConstantsValues.SituacaoRevisaoVencida, RegrasRevisao.Situacao(-200));
        }

        [Fact]
        public void PercentualDoCiclo_TemUmaCasaDecimal()
        {
            Assert.Equal(45.0m, RegrasRevisao.PercentualCiclo(9000, KmBaseDoTeste));
            Assert.Equal(123.5m, RegrasRevisao.PercentualCiclo(12345, 0));
        }

        [Fact]
        public void PercentualDoCiclo_PodePassarDeCem()
        {
            Assert.Equal(120.0m, RegrasRevisao.PercentualCiclo(12000, 0));
        }
    }
}
