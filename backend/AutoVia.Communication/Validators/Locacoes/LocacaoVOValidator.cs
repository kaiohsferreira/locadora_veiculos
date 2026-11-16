using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Locacoes
{
    /// <summary>Valida os dados para iniciar uma locacao.</summary>
    public class LocacaoVOValidator : AbstractValidator<LocacaoVO>
    {
        public LocacaoVOValidator()
        {
            RuleFor(x => x.IdReserva)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroLocacaoReservaObrigatoria);
        }
    }
}
