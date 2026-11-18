using AutoVia.Api.Requests.Armazenamento;
using AutoVia.Helpers;
using FluentValidation;
using System.Linq;

namespace AutoVia.Api.Validators
{
    /// <summary>
    /// Formato do link de arquivo da rota publica (chave, exp e sig). Toda falha de formato e 400 com a mesma mensagem.
    /// A chave insegura NAO e erro de formato: quem recusa e o servico, com 404. O padrao do exp e usado sem opcoes de
    /// regex (ancorado por \A e \z), e a assinatura e conferida caractere a caractere, porque em .NET o $ casa antes de
    /// uma quebra de linha final.
    /// </summary>
    public sealed class LerArquivoArmazenadoRequestValidator : AbstractValidator<LerArquivoArmazenadoRequest>
    {
        public LerArquivoArmazenadoRequestValidator()
        {
            RuleFor(x => x.QuantidadeChave)
                .Equal(ConstantsValues.QuantidadeEsperadaDeCadaParametroLink).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);

            RuleFor(x => x.QuantidadeExp)
                .Equal(ConstantsValues.QuantidadeEsperadaDeCadaParametroLink).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);

            RuleFor(x => x.QuantidadeSig)
                .Equal(ConstantsValues.QuantidadeEsperadaDeCadaParametroLink).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);

            RuleFor(x => x.Chave)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido)
                .Length(ConstantsValues.TamanhoMinimoChaveArmazenamento, ConstantsValues.TamanhoMaximoChaveArmazenamento).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);

            RuleFor(x => x.Exp)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido)
                .Matches(ConstantsValues.PadraoExpiracaoLink).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);

            RuleFor(x => x.Sig)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido)
                .Length(ConstantsValues.TamanhoAssinaturaLinkCaracteres, ConstantsValues.TamanhoAssinaturaLinkCaracteres).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido)
                .Must(EhAlfabetoBase64Url).WithMessage(ConstantsMessage.ErroArmazenamentoLinkInvalido);
        }

        /// <summary>Confere cada caractere da assinatura contra o alfabeto base64url (sem expressao regular).</summary>
        private static bool EhAlfabetoBase64Url(string? valor)
        {
            return valor != null && valor.All(caractere => ConstantsValues.AlfabetoBase64Url.IndexOf(caractere) >= 0);
        }
    }
}
