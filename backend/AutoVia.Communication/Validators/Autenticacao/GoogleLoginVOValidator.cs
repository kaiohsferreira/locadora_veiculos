using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Autenticacao
{
    /// <summary>
    /// Validador FluentValidation do login com Google: a credencial e obrigatoria e tem no maximo
    /// 4096 caracteres. Mensagens vem de ConstantsMessage. O lembrar nao tem regra.
    /// </summary>
    public class GoogleLoginVOValidator : AbstractValidator<GoogleLoginVO>
    {
        public GoogleLoginVOValidator()
        {
            RuleFor(x => x.Credencial)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroGoogleCredencialObrigatoria);

            RuleFor(x => x.Credencial)
                .MaximumLength(ConstantsValues.TamanhoMaximoCredencialGoogle)
                .WithMessage(ConstantsMessage.ErroGoogleCredencialTamanho);
        }
    }
}
