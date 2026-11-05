using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Autenticacao
{
    /// <summary>
    /// Validador FluentValidation do login. Perfil precisa ser CLIENTE ou FUNCIONARIO;
    /// o CPF, depois de remover mascara, tem 11 digitos; a senha tem de 1 a 128
    /// caracteres. Mensagens vem de ConstantsMessage.
    /// </summary>
    public class LoginVOValidator : AbstractValidator<LoginVO>
    {
        public LoginVOValidator()
        {
            RuleFor(x => x.Perfil)
                .Must(perfil => perfil.HasValue && Enum.IsDefined<PerfilUsuario>(perfil.Value))
                .WithMessage(ConstantsMessage.ErroLoginPerfilInvalido);

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
