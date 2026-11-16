using AutoVia.Api.Requests.Avaria;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Api.Validators
{
    /// <summary>Validacao do arquivo do PUT de reenvio da foto: as mesmas regras de arquivo do POST (CriarAvariaRequestValidator.ValidarFotoAsync).</summary>
    public sealed class RetryAvariaFotoRequestValidator : AbstractValidator<RetryAvariaFotoRequest>
    {
        public RetryAvariaFotoRequestValidator()
        {
            RuleFor(x => x.Foto).NotNull().WithMessage(ConstantsMessage.ErroAvariaFotoObrigatoria)
                .MustAsync(CriarAvariaRequestValidator.ValidarFotoAsync).WithMessage(ConstantsMessage.ErroAvariaFotoFormatoInvalido);
        }
    }
}
