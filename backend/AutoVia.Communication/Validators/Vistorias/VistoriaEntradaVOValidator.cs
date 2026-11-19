using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Vistorias
{
    public class VistoriaEntradaVOValidator : AbstractValidator<VistoriaEntradaVO>
    {
        public VistoriaEntradaVOValidator()
        {
            RuleFor(x => x.KmRegistrado).NotNull().WithMessage(ConstantsMessage.ErroVistoriaKmObrigatorio);
            RuleFor(x => x.KmRegistrado).GreaterThanOrEqualTo(0).WithMessage(ConstantsMessage.ErroVistoriaKmInvalido);
            RuleFor(x => x.NivelCombustivel).NotNull().WithMessage(ConstantsMessage.ErroVistoriaCombustivelObrigatorio);
            RuleFor(x => x.NivelCombustivel).InclusiveBetween(0m, 1m).WithMessage(ConstantsMessage.ErroVistoriaCombustivelInvalido);
            RuleFor(x => x.NivelCombustivel).Must(nivel => !nivel.HasValue || decimal.Round(nivel.Value, 2) == nivel.Value)
                .WithMessage(ConstantsMessage.ErroVistoriaCombustivelInvalido);
            RuleFor(x => x.Observacoes).Must(observacoes => observacoes == null || !observacoes.Contains('\0'))
                .WithMessage(ConstantsMessage.ErroVistoriaObservacoesInvalidas);
        }
    }
}
