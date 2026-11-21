using AutoVia.Communication.Regras;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using AutoVia.Communication.Validators.PainelFinanceiro;
using AutoVia.Helpers;
using System;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Periodo e aba do painel financeiro (P7-BE-05): as regras puras de periodo (incompleto, intervalo invertido e limite de
    /// 366 dias, pela diferenca em dias) e os validadores FluentValidation que devolvem 400 com a mensagem aprovada.
    /// </summary>
    public class PainelFinanceiroValidadoresTests
    {
        private static readonly DateOnly Inicio = new DateOnly(2026, 1, 1);

        [Fact]
        public void Periodo_SoDeOuSoAte_E_Incompleto()
        {
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIncompleto, RegrasPeriodoFinanceiro.ErroDoPeriodo(Inicio, null));
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIncompleto, RegrasPeriodoFinanceiro.ErroDoPeriodo(null, Inicio));
        }

        [Fact]
        public void Periodo_AusenteOuCompleto_Valido()
        {
            Assert.Null(RegrasPeriodoFinanceiro.ErroDoPeriodo(null, null));
            Assert.Null(RegrasPeriodoFinanceiro.ErroDoPeriodo(Inicio, Inicio));
        }

        [Fact]
        public void Periodo_DeMaiorQueAte_E_Intervalo()
        {
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIntervalo, RegrasPeriodoFinanceiro.ErroDoPeriodo(Inicio.AddDays(1), Inicio));
        }

        [Fact]
        public void Periodo_Com366DiasDeDiferencaVale_E_Com367Nao()
        {
            DateOnly limite = Inicio.AddDays(ConstantsValues.DiasLimitePeriodoFinanceiro);
            DateOnly passou = limite.AddDays(1);

            Assert.Null(RegrasPeriodoFinanceiro.ErroDoPeriodo(Inicio, limite));
            Assert.Equal(
                string.Format(ConstantsMessage.ErroPeriodoFinanceiroLimite, ConstantsValues.DiasLimitePeriodoFinanceiro),
                RegrasPeriodoFinanceiro.ErroDoPeriodo(Inicio, passou));
        }

        [Fact]
        public void ValidadorDoIndicador_PeriodoIncompletoDevolveAMensagemNoCampoDe()
        {
            FiltroPainelFinanceiroVOValidator validador = new FiltroPainelFinanceiroVOValidator();
            FiltroPainelFinanceiroVO filtro = new FiltroPainelFinanceiroVO { De = Inicio };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIncompleto, resultado.Errors[0].ErrorMessage);
            Assert.Equal(nameof(FiltroPainelFinanceiroVO.De), resultado.Errors[0].PropertyName);
        }

        [Fact]
        public void ValidadorDoIndicador_SemDatasEValido()
        {
            FiltroPainelFinanceiroVOValidator validador = new FiltroPainelFinanceiroVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroPainelFinanceiroVO());

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(ConstantsValues.AbaTitulosTodos)]
        [InlineData(ConstantsValues.AbaTitulosEmAtraso)]
        [InlineData(ConstantsValues.AbaTitulosVenceHoje)]
        [InlineData(ConstantsValues.AbaTitulosPagosRecentes)]
        public void ValidadorDosTitulos_AbasConhecidasSaoValidas(string aba)
        {
            FiltroTitulosFinanceirosVOValidator validador = new FiltroTitulosFinanceirosVOValidator();
            FiltroTitulosFinanceirosVO filtro = new FiltroTitulosFinanceirosVO { Aba = aba };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("CANCELADO")]
        [InlineData("todos")]
        [InlineData("")]
        public void ValidadorDosTitulos_AbaDesconhecidaDevolveAMensagemDeAba(string aba)
        {
            FiltroTitulosFinanceirosVOValidator validador = new FiltroTitulosFinanceirosVOValidator();
            FiltroTitulosFinanceirosVO filtro = new FiltroTitulosFinanceirosVO { Aba = aba };

            FluentValidation.Results.ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroAbaTitulosFinanceirosInvalida);
        }

        [Fact]
        public void ValidadorDosTitulos_PeriodoIncompletoOuInvertidoEInvalido()
        {
            FiltroTitulosFinanceirosVOValidator validador = new FiltroTitulosFinanceirosVOValidator();

            FluentValidation.Results.ValidationResult soDe = validador.Validate(new FiltroTitulosFinanceirosVO { De = Inicio });
            FluentValidation.Results.ValidationResult invertido = validador.Validate(new FiltroTitulosFinanceirosVO { De = Inicio.AddDays(2), Ate = Inicio });

            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIncompleto, soDe.Errors[0].ErrorMessage);
            Assert.Equal(ConstantsMessage.ErroPeriodoFinanceiroIntervalo, invertido.Errors[0].ErrorMessage);
        }

        [Fact]
        public void ValidadorDosTitulos_PaginaZeroEInvalida()
        {
            FiltroTitulosFinanceirosVOValidator validador = new FiltroTitulosFinanceirosVOValidator();

            FluentValidation.Results.ValidationResult resultado = validador.Validate(new FiltroTitulosFinanceirosVO { Pagina = 0 });

            Assert.False(resultado.IsValid);
        }
    }
}
