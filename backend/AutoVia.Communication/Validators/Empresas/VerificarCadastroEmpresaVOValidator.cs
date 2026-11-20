using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Helpers;
using FluentValidation;
using FluentValidation.Results;
using System;

namespace AutoVia.Communication.Validators.Empresas
{
    /// <summary>
    /// Validador FluentValidation da verificacao publica de locadora. Roda antes da action: valor malformado
    /// devolve 400 e nunca chega ao banco. Campo ausente ou fora do enum recebe ErroLocadoraVerificacaoCampoInvalido.
    /// O teto de 200 caracteres vale para o valor cru. CNPJ aceita mascara (conta so os digitos, com digito
    /// verificador). E-mail e validado depois de aparado, como o servico grava.
    /// </summary>
    public class VerificarCadastroEmpresaVOValidator : AbstractValidator<VerificarCadastroEmpresaVO>
    {
        // Caractere NUL: no ramo de e-mail o valor vai para a pesquisa no banco; o PostgreSQL recusa o texto (erro 500). Recusa aqui, com 400.
        private const char CaractereNulo = '\0';
        private const string NomeCampoValor = nameof(VerificarCadastroEmpresaVO.Valor);

        public VerificarCadastroEmpresaVOValidator()
        {
            RuleFor(x => x.Campo)
                .Must(campo => campo.HasValue && Enum.IsDefined<CampoVerificacaoEmpresa>(campo.Value))
                .WithMessage(ConstantsMessage.ErroLocadoraVerificacaoCampoInvalido);

            RuleFor(x => x.Valor)
                .Must(valor => TamanhoCruAceito(valor))
                .WithMessage(ConstantsMessage.ErroLocadoraVerificacaoValorInvalido);

            RuleFor(x => x.Valor)
                .Must(valor => valor == null || !valor.Contains(CaractereNulo))
                .WithMessage(string.Format(ConstantsMessage.ErroCampoInvalido, NomeCampoValor));

            When(x => x.Campo == CampoVerificacaoEmpresa.CNPJ, () =>
            {
                RuleFor(x => x.Valor)
                    .Cascade(CascadeMode.Stop)
                    .Must(valor => !string.IsNullOrWhiteSpace(valor))
                    .WithMessage(ConstantsMessage.ErroLocadoraCnpjObrigatorio)
                    .Must(valor => StaticMethods.IsCnpjValido(valor))
                    .WithMessage(ConstantsMessage.ErroLocadoraCnpjInvalido);
            });

            When(x => x.Campo == CampoVerificacaoEmpresa.EMAIL, () =>
            {
                RuleFor(x => x.Valor)
                    .Cascade(CascadeMode.Stop)
                    .Must(valor => !string.IsNullOrWhiteSpace(valor))
                    .WithMessage(ConstantsMessage.ErroLocadoraEmailObrigatorio)
                    .Must(valor => TamanhoAteAposTrim(valor, ConstantsValues.TamanhoMaximoEmailLocadora))
                    .WithMessage(string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloEmailLocadora, ConstantsValues.TamanhoMaximoEmailLocadora));

                RuleFor(x => x.Valor)
                    .Must(valor => EmailValidoAposTrim(valor))
                    .WithMessage(ConstantsMessage.ErroLocadoraEmailInvalido)
                    .When(x => !string.IsNullOrWhiteSpace(x.Valor));
            });
        }

        /// <summary>Validador de e-mail do proprio FluentValidation, aplicado ao texto ja aparado.</summary>
        private static readonly InlineValidator<string> ValidadorEmail = CriarValidadorEmail();

        /// <summary>Monta o validador de e-mail uma vez, sobre o proprio texto.</summary>
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
