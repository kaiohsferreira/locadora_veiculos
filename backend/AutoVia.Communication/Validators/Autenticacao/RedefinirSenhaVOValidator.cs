using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation;
using System.Linq;

namespace AutoVia.Communication.Validators.Autenticacao
{
    /// <summary>
    /// Validador FluentValidation da redefinicao de senha. Token de 1 a 128 caracteres; nova senha
    /// com a mesma regra do cadastro do cliente (TamanhoMinimoSenhaCadastro a TamanhoMaximoSenhaCadastro,
    /// letra e numero); confirmacao igual a nova senha. Cada campo para na primeira regra que falha.
    /// </summary>
    public class RedefinirSenhaVOValidator : AbstractValidator<RedefinirSenhaVO>
    {
        public RedefinirSenhaVOValidator()
        {
            RuleFor(x => x.Token)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroRedefinicaoTokenObrigatorio)
                .MaximumLength(ConstantsValues.TamanhoMaximoToken)
                .WithMessage(ConstantsMessage.ErroRedefinicaoLinkInvalido);

            RuleFor(x => x.NovaSenha)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroRedefinicaoSenhaObrigatoria)
                .Must(senha => senha.Length >= ConstantsValues.TamanhoMinimoSenhaCadastro && senha.Length <= ConstantsValues.TamanhoMaximoSenhaCadastro)
                .WithMessage(string.Format(ConstantsMessage.ErroRedefinicaoSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro))
                .Must(senha => senha.Any(caractere => char.IsLetter(caractere)) && senha.Any(caractere => char.IsDigit(caractere)))
                .WithMessage(ConstantsMessage.ErroRedefinicaoSenhaFraca);

            RuleFor(x => x.ConfirmacaoSenha)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage(ConstantsMessage.ErroRedefinicaoConfirmacaoObrigatoria)
                .Must((vo, confirmacao) => confirmacao == vo.NovaSenha)
                .WithMessage(ConstantsMessage.ErroRedefinicaoConfirmacaoDiferente);
        }
    }
}
