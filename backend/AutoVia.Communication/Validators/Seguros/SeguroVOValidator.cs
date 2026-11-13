using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Seguros
{
    /// <summary>
    /// Validador FluentValidation do VO de entrada da apolice (POST e PUT). Roda antes da
    /// action (400). A regra de vigencia (fim posterior ao inicio) repete o CHECK
    /// ck_seguro_vigencia do banco, para o erro voltar com mensagem clara.
    /// As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class SeguroVOValidator : AbstractValidator<SeguroVO>
    {
        public SeguroVOValidator()
        {
            RuleFor(x => x.IdVeiculo)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroSeguroVeiculoObrigatorio);

            RuleFor(x => x.Seguradora)
                .Must(seguradora => !string.IsNullOrWhiteSpace(seguradora))
                .WithMessage(ConstantsMessage.ErroSeguroSeguradoraObrigatoria);

            RuleFor(x => x.Seguradora)
                .MaximumLength(ConstantsValues.TamanhoMaximoSeguradora)
                .WithMessage(string.Format(ConstantsMessage.ErroSeguroSeguradoraTamanho, ConstantsValues.TamanhoMaximoSeguradora));

            RuleFor(x => x.NumeroApolice)
                .Must(numero => !string.IsNullOrWhiteSpace(numero))
                .WithMessage(ConstantsMessage.ErroSeguroNumeroApoliceObrigatorio);

            RuleFor(x => x.NumeroApolice)
                .MaximumLength(ConstantsValues.TamanhoMaximoNumeroApolice)
                .WithMessage(string.Format(ConstantsMessage.ErroSeguroNumeroApoliceTamanho, ConstantsValues.TamanhoMaximoNumeroApolice));

            RuleFor(x => x.DataInicio)
                .NotEqual(default(DateOnly))
                .WithMessage(ConstantsMessage.ErroSeguroDataInicioObrigatoria);

            RuleFor(x => x.DataFim)
                .NotEqual(default(DateOnly))
                .WithMessage(ConstantsMessage.ErroSeguroDataFimObrigatoria);

            RuleFor(x => x.DataFim)
                .GreaterThan(x => x.DataInicio)
                .When(x => x.DataInicio != default(DateOnly) && x.DataFim != default(DateOnly))
                .WithMessage(ConstantsMessage.ErroSeguroVigenciaInvalida);

            RuleFor(x => x.ValorPremio)
                .GreaterThan(0m)
                .WithMessage(ConstantsMessage.ErroSeguroValorPremio)
                .PrecisionScale(10, 2, true)
                .WithMessage(ConstantsMessage.ErroSeguroValorPremioPrecisao);

            RuleFor(x => x.Cobertura)
                .MaximumLength(ConstantsValues.TamanhoMaximoCoberturaSeguro)
                .WithMessage(string.Format(ConstantsMessage.ErroSeguroCoberturaTamanho, ConstantsValues.TamanhoMaximoCoberturaSeguro));
        }
    }
}
