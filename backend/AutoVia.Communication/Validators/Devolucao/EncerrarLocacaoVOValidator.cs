using AutoVia.Communication.ViewObjects.Devolucao;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Devolucao;

/// <summary>Validacao do corpo do encerramento: politica e versao positivas, total com ate 2 casas e dentro do limite.</summary>
public class EncerrarLocacaoVOValidator : AbstractValidator<EncerrarLocacaoVO>
{
    public EncerrarLocacaoVOValidator()
    {
        RuleFor(x => x.IdConfiguracao).GreaterThan(0).WithMessage(ConstantsMessage.ErroEncerramentoParametros);
        RuleFor(x => x.VersaoEsperada).GreaterThan(0).WithMessage(ConstantsMessage.ErroEncerramentoParametros);
        RuleFor(x => x.TotalConfirmado).GreaterThanOrEqualTo(0m).WithMessage(ConstantsMessage.ErroEncerramentoParametros);
        RuleFor(x => x.TotalConfirmado).LessThanOrEqualTo(ConstantsValues.LimiteTotalEncerramento).WithMessage(ConstantsMessage.ErroEncerramentoParametros);
        RuleFor(x => x.TotalConfirmado).Must(x => decimal.Round(x, 2) == x).WithMessage(ConstantsMessage.ErroEncerramentoParametros);
    }
}
