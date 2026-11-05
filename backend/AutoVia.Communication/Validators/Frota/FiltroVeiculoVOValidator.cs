using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Frota
{
    /// <summary>
    /// Validador FluentValidation do filtro da listagem de veiculos (GET /api/veiculos).
    /// Inclui as regras de paginacao (FiltroPaginacaoVOValidator), porque o filtro de acao
    /// resolve o validador pelo tipo exato. Categoria, status e placa so sao validados quando informados.
    /// As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class FiltroVeiculoVOValidator : AbstractValidator<FiltroVeiculoVO>
    {
        public FiltroVeiculoVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.IdCategoria)
                .Must(idCategoria => !idCategoria.HasValue || idCategoria.Value > 0)
                .WithMessage(ConstantsMessage.ErroVeiculoFiltroCategoriaInvalida);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, string.Join(", ", Enum.GetNames<StatusVeiculo>())));

            RuleFor(x => x.Placa)
                .MaximumLength(ConstantsValues.TamanhoMaximoBuscaPlacaVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoFiltroPlacaTamanho, ConstantsValues.TamanhoMaximoBuscaPlacaVeiculo));
        }
    }
}
