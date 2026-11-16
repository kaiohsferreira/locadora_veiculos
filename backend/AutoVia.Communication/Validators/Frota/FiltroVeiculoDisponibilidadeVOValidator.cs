using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Frota
{
    public class FiltroVeiculoDisponibilidadeVOValidator : AbstractValidator<FiltroVeiculoDisponibilidadeVO>
    {
        public FiltroVeiculoDisponibilidadeVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());
            RuleFor(x => x.DataRetirada).NotEqual(default(DateOnly)).WithMessage(ConstantsMessage.ErroDisponibilidadeDataObrigatoria);
            RuleFor(x => x.DataDevolucao).NotEqual(default(DateOnly)).WithMessage(ConstantsMessage.ErroDisponibilidadeDataObrigatoria);
            RuleFor(x => x.DataDevolucao).GreaterThan(x => x.DataRetirada).WithMessage(ConstantsMessage.ErroDisponibilidadePeriodoInvalido);
            RuleFor(x => x.IdCategoria).GreaterThan(0).When(x => x.IdCategoria.HasValue).WithMessage(ConstantsMessage.ErroDisponibilidadeCategoriaInvalida);
            RuleFor(x => x.ValorDiariaMin).GreaterThanOrEqualTo(0m).When(x => x.ValorDiariaMin.HasValue).WithMessage(ConstantsMessage.ErroDisponibilidadeValorInvalido);
            RuleFor(x => x.ValorDiariaMax).GreaterThanOrEqualTo(0m).When(x => x.ValorDiariaMax.HasValue).WithMessage(ConstantsMessage.ErroDisponibilidadeValorInvalido);
            RuleFor(x => x.ValorDiariaMax).GreaterThanOrEqualTo(x => x.ValorDiariaMin).When(x => x.ValorDiariaMin.HasValue && x.ValorDiariaMax.HasValue).WithMessage(ConstantsMessage.ErroDisponibilidadeFaixaInvalida);
        }
    }
}
