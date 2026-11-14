using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Painel;
using AutoVia.Communication.ViewObjects.Painel;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Validador do parametro periodo da visao geral: os quatro valores do enum passam; um valor fora do enum falha com a
    /// mensagem de requisicao invalida; sem o parametro vale TRINTA_DIAS.
    /// </summary>
    public class FiltroVisaoGeralVOValidatorTests
    {
        private const int ValorForaDoEnum = 99;

        [Theory]
        [InlineData(PeriodoVisaoGeral.HOJE)]
        [InlineData(PeriodoVisaoGeral.SETE_DIAS)]
        [InlineData(PeriodoVisaoGeral.TRINTA_DIAS)]
        [InlineData(PeriodoVisaoGeral.MES_ATUAL)]
        public void PeriodoDoEnum_EValido(PeriodoVisaoGeral periodo)
        {
            FiltroVisaoGeralVOValidator validador = new FiltroVisaoGeralVOValidator();
            FiltroVisaoGeralVO filtro = new FiltroVisaoGeralVO { Periodo = periodo };

            ValidationResult resultado = validador.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PeriodoForaDoEnum_EInvalido()
        {
            FiltroVisaoGeralVOValidator validador = new FiltroVisaoGeralVOValidator();
            FiltroVisaoGeralVO filtro = new FiltroVisaoGeralVO { Periodo = (PeriodoVisaoGeral)ValorForaDoEnum };

            ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Equal(ConstantsMessage.ErroRequisicaoInvalida, resultado.Errors[0].ErrorMessage);
        }

        [Fact]
        public void SemPeriodo_ValeTrintaDias()
        {
            FiltroVisaoGeralVO filtro = new FiltroVisaoGeralVO();

            Assert.Equal(PeriodoVisaoGeral.TRINTA_DIAS, filtro.Periodo);
        }
    }
}
