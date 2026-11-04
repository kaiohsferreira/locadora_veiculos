using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Helpers
{
    public class StaticMethodsTests
    {
        [Theory]
        [InlineData("529.982.247-25", true)]
        [InlineData("52998224725", true)]
        [InlineData("529.982.247-24", false)]
        [InlineData("111.111.111-11", false)]
        [InlineData("123", false)]
        [InlineData(null, false)]
        public void IsCpfValido_ConfereDigitosVerificadores(string? cpf, bool esperado)
        {
            Assert.Equal(esperado, StaticMethods.IsCpfValido(cpf));
        }

        [Theory]
        [InlineData("ABC1234", true)]
        [InlineData("abc-1234", true)]
        [InlineData("BRA2E19", true)]
        [InlineData("AB12345", false)]
        [InlineData("ABC12345", false)]
        [InlineData("", false)]
        public void IsPlacaValida_AceitaPadraoAntigoEMercosul(string placa, bool esperado)
        {
            Assert.Equal(esperado, StaticMethods.IsPlacaValida(placa));
        }

        [Fact]
        public void NormalizePlaca_DeixaNoFormatoDaColunaChar7()
        {
            Assert.Equal("BRA2E19", StaticMethods.NormalizePlaca(" bra-2e19 "));
        }

        [Fact]
        public void FormateToMonetary_UsaFormatoBrasileiro()
        {
            string valor = StaticMethods.FormateToMonetary(1234.5m);

            Assert.Contains("1.234,50", valor);
            Assert.StartsWith("R$", valor);
        }

        [Fact]
        public void FormateDateToPTBR_UsaDiaMesAno()
        {
            Assert.Equal("30/11/2026", StaticMethods.FormateDateToPTBR(new DateOnly(2026, 11, 30)));
        }

        [Fact]
        public void FormateCpf_AplicaMascara()
        {
            Assert.Equal("529.982.247-25", StaticMethods.FormateCpf("52998224725"));
        }

        [Fact]
        public void FiltroPaginacaoVOValidator_RecusaTamanhoAcimaDoMaximo()
        {
            FiltroPaginacaoVOValidator validator = new FiltroPaginacaoVOValidator();
            FiltroPaginacaoVO filtro = new FiltroPaginacaoVO { Pagina = 1, TamanhoPagina = 101 };

            ValidationResult resultado = validator.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, x => x.PropertyName == nameof(FiltroPaginacaoVO.TamanhoPagina));
        }

        [Fact]
        public void FiltroPaginacaoVOValidator_AceitaPadrao()
        {
            FiltroPaginacaoVOValidator validator = new FiltroPaginacaoVOValidator();

            ValidationResult resultado = validator.Validate(new FiltroPaginacaoVO());

            Assert.True(resultado.IsValid);
            Assert.Equal(0, new FiltroPaginacaoVO { Pagina = 1 }.Deslocamento);
        }
    }
}
