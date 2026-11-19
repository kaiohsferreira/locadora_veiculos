using AutoVia.Communication.ViewObjects.Manutencao;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Manutencao
{
    /// <summary>
    /// Validador FluentValidation do encerramento de ordem de manutencao (PATCH /api/manutencoes/{id}/encerrar). A
    /// quilometragem e obrigatoria e nao pode ser negativa; o custo, quando informado, nao pode ser negativo e cabe em
    /// NUMERIC(10,2). A data de saida futura e a comparacao com a data de entrada e com o km do veiculo ficam no
    /// repositorio e no servico. As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class EncerrarManutencaoVOValidator : AbstractValidator<EncerrarManutencaoVO>
    {
        public EncerrarManutencaoVOValidator()
        {
            RuleFor(x => x.KmAtual)
                .NotNull()
                .WithMessage(ConstantsMessage.ErroManutencaoKmObrigatorio);

            RuleFor(x => x.KmAtual)
                .GreaterThanOrEqualTo(0)
                .When(x => x.KmAtual.HasValue)
                .WithMessage(ConstantsMessage.ErroVeiculoKmNegativo);

            RuleFor(x => x.Custo)
                .GreaterThanOrEqualTo(0m)
                .When(x => x.Custo.HasValue)
                .WithMessage(ConstantsMessage.ErroManutencaoCustoInvalido);

            RuleFor(x => x.Custo)
                .PrecisionScale(ConstantsValues.PrecisaoCustoManutencao, ConstantsValues.EscalaCustoManutencao, true)
                .When(x => x.Custo.HasValue)
                .WithMessage(string.Format(ConstantsMessage.ErroManutencaoCustoPrecisao, ConstantsValues.PrecisaoCustoManutencao - ConstantsValues.EscalaCustoManutencao, ConstantsValues.EscalaCustoManutencao));
        }
    }
}
