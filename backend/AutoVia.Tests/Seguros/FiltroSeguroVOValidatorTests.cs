using AutoVia.Communication.Validators.Seguros;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Seguros
{
    /// <summary>
    /// Testes do FiltroSeguroVOValidator: paginacao herdada e filtro de veiculo opcional.
    /// </summary>
    public class FiltroSeguroVOValidatorTests
    {
        [Fact]
        public void FiltroPadrao_PassaNaValidacao()
        {
            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(new FiltroSeguroVO());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void VeiculoZero_FalhaComMensagemDeFiltro()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { IdVeiculo = 0 };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.Contains(ConstantsMessage.ErroSeguroFiltroVeiculoInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void PaginaZero_FalhaComMensagemDePaginacao()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { Pagina = 0 };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.Contains(ConstantsMessage.ErroPaginaInvalida, resultado.Errors.Select(erro => erro.ErrorMessage));
        }
    }
}
