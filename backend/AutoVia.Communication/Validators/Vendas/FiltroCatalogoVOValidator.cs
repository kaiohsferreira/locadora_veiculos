using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Vendas
{
    /// <summary>
    /// Validador FluentValidation do filtro do catalogo de seminovos (GET /api/veiculos/a-venda).
    /// Inclui as regras de paginacao (FiltroPaginacaoVOValidator), porque o filtro de acao
    /// resolve o validador pelo tipo exato. Cada filtro so e validado quando informado.
    /// </summary>
    public class FiltroCatalogoVOValidator : AbstractValidator<FiltroCatalogoVO>
    {
        public FiltroCatalogoVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Marca)
                .MaximumLength(ConstantsValues.TamanhoMaximoMarcaVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroCatalogoMarcaTamanho, ConstantsValues.TamanhoMaximoMarcaVeiculo));

            RuleFor(x => x.PrecoMinimo)
                .Must(preco => !preco.HasValue || preco.Value >= 0m)
                .WithMessage(ConstantsMessage.ErroCatalogoPrecoNegativo);

            RuleFor(x => x.PrecoMaximo)
                .Must(preco => !preco.HasValue || preco.Value >= 0m)
                .WithMessage(ConstantsMessage.ErroCatalogoPrecoNegativo);

            RuleFor(x => x.PrecoMaximo)
                .Must((filtro, precoMaximo) => !filtro.PrecoMinimo.HasValue || !precoMaximo.HasValue || filtro.PrecoMinimo.Value <= precoMaximo.Value)
                .WithMessage(ConstantsMessage.ErroCatalogoFaixaPreco);

            RuleFor(x => x.AnoMinimo)
                .Must(ano => !ano.HasValue || ano.Value > 0)
                .WithMessage(ConstantsMessage.ErroCatalogoAnoInvalido);

            RuleFor(x => x.AnoMaximo)
                .Must(ano => !ano.HasValue || ano.Value > 0)
                .WithMessage(ConstantsMessage.ErroCatalogoAnoInvalido);

            RuleFor(x => x.AnoMaximo)
                .Must((filtro, anoMaximo) => !filtro.AnoMinimo.HasValue || !anoMaximo.HasValue || filtro.AnoMinimo.Value <= anoMaximo.Value)
                .WithMessage(ConstantsMessage.ErroCatalogoFaixaAno);

            RuleFor(x => x.KmMaximo)
                .Must(km => !km.HasValue || km.Value >= 0)
                .WithMessage(ConstantsMessage.ErroCatalogoKmNegativo);
        }
    }
}
