using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Equipe
{
    /// <summary>
    /// Validador FluentValidation do filtro da listagem da equipe: herda a paginacao e confere a busca (ate 60 caracteres
    /// depois do Trim) e o cargo, quando informado.
    /// </summary>
    public class FiltroEquipeVOValidator : AbstractValidator<FiltroEquipeVO>
    {
        public FiltroEquipeVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Busca)
                .Must(busca => string.IsNullOrWhiteSpace(busca) || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaEquipe)
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaEquipe));

            RuleFor(x => x.Cargo)
                .Must(cargo => !cargo.HasValue || Enum.IsDefined(cargo.Value))
                .WithMessage(ConstantsMessage.ErroRequisicaoInvalida);
        }
    }
}
