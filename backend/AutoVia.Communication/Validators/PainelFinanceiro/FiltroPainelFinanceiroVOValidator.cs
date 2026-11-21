using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using FluentValidation;

namespace AutoVia.Communication.Validators.PainelFinanceiro
{
    /// <summary>
    /// Validador do filtro dos indicadores do painel financeiro: o periodo precisa vir completo ou vazio, na ordem certa e
    /// dentro do limite de dias (RegrasPeriodoFinanceiro). Erro vira 400 antes da action, com a mensagem no detail.
    /// </summary>
    public class FiltroPainelFinanceiroVOValidator : AbstractValidator<FiltroPainelFinanceiroVO>
    {
        public FiltroPainelFinanceiroVOValidator()
        {
            RuleFor(x => x).Custom((filtro, contexto) =>
            {
                string? erro = RegrasPeriodoFinanceiro.ErroDoPeriodo(filtro.De, filtro.Ate);

                if (erro != null)
                {
                    contexto.AddFailure(nameof(FiltroPainelFinanceiroVO.De), erro);
                }
            });
        }
    }
}
