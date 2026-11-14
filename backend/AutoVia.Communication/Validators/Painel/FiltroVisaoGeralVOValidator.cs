using AutoVia.Communication.ViewObjects.Painel;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Painel
{
    /// <summary>
    /// Validador FluentValidation do filtro da visao geral: o periodo precisa ser um dos quatro valores do enum. Valor fora do
    /// enum (por exemplo, um numero que nao existe) devolve 400 antes da action.
    /// </summary>
    public class FiltroVisaoGeralVOValidator : AbstractValidator<FiltroVisaoGeralVO>
    {
        public FiltroVisaoGeralVOValidator()
        {
            RuleFor(x => x.Periodo)
                .Must(periodo => Enum.IsDefined(periodo))
                .WithMessage(ConstantsMessage.ErroRequisicaoInvalida);
        }
    }
}
