using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;

namespace AutoVia.Communication.Validators.Clientes
{
    /// <summary>
    /// Validador FluentValidation da verificacao de cadastro. Roda antes da action: valor
    /// malformado devolve 400 e nunca chega ao banco. Campo ausente ou fora do enum recebe
    /// ErroClienteVerificacaoCampoInvalido. O teto de 200 caracteres vale para o valor cru.
    /// CPF e CNH aceitam mascara (contam so os digitos). O e-mail e validado depois de aparado,
    /// como o servico grava. As mensagens vem de ConstantsMessage e os limites de ConstantsValues.
    /// </summary>
    public class VerificarCadastroVOValidator : AbstractValidator<VerificarCadastroVO>
    {
        public VerificarCadastroVOValidator()
        {
            RuleFor(x => x.Campo)
                .Must(campo => campo.HasValue && Enum.IsDefined<CampoVerificacaoCadastro>(campo.Value))
                .WithMessage(ConstantsMessage.ErroClienteVerificacaoCampoInvalido);

            RuleFor(x => x.Valor)
                .Must(valor => TamanhoCruAceito(valor))
                .WithMessage(ConstantsMessage.ErroClienteVerificacaoValorInvalido);

            When(x => x.Campo == CampoVerificacaoCadastro.CPF, () =>
            {
                RuleFor(x => x.Valor)
                    .Cascade(CascadeMode.Stop)
                    .Must(valor => !string.IsNullOrWhiteSpace(valor))
                    .WithMessage(ConstantsMessage.ErroClienteCpfObrigatorio)
                    .Must(valor => StaticMethods.IsCpfValido(valor))
                    .WithMessage(ConstantsMessage.ErroClienteCpfInvalido);
            });

            When(x => x.Campo == CampoVerificacaoCadastro.CNH, () =>
            {
                RuleFor(x => x.Valor)
                    .Cascade(CascadeMode.Stop)
                    .Must(valor => !string.IsNullOrWhiteSpace(valor))
                    .WithMessage(ConstantsMessage.ErroClienteCnhNumeroObrigatorio)
                    .Must(valor => StaticMethods.OnlyDigits(valor).Length == ConstantsValues.TamanhoCnhNumero)
                    .WithMessage(string.Format(ConstantsMessage.ErroClienteCnhNumeroInvalido, ConstantsValues.TamanhoCnhNumero));
            });

            When(x => x.Campo == CampoVerificacaoCadastro.EMAIL, () =>
            {
                RuleFor(x => x.Valor)
                    .Cascade(CascadeMode.Stop)
                    .Must(valor => !string.IsNullOrWhiteSpace(valor))
                    .WithMessage(ConstantsMessage.ErroClienteEmailObrigatorio)
                    .Must(valor => TamanhoAteAposTrim(valor, ConstantsValues.TamanhoMaximoEmailCliente))
                    .WithMessage(string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloEmailCliente, ConstantsValues.TamanhoMaximoEmailCliente));

                // Formato do e-mail: medido depois de aparado, como o servico grava. So roda com valor preenchido.
                RuleFor(x => x.Valor)
                    .Must(valor => EmailValidoAposTrim(valor))
                    .WithMessage(ConstantsMessage.ErroClienteEmailInvalido)
                    .When(x => !string.IsNullOrWhiteSpace(x.Valor));
            });
        }

        /// <summary>Validador de e-mail do proprio FluentValidation (mesma regra EmailAddress do ClienteVOValidator), aplicado ao texto ja aparado.</summary>
        private static readonly InlineValidator<string> ValidadorEmail = CriarValidadorEmail();

        /// <summary>Monta o validador de e-mail uma vez: a regra EmailAddress do FluentValidation sobre o proprio texto.</summary>
        private static InlineValidator<string> CriarValidadorEmail()
        {
            InlineValidator<string> validador = new InlineValidator<string>();
            validador.RuleFor(texto => texto).EmailAddress();

            return validador;
        }

        /// <summary>E-mail valido depois de remover as bordas (nulo ou so espacos nao chega aqui: a regra de obrigatorio para antes).</summary>
        private static bool EmailValidoAposTrim(string? valor)
        {
            string aparado = valor == null ? string.Empty : valor.Trim();
            ValidationResult resultado = ValidadorEmail.Validate(aparado);

            return resultado.IsValid;
        }

        /// <summary>Teto do valor cru (antes de qualquer normalizacao): nulo passa para a regra de obrigatorio; senao no maximo 200 caracteres.</summary>
        private static bool TamanhoCruAceito(string? valor)
        {
            if (valor == null)
            {
                return true;
            }

            return valor.Length <= ConstantsValues.TamanhoMaximoValorVerificacaoCadastro;
        }

        /// <summary>Texto medido sem as bordas: o tamanho do Trim nao pode passar do maximo.</summary>
        private static bool TamanhoAteAposTrim(string? valor, int tamanhoMaximo)
        {
            if (valor == null)
            {
                return true;
            }

            return valor.Trim().Length <= tamanhoMaximo;
        }
    }
}
