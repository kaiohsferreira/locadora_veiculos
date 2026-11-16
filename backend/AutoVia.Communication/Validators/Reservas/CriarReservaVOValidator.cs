using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Reservas
{
    /// <summary>Valida o corpo do POST de reserva: veiculo, datas presentes, periodo e duracao maxima.</summary>
    public class CriarReservaVOValidator : AbstractValidator<CriarReservaVO>
    {
        public CriarReservaVOValidator()
        {
            RuleFor(x => x.IdVeiculo).GreaterThan(0).WithMessage(ConstantsMessage.ErroReservaVeiculoInvalido);
            RuleFor(x => x.DataRetirada).NotEmpty().WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
            RuleFor(x => x.DataDevolucao).NotEmpty().WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
            RuleFor(x => x).Must(x => x.DataRetirada < x.DataDevolucao).WithMessage(ConstantsMessage.ErroReservaPeriodoInvalido);
            RuleFor(x => x.DataDevolucao)
                .Must((entrada, devolucao) => entrada.DataRetirada >= entrada.DataDevolucao
                    || devolucao.DayNumber - entrada.DataRetirada.DayNumber <= ConstantsValues.DuracaoMaximaReservaDiarias)
                .WithMessage(string.Format(ConstantsMessage.ErroReservaDuracaoMaxima, ConstantsValues.DuracaoMaximaReservaDiarias));
        }
    }
}
