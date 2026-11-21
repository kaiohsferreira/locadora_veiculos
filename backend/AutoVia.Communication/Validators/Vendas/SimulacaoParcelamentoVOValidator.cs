using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Vendas
{
    /// <summary>
    /// Validador da simulacao do parcelamento: entrada nao negativa e cabendo na coluna NUMERIC(12,2), parcelas de 1 a 60.
    /// A comparacao da entrada com o valor de venda depende do banco e fica no servico.
    /// </summary>
    public class SimulacaoParcelamentoVOValidator : AbstractValidator<SimulacaoParcelamentoVO>
    {
        public SimulacaoParcelamentoVOValidator()
        {
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
