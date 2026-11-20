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
        // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400 (ErroCampoInvalido).
        private const char CaractereNulo = '\0';
        private const string NomeCampoNome = nameof(CategoriaVO.Nome);
        private const string NomeCampoDescricao = nameof(CategoriaVO.Descricao);

        public CategoriaVOValidator()
        {
            RuleFor(x => x.Nome)
                .Must(nome => !string.IsNullOrWhiteSpace(nome))
                .WithMessage(ConstantsMessage.ErroCategoriaNomeObrigatorio);

            RuleFor(x => x.Nome)
                .MaximumLength(ConstantsValues.TamanhoMaximoNomeCategoria)
                .WithMessage(string.Format(ConstantsMessage.ErroCategoriaNomeTamanho, ConstantsValues.TamanhoMaximoNomeCategoria));

            RuleFor(x => x.Nome)
                .Must(nome => nome == null || !nome.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoNome));

            RuleFor(x => x.Descricao)
                .MaximumLength(ConstantsValues.TamanhoMaximoDescricaoCategoria)
                .WithMessage(string.Format(ConstantsMessage.ErroCategoriaDescricaoTamanho, ConstantsValues.TamanhoMaximoDescricaoCategoria));

            RuleFor(x => x.Descricao)
                .Must(descricao => descricao == null || !descricao.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoDescricao));

            RuleFor(x => x.ValorDiariaBase)
                .GreaterThan(0m)
                .WithMessage(ConstantsMessage.ErroCategoriaValorDiaria)
                .PrecisionScale(10, 2, true)
                .WithMessage(ConstantsMessage.ErroCategoriaValorDiariaPrecisao);
        }
    }
}
