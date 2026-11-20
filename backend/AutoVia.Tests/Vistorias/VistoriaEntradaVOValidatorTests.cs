using AutoVia.Communication.Validators.Vistorias;
using AutoVia.Communication.ViewObjects.Vistorias;
using FluentValidation.Results;

namespace AutoVia.Tests.Vistorias
{
    public class VistoriaEntradaVOValidatorTests
    {
        [Theory]
        [InlineData(null, null, false)]
        [InlineData(null, "0.5", false)]
        [InlineData(10, null, false)]
        [InlineData(-1, "0.5", false)]
        [InlineData(0, "0", true)]
        [InlineData(0, "1", true)]
        [InlineData(10, "0.01", true)]
        [InlineData(10, "0.500", true)]
        [InlineData(10, "0.001", false)]
        [InlineData(10, "1.01", false)]
        [InlineData(10, "-0.01", false)]
        public void ValidaObrigatoriedadeFaixaEPrecisao(int? km, string? nivel, bool valido)
        {
            VistoriaEntradaVO entrada = new VistoriaEntradaVO
            {
                KmRegistrado = km,
                NivelCombustivel = nivel == null ? null : decimal.Parse(nivel, System.Globalization.CultureInfo.InvariantCulture)
            };
            ValidationResult resultado = new VistoriaEntradaVOValidator().Validate(entrada);
            Assert.Equal(valido, resultado.IsValid);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("Observação\ncom duas linhas", true)]
        [InlineData("texto\0invalido", false)]
        public void ObservacoesAceitamTextoLivreSemCaractereNulo(string? observacoes, bool valido)
        {
            ValidationResult resultado = new VistoriaEntradaVOValidator().Validate(new VistoriaEntradaVO
            {
                KmRegistrado = 0, NivelCombustivel = 1m, Observacoes = observacoes
            });
            Assert.Equal(valido, resultado.IsValid);
        }

        [Fact]
        public void ObservacoesSemLimiteArbitrario()
        {
            ValidationResult resultado = new VistoriaEntradaVOValidator().Validate(new VistoriaEntradaVO
            {
                KmRegistrado = int.MaxValue, NivelCombustivel = 1m, Observacoes = new string('a', 20000)
            });
            Assert.True(resultado.IsValid);
        }
    }
}
