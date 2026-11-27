using AutoVia.Core.Regras;
using System.Collections.Generic;
using Xunit;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes do calculo do parcelamento da proposta de compra (RF-27): parcela = (valor de venda - entrada) / parcelas,
    /// arredondada para baixo no centavo, e a ultima parcela absorve a diferenca. A conta "feita a parte" do criterio de
    /// comprovacao esta escrita em cada caso.
    /// </summary>
    public class RegrasPropostaCompraTests
    {
        [Fact]
        public void DivisaoExata_TodasAsParcelasIguais()
        {
            // (80.000,00 - 20.000,00) / 12 = 5.000,00
            ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(80000m, 20000m, 12);

            Assert.Equal(60000m, parcelamento.ValorFinanciado);
            Assert.Equal(5000m, parcelamento.ValorParcela);
            Assert.Equal(5000m, parcelamento.ValorUltimaParcela);
        }

        [Fact]
        public void DivisaoQueNaoFecha_UltimaParcelaAbsorveOCentavo()
        {
            // 10.000,00 / 3 = 3.333,333... -> 3.333,33 + 3.333,33 + 3.333,34 = 10.000,00
            ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(10000m, 0m, 3);

            Assert.Equal(3333.33m, parcelamento.ValorParcela);
            Assert.Equal(3333.34m, parcelamento.ValorUltimaParcela);
        }

        [Fact]
        public void ArredondaParaBaixoMesmoQuandoOTerceiroDigitoPassaDeCinco()
        {
            // 20.000,00 / 3 = 6.666,666... -> parcela 6.666,66 (para baixo) e ultima 6.666,68
            ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(20000m, 0m, 3);

            Assert.Equal(6666.66m, parcelamento.ValorParcela);
            Assert.Equal(6666.68m, parcelamento.ValorUltimaParcela);
        }

        [Fact]
        public void UmaParcela_ParcelaEUltimaSaoOValorFinanciado()
        {
            ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(45990.90m, 5990.90m, 1);

            Assert.Equal(40000m, parcelamento.ValorParcela);
            Assert.Equal(40000m, parcelamento.ValorUltimaParcela);
        }

        public static IEnumerable<object[]> CasosDeSoma()
        {
            yield return new object[] { 89900m, 10000m, 60 };
            yield return new object[] { 10000m, 0m, 7 };
            yield return new object[] { 123456.78m, 0.01m, 59 };
            yield return new object[] { 0.60m, 0m, 60 };
            yield return new object[] { 99999999.99m, 0m, 13 };
        }

        [Theory]
        [MemberData(nameof(CasosDeSoma))]
        public void SomaDasParcelas_FechaNoValorFinanciadoEAUltimaNuncaEMenor(decimal valorVenda, decimal valorEntrada, int numeroParcelas)
        {
            ParcelamentoCalculado parcelamento = RegrasPropostaCompra.CalcularParcelamento(valorVenda, valorEntrada, numeroParcelas);

            decimal soma = (parcelamento.ValorParcela * (numeroParcelas - 1)) + parcelamento.ValorUltimaParcela;

            Assert.Equal(valorVenda - valorEntrada, soma);
            Assert.True(parcelamento.ValorUltimaParcela >= parcelamento.ValorParcela);
            Assert.True(parcelamento.ValorUltimaParcela - parcelamento.ValorParcela < numeroParcelas * 0.01m);
            Assert.Equal(decimal.Round(parcelamento.ValorParcela, 2), parcelamento.ValorParcela);
            Assert.Equal(decimal.Round(parcelamento.ValorUltimaParcela, 2), parcelamento.ValorUltimaParcela);
        }

        [Theory]
        [InlineData(50000, 49999.99, true)]
        [InlineData(50000, 50000, false)]
        [InlineData(50000, 50000.01, false)]
        public void EntradaPrecisaSerMenorQueOValor(decimal valorVenda, decimal valorEntrada, bool esperado)
        {
            Assert.Equal(esperado, RegrasPropostaCompra.EntradaMenorQueValor(valorVenda, valorEntrada));
        }

        [Fact]
        public void ParcelaAbaixoDeUmCentavo_NaoAtingeOMinimo()
        {
            // 0,59 / 60 = 0,0098... -> parcela 0,00
            ParcelamentoCalculado abaixo = RegrasPropostaCompra.CalcularParcelamento(0.59m, 0m, 60);
            ParcelamentoCalculado noLimite = RegrasPropostaCompra.CalcularParcelamento(0.60m, 0m, 60);

            Assert.False(RegrasPropostaCompra.ParcelaAtingeOMinimo(abaixo));
            Assert.True(RegrasPropostaCompra.ParcelaAtingeOMinimo(noLimite));
        }
    }
}
