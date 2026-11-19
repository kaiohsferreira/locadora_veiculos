using AutoVia.Communication.Enums;
using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Pagamento
{
    /// <summary>
    /// Validador do lancamento por rota (POST /api/pagamentos): origem unica, tipo pelo nome (CAUCAO recusada por esta rota),
    /// tipo x origem, valor com precisao NUMERIC(12,2) e vencimento obrigatorio. Toda recusa vira 400 antes do servico.
    /// </summary>
    public class LancarPagamentoVOValidator : AbstractValidator<LancarPagamentoVO>
    {
        public LancarPagamentoVOValidator()
        {
            RuleFor(x => x).Custom((vo, contexto) =>
            {
                string? erroDeOrigem = RegrasEntradaPagamento.ErroDeOrigem(vo.IdLocacao, vo.IdVenda);

                if (erroDeOrigem != null)
                {
                    contexto.AddFailure(nameof(LancarPagamentoVO.IdLocacao), erroDeOrigem);
                }
            });

            RuleFor(x => x.Tipo)
                .Must(tipo => !string.IsNullOrWhiteSpace(tipo))
                .WithMessage(ConstantsMessage.ErroPagamentoTipoObrigatorio);

            RuleFor(x => x.Tipo)
                .Must(tipo => !RegrasEntradaPagamento.EhCaucao(tipo))
                .WithMessage(ConstantsMessage.ErroPagamentoCaucaoPorRota);

            RuleFor(x => x.Tipo)
                .Must(tipo => string.IsNullOrWhiteSpace(tipo) || RegrasEntradaPagamento.EhCaucao(tipo) || RegrasEntradaPagamento.TentarLerTipo(tipo, out TipoPagamento _))
                .WithMessage(ConstantsMessage.ErroPagamentoTipoInvalido);

            RuleFor(x => x).Custom((vo, contexto) =>
            {
                if (!RegrasEntradaPagamento.TentarLerTipo(vo.Tipo, out TipoPagamento tipo) || tipo == TipoPagamento.CAUCAO)
                {
                    return;
                }

                string? erroDoTipo = RegrasEntradaPagamento.ErroDeTipoPorOrigem(tipo, vo.IdLocacao.HasValue, vo.IdVenda.HasValue);

                if (erroDoTipo != null)
                {
                    contexto.AddFailure(nameof(LancarPagamentoVO.Tipo), erroDoTipo);
                }
            });

            RuleFor(x => x.Valor).Custom((valor, contexto) =>
            {
                string? erroDoValor = RegrasEntradaPagamento.ErroDeValor(valor);

                if (erroDoValor != null)
                {
                    contexto.AddFailure(nameof(LancarPagamentoVO.Valor), erroDoValor);
                }
            });

            RuleFor(x => x.DataVencimento)
                .NotNull()
                .WithMessage(ConstantsMessage.ErroPagamentoVencimentoObrigatorio);
        }
    }
}
