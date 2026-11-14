using AutoVia.Communication.ViewObjects.Equipe;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Equipe
{
    /// <summary>
    /// Validador FluentValidation da situacao do funcionario (PATCH /api/equipe/{id}/situacao): o campo ativo e obrigatorio.
    /// </summary>
    public class SituacaoFuncionarioVOValidator : AbstractValidator<SituacaoFuncionarioVO>
    {
        public SituacaoFuncionarioVOValidator()
        {
            RuleFor(x => x.Ativo)
                .Must(ativo => ativo.HasValue)
                .WithMessage(ConstantsMessage.ErroEquipeSituacaoObrigatoria);
        }
    }
}
