using AutoVia.Communication.Validators.Vendas;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Teto de pagina do catalogo de seminovos (T9; catalogo-venda.contract.json, C4): o FiltroCatalogoVOValidator herda a
    /// regra do validador base pelo Include. Pagina 2147483647 com tamanhoPagina 100 da 400 com a mensagem de deslocamento.
    /// </summary>
    public class CatalogoPaginaValidatorTests
    {
        private const int TamanhoPaginaDoTeste = 100;
        private const int PaginaNoLimiteComTamanhoCem = 21474837;
        private const int PaginaAcimaDoLimiteComTamanhoCem = 21474838;

        [Fact]
        public void Catalogo_PaginaDoTamanhoMaximoDoInt_DaErroDeDeslocamento()
        {
            FiltroCatalogoVOValidator validador = new FiltroCatalogoVOValidator();
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { Pagina = int.MaxValue, TamanhoPagina = TamanhoPaginaDoTeste };

            ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }

        [Fact]
        public void Catalogo_NoLimiteExatoPassa_UmAcimaReprova()
        {
            FiltroCatalogoVOValidator validador = new FiltroCatalogoVOValidator();

            ValidationResult noLimite = validador.Validate(new FiltroCatalogoVO { Pagina = PaginaNoLimiteComTamanhoCem, TamanhoPagina = TamanhoPaginaDoTeste });
            ValidationResult acimaDoLimite = validador.Validate(new FiltroCatalogoVO { Pagina = PaginaAcimaDoLimiteComTamanhoCem, TamanhoPagina = TamanhoPaginaDoTeste });

            Assert.DoesNotContain(noLimite.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
            Assert.Contains(acimaDoLimite.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }
    }
}
