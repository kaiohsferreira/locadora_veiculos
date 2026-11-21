using AutoVia.Api.Requests.Avaria;
using System.Globalization;

namespace AutoVia.Tests.Avarias
{
    /// <summary>Formatos do valor estimado do formulario da avaria: aceitos e recusados pela funcao pura, sem cultura.</summary>
    public sealed class ConversorValorEstimadoFormularioTests
    {
        [Theory]
        [InlineData("150.50", "150.50")]
        [InlineData("150.5", "150.5")]
        [InlineData("1500", "1500")]
        [InlineData("0", "0")]
        [InlineData("0.00", "0.00")]
        [InlineData("0.01", "0.01")]
        [InlineData("00150.50", "150.50")]
        [InlineData("99999999.99", "99999999.99")]
        public void FormatoAceito_ConverteComPontoDecimal(string texto, string esperado)
        {
            bool convertido = ConversorValorEstimadoFormulario.TentarConverter(texto, out decimal? valor);

            Assert.True(convertido);
            Assert.Equal(decimal.Parse(esperado, CultureInfo.InvariantCulture), valor);
        }

        [Theory]
        [InlineData("150,50")]
        [InlineData("150,5")]
        [InlineData("1.500,00")]
        [InlineData("1,500.00")]
        [InlineData("1.500")]
        [InlineData("1 500")]
        [InlineData(" 150.50")]
        [InlineData("150.50 ")]
        [InlineData("   ")]
        [InlineData("+150")]
        [InlineData("-1")]
        [InlineData("1e3")]
        [InlineData("1E3")]
        [InlineData("150.")]
        [InlineData(".50")]
        [InlineData("150.505")]
        [InlineData("1.5.0")]
        [InlineData("150.50\n")]
        [InlineData("abc")]
        [InlineData("0x1F")]
        [InlineData("١٥٠")]
        [InlineData("99999999999999999999999999999999.99")]
        public void FormatoRecusado_DevolveFalsoESemValor(string texto)
        {
            bool convertido = ConversorValorEstimadoFormulario.TentarConverter(texto, out decimal? valor);

            Assert.False(convertido);
            Assert.Null(valor);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void VazioOuNulo_SignificaSemValor(string? texto)
        {
            bool convertido = ConversorValorEstimadoFormulario.TentarConverter(texto, out decimal? valor);

            Assert.True(convertido);
            Assert.Null(valor);
        }

        [Fact]
        public void FaixaDeValor_NaoEhConferidaAqui_FicaNoValidador()
        {
            bool convertido = ConversorValorEstimadoFormulario.TentarConverter("100000000.00", out decimal? valor);

            Assert.True(convertido);
            Assert.Equal(100000000.00m, valor);
        }
    }
}
