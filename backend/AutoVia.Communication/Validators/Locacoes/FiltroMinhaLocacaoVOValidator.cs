using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.Locacoes
{
    /// <summary>Valida a situacao (EM_ANDAMENTO ou HISTORICO; ausente ou vazia vale em andamento) e a paginacao da lista do cliente.</summary>
    public class FiltroMinhaLocacaoVOValidator : AbstractValidator<FiltroMinhaLocacaoVO>
    {
        public FiltroMinhaLocacaoVOValidator()
        {
            // Pagina e tamanho (inclusive o estouro de deslocamento) vem do FiltroPaginacaoVOValidator; nao repetir aqui.
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Situacao)
                .Must(situacao => string.IsNullOrEmpty(situacao)
                    || situacao == ConstantsValues.SituacaoMinhasLocacoesEmAndamento
                    || situacao == ConstantsValues.SituacaoMinhasLocacoesHistorico)
                .WithMessage(ConstantsMessage.ErroSituacaoLocacaoInvalida);
        }
    }
}
