using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Frota
{
    public class FiltroVeiculoLocacaoVOValidator : AbstractValidator<FiltroVeiculoLocacaoVO>
    {
        public FiltroVeiculoLocacaoVOValidator()
        {
            When(x => x.DataRetirada.HasValue || x.DataDevolucao.HasValue, () =>
            {
                RuleFor(x => x.DataRetirada).NotNull().WithMessage(ConstantsMessage.ErroDisponibilidadeDataObrigatoria);
                RuleFor(x => x.DataDevolucao).NotNull().WithMessage(ConstantsMessage.ErroDisponibilidadeDataObrigatoria);
                RuleFor(x => x.DataDevolucao)
                    .GreaterThan(x => x.DataRetirada)
                    .When(x => x.DataRetirada.HasValue && x.DataDevolucao.HasValue)
                    .WithMessage(ConstantsMessage.ErroDisponibilidadePeriodoInvalido);
            });
        }
    }
}
