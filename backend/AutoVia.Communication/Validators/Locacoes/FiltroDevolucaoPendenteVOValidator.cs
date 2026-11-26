using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Locacoes
{
    /// <summary>
    /// Validador do filtro da lista de devolucoes pendentes (GET /api/locacoes). Inclui a paginacao (FiltroPaginacaoVOValidator).
    /// Status, quando vier, e EM_ANDAMENTO ou ATRASADA (nome exato, sem trocar maiusculas); vazio ou ausente vale EM_ANDAMENTO.
    /// Qualquer outro texto responde 400 antes da action.
    /// </summary>
    public class FiltroDevolucaoPendenteVOValidator : AbstractValidator<FiltroDevolucaoPendenteVO>
    {
        public FiltroDevolucaoPendenteVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Status)
                .Must(status => string.IsNullOrEmpty(status)
                    || status == nameof(StatusLocacao.EM_ANDAMENTO)
                    || status == nameof(StatusLocacao.ATRASADA))
                .WithMessage(ConstantsMessage.ErroFiltroDevolucaoPendenteInvalido);
        }
    }
}
