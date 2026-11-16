using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Guarda de estouro de pagina no validador base de paginacao (veiculo.contract.json v3, R7; api-conventions
    /// pagination.guardaDeEstouro (v6)): pagina x tamanhoPagina nao pode estourar int. A regra fica so no validador base;
    /// a busca de disponiveis herda a regra pelo Include e nao tem mais copia propria.
    /// </summary>
    public class FiltroPaginacaoVOValidatorTests
    {
        private const int TamanhoPaginaMaximoDoTeste = 100;
        private const int PaginaNoLimiteComTamanhoCem = 21474837;
        private const int PaginaAcimaDoLimiteComTamanhoCem = 21474838;

        private static bool ContemErroDeDeslocamento(ValidationResult resultado)
        {
            return resultado.Errors.Any(erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }

        [Fact]
        public void ValidadorBase_PaginaQueEstouraInt_DaErroComAMensagemDeDeslocamento()
        {
            FiltroPaginacaoVOValidator validador = new FiltroPaginacaoVOValidator();
            FiltroPaginacaoVO filtro = new FiltroPaginacaoVO { Pagina = int.MaxValue, TamanhoPagina = TamanhoPaginaMaximoDoTeste };

            ValidationResult resultado = validador.Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.True(ContemErroDeDeslocamento(resultado));
        }

        [Fact]
        public void ValidadorBase_NoLimiteExatoPassa_UmAcimaReprova()
        {
            FiltroPaginacaoVOValidator validador = new FiltroPaginacaoVOValidator();

            ValidationResult noLimite = validador.Validate(new FiltroPaginacaoVO { Pagina = PaginaNoLimiteComTamanhoCem, TamanhoPagina = TamanhoPaginaMaximoDoTeste });
            ValidationResult acimaDoLimite = validador.Validate(new FiltroPaginacaoVO { Pagina = PaginaAcimaDoLimiteComTamanhoCem, TamanhoPagina = TamanhoPaginaMaximoDoTeste });

            Assert.False(ContemErroDeDeslocamento(noLimite));
            Assert.True(ContemErroDeDeslocamento(acimaDoLimite));
        }

        [Fact]
        public void ValidadorBase_TamanhoUmAceitaQualquerPaginaDentroDoInt()
        {
            FiltroPaginacaoVOValidator validador = new FiltroPaginacaoVOValidator();

            ValidationResult resultado = validador.Validate(new FiltroPaginacaoVO { Pagina = int.MaxValue, TamanhoPagina = 1 });

            Assert.False(ContemErroDeDeslocamento(resultado));
        }

        [Fact]
        public void ValidadorDaBusca_PaginaQueEstouraTemUmSoErroDeDeslocamento()
        {
            FiltroVeiculoDisponibilidadeVOValidator validador = new FiltroVeiculoDisponibilidadeVOValidator();
            FiltroVeiculoDisponibilidadeVO filtro = new FiltroVeiculoDisponibilidadeVO
            {
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 12),
                Pagina = int.MaxValue,
                TamanhoPagina = TamanhoPaginaMaximoDoTeste
            };

            ValidationResult resultado = validador.Validate(filtro);

            Assert.Single(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaDeslocamentoInvalido);
        }
    }
}
