using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation;
using System;

namespace AutoVia.Communication.Validators.Autenticacao
{
    /// <summary>
    /// Validador FluentValidation do pedido de recuperacao de senha. Reusa as mensagens do login:
    /// perfil CLIENTE ou FUNCIONARIO; CPF com 11 digitos apos remover a mascara.
    /// </summary>
    public class SolicitarRecuperacaoSenhaVOValidator : AbstractValidator<SolicitarRecuperacaoSenhaVO>
    {
        public SolicitarRecuperacaoSenhaVOValidator()
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
        }
    }
}
