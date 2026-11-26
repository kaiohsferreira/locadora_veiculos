using AutoVia.Communication.ViewObjects.Contratos;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Contratos
{
    /// <summary>
    /// Validacao de entrada do POST de assinatura do contrato (400 antes de qualquer trava). Nome, aceite das condicoes gerais e
    /// imagem seguem as regras de RegrasFormatoAssinaturaContrato. O aceite de combustivel nao e validado aqui: depende da clausula
    /// 5 do documento, e o servico confere (O-6).
    /// </summary>
    public sealed class AssinarContratoLocacaoVOValidator : AbstractValidator<AssinarContratoLocacaoVO>
    {
        public AssinarContratoLocacaoVOValidator()
        {
            RuleFor(x => x.NomeAssinante).Custom((nome, contexto) =>
            {
                string? erro = RegrasFormatoAssinaturaContrato.ErroNomeAssinante(nome);
                if (erro != null)
                {
                    contexto.AddFailure(erro);
                }
            });

            RuleFor(x => x.AceiteCondicoesGerais).Equal(true).WithMessage(ConstantsMessage.ErroContratoAceiteObrigatorio);

            RuleFor(x => x.AssinaturaPngBase64).Custom((base64, contexto) =>
            {
                string? erro = RegrasFormatoAssinaturaContrato.ErroAssinaturaPng(base64);
                if (erro != null)
                {
                    contexto.AddFailure(erro);
                }
            });
        }
    }
}
