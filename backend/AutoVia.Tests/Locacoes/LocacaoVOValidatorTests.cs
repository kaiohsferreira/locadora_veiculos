using AutoVia.Communication.Validators.Locacoes;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Linq;

namespace AutoVia.Tests.Locacoes
{
    public class LocacaoVOValidatorTests
    {
        [Fact]
        public void IdReservaPositivo_PassaNaValidacao()
        {
            LocacaoVO vo = new LocacaoVO { IdReserva = 1 };
            LocacaoVOValidator validator = new LocacaoVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IdReservaNaoPositivo_FalhaComMensagemObrigatoria(int idReserva)
        {
            LocacaoVO vo = new LocacaoVO { IdReserva = idReserva };
            LocacaoVOValidator validator = new LocacaoVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroLocacaoReservaObrigatoria, resultado.Errors.Select(x => x.ErrorMessage));
        }
    }
}
