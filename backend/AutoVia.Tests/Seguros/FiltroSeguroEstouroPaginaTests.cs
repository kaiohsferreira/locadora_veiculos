using AutoVia.Communication.Validators.Seguros;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Seguros
{
    /// <summary>
    /// Estouro de pagina na listagem de apolices (item 5 do plano de pendencias da branch_s2_kaio, aprovado em 20/11/2026).
    /// O deslocamento (Pagina - 1) * TamanhoPagina nao pode passar de int: com pagina no limite do inteiro e tamanho 100, e
    /// com tamanho no limite do inteiro, o validador de entrada recusa com a mensagem de deslocamento (400), antes de chegar
    /// ao servico. O validador e o mesmo que o filtro de acao usa para FiltroSeguroVO (FiltroSeguroVOValidator).
    /// </summary>
    public class FiltroSeguroEstouroPaginaTests
    {
        private const int PaginaNoLimiteDoInteiro = int.MaxValue;
        private const int TamanhoNoLimiteDoInteiro = int.MaxValue;
        private const int TamanhoDoTesteDeEstouro = ConstantsValues.TamanhoPaginaMaximo;
        private const int PaginaQueEstouraComTamanhoMaximo = 3;
        private const int PaginaMaximaQueCabeComTamanhoMaximo = (int.MaxValue / ConstantsValues.TamanhoPaginaMaximo) + 1;
        private const int TamanhoUm = 1;

        /// <summary>Pagina no limite do inteiro com tamanho 100 estoura o deslocamento: 400 com a mensagem de deslocamento.</summary>
        [Fact]
        public void PaginaNoLimiteDoInteiro_ComTamanhoCem_RecusaPelaGuardaDeDeslocamento()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { Pagina = PaginaNoLimiteDoInteiro, TamanhoPagina = TamanhoDoTesteDeEstouro };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }

        /// <summary>Tamanho no limite do inteiro (fora do maximo de 100) e recusado; com pagina 3 o deslocamento tambem estoura.</summary>
        [Fact]
        public void TamanhoNoLimiteDoInteiro_ComPaginaTres_RecusaComTamanhoEDeslocamento()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { Pagina = PaginaQueEstouraComTamanhoMaximo, TamanhoPagina = TamanhoNoLimiteDoInteiro };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == string.Format(ConstantsMessage.ErroTamanhoPaginaInvalido, ConstantsValues.TamanhoPaginaMaximo));
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }

        /// <summary>Limite exato que ainda cabe (pagina maxima com tamanho 100) passa: a guarda nao recusa pagina valida.</summary>
        [Fact]
        public void PaginaNoMaximoQueCabe_ComTamanhoCem_PassaSemErroDeDeslocamento()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { Pagina = PaginaMaximaQueCabeComTamanhoMaximo, TamanhoPagina = TamanhoDoTesteDeEstouro };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid, string.Join("; ", resultado.Errors.Select(erro => erro.ErrorMessage)));
        }

        /// <summary>Pagina no limite do inteiro com tamanho 1 cabe no int (deslocamento de int.MaxValue menos 1) e passa.</summary>
        [Fact]
        public void PaginaNoLimiteDoInteiro_ComTamanhoUm_PassaSemErroDeDeslocamento()
        {
            FiltroSeguroVO filtro = new FiltroSeguroVO { Pagina = PaginaNoLimiteDoInteiro, TamanhoPagina = TamanhoUm };

            ValidationResult resultado = new FiltroSeguroVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid, string.Join("; ", resultado.Errors.Select(erro => erro.ErrorMessage)));
        }
    }
}
