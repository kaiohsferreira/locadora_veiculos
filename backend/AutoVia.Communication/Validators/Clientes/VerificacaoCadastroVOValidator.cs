using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Clientes
{
    /// <summary>
    /// Validador FluentValidation da verificacao do cadastro pela locadora. Roda antes da action (400 com ProblemDetails).
    /// A decisao e obrigatoria e so pode ser APROVADO ou RECUSADO (PENDENTE nao e decisao). A recusa exige justificativa
    /// com texto. A justificativa tem teto de 500 caracteres medidos depois do Trim, como o servico grava. As mensagens
    /// vem de ConstantsMessage e o limite de ConstantsValues.
    /// </summary>
    public class VerificacaoCadastroVOValidator : AbstractValidator<VerificacaoCadastroVO>
    {
        public VerificacaoCadastroVOValidator()
        {
            RuleFor(x => x.Decisao)
                .Must(decisao => DecisaoValida(decisao))
                .WithMessage(ConstantsMessage.ErroVerificacaoDecisaoObrigatoria);

            When(x => x.Decisao == StatusCadastroCliente.RECUSADO, () =>
            {
                RuleFor(x => x.Justificativa)
                    .Cascade(CascadeMode.Stop)
                    .Must(justificativa => !string.IsNullOrWhiteSpace(justificativa))
                    .WithMessage(ConstantsMessage.ErroVerificacaoJustificativaObrigatoria);
            });

            RuleFor(x => x.Justificativa)
                .Must(justificativa => TamanhoAteAposTrimAceito(justificativa))
                .WithMessage(string.Format(ConstantsMessage.ErroVerificacaoJustificativaTamanho, ConstantsValues.TamanhoMaximoJustificativaRecusa));

            // Caractere NUL: o PostgreSQL recusa o texto e o erro sairia como 500. Recusa aqui, com 400.
            RuleFor(x => x.Justificativa)
                .Must(justificativa => SemCaractereNul(justificativa))
                .WithMessage(ConstantsMessage.ErroVerificacaoJustificativaInvalida);
        }

        /// <summary>Justificativa ausente passa; presente, nao pode conter o caractere NUL (\u0000).</summary>
        private static bool SemCaractereNul(string? justificativa)
        {
            if (justificativa == null)
            {
                return true;
            }

            return justificativa.IndexOf('\0') < 0;
        }

        /// <summary>Decisao presente e restrita a APROVADO ou RECUSADO.</summary>
        private static bool DecisaoValida(StatusCadastroCliente? decisao)
        {
            if (!decisao.HasValue)
            {
                return false;
            }

            return decisao.Value == StatusCadastroCliente.APROVADO || decisao.Value == StatusCadastroCliente.RECUSADO;
        }

        /// <summary>Justificativa ausente passa (a regra de obrigatoriedade trata a recusa); presente, medida sem as bordas.</summary>
        private static bool TamanhoAteAposTrimAceito(string? justificativa)
        {
            if (justificativa == null)
            {
                return true;
            }

            return justificativa.Trim().Length <= ConstantsValues.TamanhoMaximoJustificativaRecusa;
        }
    }
}
