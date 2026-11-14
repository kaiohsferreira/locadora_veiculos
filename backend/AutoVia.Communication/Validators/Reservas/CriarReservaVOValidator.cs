using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Reservas
{
    public class CriarReservaVOValidator : AbstractValidator<CriarReservaVO>
    {
        public CriarReservaVOValidator()
        {
            RuleFor(x => x.IdVeiculo).GreaterThan(0).WithMessage(ConstantsMessage.ErroReservaVeiculoInvalido);
            RuleFor(x => x.DataRetirada).NotEmpty().WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
            RuleFor(x => x.DataDevolucao).NotEmpty().WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
            RuleFor(x => x).Must(x => x.DataRetirada < x.DataDevolucao).WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
        }
    }
}
