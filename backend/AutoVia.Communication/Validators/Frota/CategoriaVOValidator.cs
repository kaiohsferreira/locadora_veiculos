using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Frota
{
    /// <summary>
    /// Validador FluentValidation do VO de entrada da categoria (POST e PUT).
    /// As mensagens vem de ConstantsMessage. Os limites de tamanho vem de ConstantsValues.
    /// </summary>
    public class CategoriaVOValidator : AbstractValidator<CategoriaVO>
    {
        public CategoriaVOValidator()
        {
            RuleFor(x => x.Nome)
                .Must(nome => !string.IsNullOrWhiteSpace(nome))
                .WithMessage(ConstantsMessage.ErroCategoriaNomeObrigatorio);

            RuleFor(x => x.Nome)
                .MaximumLength(ConstantsValues.TamanhoMaximoNomeCategoria)
                .WithMessage(string.Format(ConstantsMessage.ErroCategoriaNomeTamanho, ConstantsValues.TamanhoMaximoNomeCategoria));

            RuleFor(x => x.Descricao)
                .MaximumLength(ConstantsValues.TamanhoMaximoDescricaoCategoria)
                .WithMessage(string.Format(ConstantsMessage.ErroCategoriaDescricaoTamanho, ConstantsValues.TamanhoMaximoDescricaoCategoria));

            RuleFor(x => x.ValorDiariaBase)
                .GreaterThan(0m)
                .WithMessage(ConstantsMessage.ErroCategoriaValorDiaria)
                .PrecisionScale(10, 2, true)
                .WithMessage(ConstantsMessage.ErroCategoriaValorDiariaPrecisao);
        }
    }
}
