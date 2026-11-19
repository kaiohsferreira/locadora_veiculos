using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.CobrancasExtras;

public sealed class SimularCobrancasExtrasVOValidator : AbstractValidator<SimularCobrancasExtrasVO>
{
    public SimularCobrancasExtrasVOValidator()
    {
        RuleFor(x => x.PacoteKmTotal).NotNull().WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros)
            .GreaterThanOrEqualTo(0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.ToleranciaAtrasoMinutos).NotNull().WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros)
            .GreaterThanOrEqualTo(0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.ValorKmExcedente).Must(x => x.HasValue && x.Value > 0m && x.Value <= 99999999.99m && decimal.Round(x.Value, 2) == x.Value).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.ValorTanqueCompleto).Must(x => x.HasValue && x.Value > 0m && x.Value <= 99999999.99m && decimal.Round(x.Value, 2) == x.Value).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.ValorUnidadeAtraso).Must(x => x.HasValue && x.Value > 0m && x.Value <= 99999999.99m && decimal.Round(x.Value, 2) == x.Value).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.UnidadeAtraso).Must(x => x == ConstantsMessage.CobrancasExtrasHora || x == ConstantsMessage.CobrancasExtrasDiaria).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.FracionamentoAtraso).Must(x => x == ConstantsMessage.CobrancasExtrasProporcional || x == ConstantsMessage.CobrancasExtrasIniciada).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
    }
}
