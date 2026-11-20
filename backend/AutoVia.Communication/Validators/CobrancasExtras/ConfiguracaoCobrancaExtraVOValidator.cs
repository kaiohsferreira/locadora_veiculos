using AutoVia.Communication.ViewObjects.CobrancasExtras;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.CobrancasExtras;

public sealed class ConfiguracaoCobrancaExtraVOValidator : AbstractValidator<ConfiguracaoCobrancaExtraVO>
{
    public ConfiguracaoCobrancaExtraVOValidator()
    {
        RuleFor(x => x.IdCategoria).Must(x => x.HasValue && x.Value > 0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.FranquiaKm).Must(x => x.HasValue && x.Value >= 0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.BaseFranquia).Must(x => x == ConstantsMessage.CobrancasExtrasTotal || x == ConstantsMessage.CobrancasExtrasPorDiaria).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.BaseAvarias).Equal(ConstantsMessage.CobrancasExtrasEstimativaVistoria).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x).Custom((model, context) =>
        {
            SimularCobrancasExtrasVOValidator validator = new SimularCobrancasExtrasVOValidator();
            FluentValidation.Results.ValidationResult resultado = validator.Validate(new SimularCobrancasExtrasVO
            {
                PacoteKmTotal = model.FranquiaKm, ValorKmExcedente = model.ValorKmExcedente,
                ValorTanqueCompleto = model.ValorTanqueCompleto, ToleranciaAtrasoMinutos = model.ToleranciaAtrasoMinutos,
                UnidadeAtraso = model.UnidadeAtraso, FracionamentoAtraso = model.FracionamentoAtraso,
                ValorUnidadeAtraso = model.ValorUnidadeAtraso
            });
            foreach (FluentValidation.Results.ValidationFailure falha in resultado.Errors) context.AddFailure(falha);
        });
    }
}

public sealed class ApurarCobrancasExtrasVOValidator : AbstractValidator<ApurarCobrancasExtrasVO>
{
    public ApurarCobrancasExtrasVOValidator()
    {
        RuleFor(x => x.IdConfiguracao).Must(x => x.HasValue && x.Value > 0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
        RuleFor(x => x.VersaoEsperada).Must(x => x.HasValue && x.Value > 0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
    }
}

public sealed class FiltroConfiguracaoCobrancaExtraVOValidator : AbstractValidator<FiltroConfiguracaoCobrancaExtraVO>
{
    public FiltroConfiguracaoCobrancaExtraVOValidator()
    {
        Include(new FiltroPaginacaoVOValidator());
        RuleFor(x => x.IdCategoria).Must(x => !x.HasValue || x.Value > 0).WithMessage(ConstantsMessage.ErroCobrancasExtrasParametros);
    }
}
