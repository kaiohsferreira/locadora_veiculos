using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Pagamento
{
    /// <summary>Valida o periodo do resumo das cobrancas do cliente (lista fechada: DOZE_MESES, ANO_ATUAL, SEIS_MESES ou TUDO).</summary>
    public class FiltroResumoMeusPagamentosVOValidator : AbstractValidator<FiltroResumoMeusPagamentosVO>
    {
        public FiltroResumoMeusPagamentosVOValidator()
        {
            RuleFor(x => x.Periodo)
                .Must(periodo => periodo == null || RegrasEntradaPagamento.EhPeriodoValido(periodo))
                .WithMessage(ConstantsMessage.ErroPagamentoPeriodoInvalido);
        }
    }
}
