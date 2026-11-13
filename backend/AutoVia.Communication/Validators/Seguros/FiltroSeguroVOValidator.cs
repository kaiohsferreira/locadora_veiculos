using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Seguros
{
    /// <summary>
    /// Validador FluentValidation do filtro da listagem de apolices (GET /api/seguros).
    /// Inclui as regras de paginacao (FiltroPaginacaoVOValidator), porque o filtro de acao
    /// resolve o validador pelo tipo exato. O veiculo so e validado quando informado.
    /// </summary>
    public class FiltroSeguroVOValidator : AbstractValidator<FiltroSeguroVO>
    {
        public FiltroSeguroVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.IdVeiculo)
                .Must(idVeiculo => !idVeiculo.HasValue || idVeiculo.Value > 0)
                .WithMessage(ConstantsMessage.ErroSeguroFiltroVeiculoInvalido);
        }
    }
}
