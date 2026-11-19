using AutoVia.Communication.Enums;
using AutoVia.Communication.Regras;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Pagamento
{
    /// <summary>Valida os filtros da listagem da locadora: paginacao (inclusive o estouro de deslocamento), status exato e intervalo de vencimento.</summary>
    public class FiltroPagamentoVOValidator : AbstractValidator<FiltroPagamentoVO>
    {
        public FiltroPagamentoVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Status).Custom((status, contexto) =>
            {
                if (status != null && !RegrasEntradaPagamento.TentarLerStatus(status, out StatusPagamento _))
                {
                    contexto.AddFailure(nameof(FiltroPagamentoVO.Status), ConstantsMessage.ErroPagamentoStatusInvalido);
                }
            });

            RuleFor(x => x.VencimentoAte).Custom((ate, contexto) =>
            {
                FiltroPagamentoVO filtro = contexto.InstanceToValidate;

                if (filtro.VencimentoDe.HasValue && ate.HasValue && filtro.VencimentoDe.Value > ate.Value)
                {
                    contexto.AddFailure(nameof(FiltroPagamentoVO.VencimentoAte), ConstantsMessage.ErroPagamentoVencimentoIntervalo);
                }
            });
        }
    }
}
