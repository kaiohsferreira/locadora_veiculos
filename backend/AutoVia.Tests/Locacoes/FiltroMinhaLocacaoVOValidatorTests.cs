using AutoVia.Communication.Validators.Locacoes;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Validador do filtro da lista do cliente: situacao (ausente, em andamento, historico) e paginacao.</summary>
    public class FiltroMinhaLocacaoVOValidatorTests
    {
        private static ValidationResult Validar(string? situacao, int pagina = 1, int tamanhoPagina = 20)
        {
            FiltroMinhaLocacaoVO filtro = new FiltroMinhaLocacaoVO
            {
                Situacao = situacao,
                Pagina = pagina,
                TamanhoPagina = tamanhoPagina
            };

            return new FiltroMinhaLocacaoVOValidator().Validate(filtro);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("EM_ANDAMENTO")]
        [InlineData("HISTORICO")]
        public void SituacaoAceita_PassaNoValidador(string? situacao)
        {
            ValidationResult resultado = Validar(situacao);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("em_andamento")]
        [InlineData("historico")]
        [InlineData("FINALIZADA")]
        [InlineData("CANCELADA")]
        [InlineData("texto livre")]
        public void SituacaoFora_DaErroComAMensagemDaSituacao(string situacao)
        {
            ValidationResult resultado = Validar(situacao);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroSituacaoLocacaoInvalida);
        }

        [Fact]
        public void PaginaZero_DaErroDaPaginacaoExistente()
        {
            ValidationResult resultado = Validar(null, pagina: 0);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroPaginaInvalida);
        }

        [Fact]
        public void TamanhoAcimaDoLimite_DaErroDaPaginacaoExistente()
        {
            ValidationResult resultado = Validar(null, tamanhoPagina: ConstantsValues.TamanhoPaginaMaximo + 1);

            Assert.False(resultado.IsValid);
        }
    }
}
