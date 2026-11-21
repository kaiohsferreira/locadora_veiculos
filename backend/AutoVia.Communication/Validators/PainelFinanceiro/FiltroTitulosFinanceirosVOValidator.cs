using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;

namespace AutoVia.Communication.Validators.PainelFinanceiro
{
    /// <summary>
    /// Validador do filtro da lista de titulos do painel financeiro: paginacao padrao do projeto, aba dentro das quatro
    /// aceitas (ConstantsValues.AbasTitulosFinanceiros) e o mesmo periodo dos indicadores (RegrasPeriodoFinanceiro).
    /// </summary>
    public class FiltroTitulosFinanceirosVOValidator : AbstractValidator<FiltroTitulosFinanceirosVO>
    {
        public FiltroTitulosFinanceirosVOValidator()
        {
            Include(new FiltroPaginacaoVOValidator());

            RuleFor(x => x.Aba)
                .Must(aba => System.Array.IndexOf(ConstantsValues.AbasTitulosFinanceiros, aba) >= 0)
                .WithMessage(ConstantsMessage.ErroAbaTitulosFinanceirosInvalida);

            RuleFor(x => x).Custom((filtro, contexto) =>
            {
                string? erro = RegrasPeriodoFinanceiro.ErroDoPeriodo(filtro.De, filtro.Ate);

                if (erro != null)
                {
                    contexto.AddFailure(nameof(FiltroTitulosFinanceirosVO.De), erro);
                }
            });
        }
    }
}
