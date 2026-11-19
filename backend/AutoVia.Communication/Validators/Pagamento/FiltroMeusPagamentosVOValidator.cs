using AutoVia.Communication.Regras;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Pagamento
{
    /// <summary>Valida os filtros das cobrancas do cliente: paginacao, aba (TODOS, PENDENTES ou PAGOS) e periodo (lista fechada).</summary>
    public class FiltroMeusPagamentosVOValidator : AbstractValidator<FiltroMeusPagamentosVO>
    {
        public FiltroMeusPagamentosVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Aba)
                .Must(aba => aba == null || RegrasEntradaPagamento.EhAbaValida(aba))
                .WithMessage(ConstantsMessage.ErroPagamentoAbaInvalida);

            RuleFor(x => x.Periodo)
                .Must(periodo => periodo == null || RegrasEntradaPagamento.EhPeriodoValido(periodo))
                .WithMessage(ConstantsMessage.ErroPagamentoPeriodoInvalido);
        }
    }
}
