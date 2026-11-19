using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Vistorias;

public class VistoriaSaidaVOValidator : AbstractValidator<VistoriaSaidaVO>
{
    public VistoriaSaidaVOValidator()
    {
        RuleFor(x => x.KmRegistrado).NotNull().WithMessage(ConstantsMessage.ErroVistoriaKmObrigatorio);
        RuleFor(x => x.KmRegistrado).GreaterThanOrEqualTo(0).WithMessage(ConstantsMessage.ErroVistoriaKmInvalido);
        RuleFor(x => x.NivelCombustivel).NotNull().WithMessage(ConstantsMessage.ErroVistoriaCombustivelObrigatorio);
        RuleFor(x => x.NivelCombustivel).InclusiveBetween(0m, 1m).WithMessage(ConstantsMessage.ErroVistoriaCombustivelInvalido);
        RuleFor(x => x.NivelCombustivel).Must(x => !x.HasValue || decimal.Round(x.Value, 2) == x.Value)
            .WithMessage(ConstantsMessage.ErroVistoriaCombustivelInvalido);
        RuleFor(x => x.Observacoes).Must(x => x == null || !x.Contains('\0'))
            .WithMessage(ConstantsMessage.ErroVistoriaObservacoesInvalidas);
        RuleFor(x => x.IdsAvariasEntrada).NotNull().WithMessage(ConstantsMessage.ErroVistoriaSaidaAvariasObrigatorias);
        RuleFor(x => x.IdsAvariasEntrada).Must(x => x == null || (x.All(id => id > 0) && x.Distinct().Count() == x.Count))
            .WithMessage(ConstantsMessage.ErroVistoriaSaidaIdsInvalidos);
        RuleFor(x => x.NovasAvarias).NotNull().WithMessage(ConstantsMessage.ErroVistoriaSaidaAvariasObrigatorias);
        RuleForEach(x => x.NovasAvarias).NotNull().WithMessage(ConstantsMessage.ErroVistoriaSaidaAvariasObrigatorias)
            .SetValidator(new NovaAvariaSaidaVOValidator());
    }
}

public class NovaAvariaSaidaVOValidator : AbstractValidator<NovaAvariaSaidaVO>
{
    public NovaAvariaSaidaVOValidator()
    {
        RuleFor(x => x.Descricao).NotEmpty().WithMessage(ConstantsMessage.ErroAvariaDescricaoObrigatoria);
        RuleFor(x => x.Descricao).MaximumLength(200).WithMessage(ConstantsMessage.ErroAvariaDescricaoTamanho);
        RuleFor(x => x.Descricao).Must(x => x == null || !x.Contains('\0')).WithMessage(ConstantsMessage.ErroVistoriaSaidaDescricaoInvalida);
        RuleFor(x => x.Gravidade).NotNull().WithMessage(ConstantsMessage.ErroAvariaGravidadeInvalida);
        RuleFor(x => x.Gravidade).IsInEnum().WithMessage(ConstantsMessage.ErroAvariaGravidadeInvalida);
        RuleFor(x => x.ValorEstimado).Must(x => !x.HasValue ||
            (x.Value >= 0m && x.Value <= 99999999.99m && decimal.Round(x.Value, 2) == x.Value))
            .WithMessage(ConstantsMessage.ErroAvariaValorEstimadoInvalido);
    }
}
