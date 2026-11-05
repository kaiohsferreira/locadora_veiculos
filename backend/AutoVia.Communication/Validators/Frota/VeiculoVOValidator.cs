using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation;
using System.Text.RegularExpressions;

namespace AutoVia.Communication.Validators.Frota
{
    /// <summary>
    /// Validador FluentValidation do VO de entrada do veiculo (POST e PUT). Textos com
    /// espacos nas pontas sao medidos sem eles, como sao gravados. Os limites vem de
    /// ConstantsValues e as mensagens de ConstantsMessage. Os anos sao calculados
    /// com o ano atual do servidor.
    /// </summary>
    public class VeiculoVOValidator : AbstractValidator<VeiculoVO>
    {
        public VeiculoVOValidator()
        {
            int anoLimite = DateTime.Now.Year + ConstantsValues.AnosAdiantamentoVeiculo;

            RuleFor(x => x.IdCategoria)
                .GreaterThan(0)
                .WithMessage(ConstantsMessage.ErroVeiculoCategoriaObrigatoria);

            RuleFor(x => x.Placa)
                .Must(placa => StaticMethods.IsPlacaValida(placa))
                .WithMessage(ConstantsMessage.ErroVeiculoPlacaInvalida);

            RuleFor(x => x.Chassi)
                .Must(chassi => Regex.IsMatch((chassi ?? string.Empty).Trim().ToUpperInvariant(), ConstantsValues.PadraoChassiVeiculo))
                .WithMessage(ConstantsMessage.ErroVeiculoChassiInvalido);

            RuleFor(x => x.Renavam)
                .Must(renavam => Regex.IsMatch((renavam ?? string.Empty).Trim(), ConstantsValues.PadraoRenavamVeiculo))
                .WithMessage(ConstantsMessage.ErroVeiculoRenavamInvalido);

            RuleFor(x => x.Marca)
                .Must(marca => !string.IsNullOrWhiteSpace(marca))
                .WithMessage(ConstantsMessage.ErroVeiculoMarcaObrigatoria);

            RuleFor(x => x.Marca)
                .Must(marca => TamanhoSemEspacos(marca) <= ConstantsValues.TamanhoMaximoMarcaVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoMarcaTamanho, ConstantsValues.TamanhoMaximoMarcaVeiculo));

            RuleFor(x => x.Modelo)
                .Must(modelo => !string.IsNullOrWhiteSpace(modelo))
                .WithMessage(ConstantsMessage.ErroVeiculoModeloObrigatorio);

            RuleFor(x => x.Modelo)
                .Must(modelo => TamanhoSemEspacos(modelo) <= ConstantsValues.TamanhoMaximoModeloVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoModeloTamanho, ConstantsValues.TamanhoMaximoModeloVeiculo));

            RuleFor(x => x.AnoFabricacao)
                .Must(ano => ano >= ConstantsValues.AnoMinimoFabricacaoVeiculo && ano <= anoLimite)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoAnoFabricacaoIntervalo, ConstantsValues.AnoMinimoFabricacaoVeiculo, anoLimite));

            RuleFor(x => x.AnoModelo)
                .GreaterThanOrEqualTo(x => x.AnoFabricacao)
                .WithMessage(ConstantsMessage.ErroVeiculoAnoModeloAnterior);

            RuleFor(x => x.AnoModelo)
                .Must(ano => ano <= anoLimite)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoAnoModeloMaximo, anoLimite));

            RuleFor(x => x.Cor)
                .Must(cor => cor == null || TamanhoSemEspacos(cor) <= ConstantsValues.TamanhoMaximoCorVeiculo)
                .WithMessage(string.Format(ConstantsMessage.ErroVeiculoCorTamanho, ConstantsValues.TamanhoMaximoCorVeiculo));

            RuleFor(x => x.KmAtual)
                .GreaterThanOrEqualTo(0)
                .WithMessage(ConstantsMessage.ErroVeiculoKmNegativo);

            RuleFor(x => x.ValorDiaria)
                .GreaterThan(0m)
                .WithMessage(ConstantsMessage.ErroVeiculoValorDiaria);

            RuleFor(x => x.ValorDiaria)
                .PrecisionScale(10, 2, true)
                .WithMessage(ConstantsMessage.ErroVeiculoValorDiariaPrecisao);

            RuleFor(x => x.ValorVenda)
                .GreaterThan(0m)
                .When(x => x.ValorVenda.HasValue)
                .WithMessage(ConstantsMessage.ErroVeiculoValorVenda);

            RuleFor(x => x.ValorVenda)
                .PrecisionScale(12, 2, true)
                .When(x => x.ValorVenda.HasValue)
                .WithMessage(ConstantsMessage.ErroVeiculoValorVendaPrecisao);

            RuleFor(x => x.DataAquisicao)
                .Must(data => !data.HasValue || data.Value <= DateOnly.FromDateTime(DateTime.Now))
                .WithMessage(ConstantsMessage.ErroVeiculoDataAquisicaoFutura);
        }

        /// <summary>Quantidade de caracteres do texto depois de tirar os espacos das pontas; texto nulo conta como zero.</summary>
        private static int TamanhoSemEspacos(string? texto)
        {
            return (texto ?? string.Empty).Trim().Length;
        }
    }
}
