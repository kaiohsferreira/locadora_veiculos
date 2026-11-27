using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Vendas
{
    /// <summary>
    /// Validador do corpo do POST /api/propostas-compra: veiculo positivo e as mesmas regras de entrada e parcelas da
    /// simulacao (SimulacaoParcelamentoVOValidator). O filtro de acao resolve o validador pelo tipo exato, por isso as
    /// regras sao repetidas aqui com as mesmas constantes.
    /// </summary>
    public class PropostaCompraVOValidator : AbstractValidator<PropostaCompraVO>
    {
        public PropostaCompraVOValidator()
        {
            RuleFor(x => x.IdVeiculo)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroPropostaVeiculoInvalido);

            RuleFor(x => x.ValorEntrada)
                .GreaterThanOrEqualTo(0m)
                .WithMessage(ConstantsMessage.ErroPropostaEntradaNegativa);

            RuleFor(x => x.ValorEntrada)
                .PrecisionScale(ConstantsValues.PrecisaoValorProposta, ConstantsValues.EscalaValorProposta, true)
                .WithMessage(ConstantsMessage.ErroPropostaEntradaPrecisao);

            RuleFor(x => x.NumeroParcelas)
                .InclusiveBetween(ConstantsValues.NumeroParcelasMinimo, ConstantsValues.NumeroParcelasMaximo)
                .WithMessage(string.Format(ConstantsMessage.ErroPropostaParcelasForaDoLimite, ConstantsValues.NumeroParcelasMinimo, ConstantsValues.NumeroParcelasMaximo));
        }
    }
}
