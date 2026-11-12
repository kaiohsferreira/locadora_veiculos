using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Autenticacao
{
    /// <summary>
    /// Validador FluentValidation do vinculo com Google: credencial obrigatoria (ate 4096); CPF com 11
    /// digitos apos remover a mascara; senha de 1 a 128 caracteres. Reusa as mensagens do login por CPF.
    /// </summary>
    public class GoogleVinculoVOValidator : AbstractValidator<GoogleVinculoVO>
    {
        public GoogleVinculoVOValidator()
        {
            RuleFor(x => x.Credencial)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroGoogleCredencialObrigatoria);

            RuleFor(x => x.Credencial)
                .MaximumLength(ConstantsValues.TamanhoMaximoCredencialGoogle)
                .WithMessage(ConstantsMessage.ErroGoogleCredencialTamanho);

            RuleFor(x => x.Cpf)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroLoginCpfObrigatorio);

            RuleFor(x => x.Cpf)
                .Must(cpf => StaticMethods.OnlyDigits(cpf).Length == ConstantsValues.TamanhoCpf)
                .WithMessage(ConstantsMessage.ErroLoginCpfFormato);

            RuleFor(x => x.Senha)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroLoginSenhaObrigatoria);

            RuleFor(x => x.Senha)
                .MaximumLength(ConstantsValues.TamanhoMaximoSenhaLogin)
                .WithMessage(string.Format(ConstantsMessage.ErroLoginSenhaTamanho, ConstantsValues.TamanhoMaximoSenhaLogin));
        }
    }
}
