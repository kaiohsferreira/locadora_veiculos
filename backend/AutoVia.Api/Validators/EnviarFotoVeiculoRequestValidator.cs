using AutoVia.Api.Requests.VeiculoFoto;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation;

namespace AutoVia.Api.Validators
{
    /// <summary>
    /// Validacao do arquivo do envio de foto do veiculo: ausente ou vazio, tamanho, tipo declarado e assinatura dos primeiros bytes.
    /// Uma mensagem por falha (cascata com parada na primeira). A assinatura reaproveita CriarAvariaRequestValidator.ValidarFotoAsync.
    /// </summary>
    public sealed class EnviarFotoVeiculoRequestValidator : AbstractValidator<EnviarFotoVeiculoRequest>
    {
        public EnviarFotoVeiculoRequestValidator()
        {
            RuleFor(x => x.Foto)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ConstantsMessage.ErroVeiculoFotoObrigatoria)
                .Must(foto => foto != null && foto.Length > 0).WithMessage(ConstantsMessage.ErroVeiculoFotoObrigatoria)
                .Must(foto => foto != null && foto.Length <= ConstantsArmazenamento.TamanhoMaximoFotoBytes).WithMessage(ConstantsMessage.ErroVeiculoFotoTamanho)
                .MustAsync(CriarAvariaRequestValidator.ValidarFotoAsync).WithMessage(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }
    }
}
