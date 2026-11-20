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
        // Caractere NUL: a busca vai para o SQL; o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
        private const char CaractereNulo = '\0';
        private const string NomeCampoBusca = nameof(FiltroEquipeVO.Busca);

        public FiltroEquipeVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Busca)
                .Must(busca => string.IsNullOrWhiteSpace(busca) || busca.Trim().Length <= ConstantsValues.TamanhoMaximoBuscaEquipe)
                .WithMessage(string.Format(ConstantsMessage.ErroEquipeBuscaTamanho, ConstantsValues.TamanhoMaximoBuscaEquipe));

            RuleFor(x => x.Busca)
                .Must(busca => busca == null || !busca.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoBusca));

            RuleFor(x => x.Cargo)
                .Must(cargo => !cargo.HasValue || Enum.IsDefined(cargo.Value))
                .WithMessage(ConstantsMessage.ErroRequisicaoInvalida);
        }
    }
}
