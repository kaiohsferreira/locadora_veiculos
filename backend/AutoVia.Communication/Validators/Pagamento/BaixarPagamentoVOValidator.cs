using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Pagamento
{
    /// <summary>Validador da baixa (PATCH /api/pagamentos/{id}/baixa): forma obrigatoria e pelo nome exato da lista fechada (depois de Trim).</summary>
    public class BaixarPagamentoVOValidator : AbstractValidator<BaixarPagamentoVO>
    {
        public BaixarPagamentoVOValidator()
        {
            RuleFor(x => x.Forma).Custom((forma, contexto) =>
            {
                if (string.IsNullOrWhiteSpace(forma))
                {
                    contexto.AddFailure(nameof(BaixarPagamentoVO.Forma), ConstantsMessage.ErroPagamentoFormaObrigatoria);
                    return;
                }

                if (!RegrasEntradaPagamento.FormaValida(forma))
                {
                    contexto.AddFailure(nameof(BaixarPagamentoVO.Forma), ConstantsMessage.ErroPagamentoFormaInvalida);
                }
            });
        }
    }
}
