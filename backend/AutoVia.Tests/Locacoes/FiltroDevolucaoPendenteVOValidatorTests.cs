using AutoVia.Communication.Validators.Locacoes;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Linq;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Validador do filtro da lista de devolucoes pendentes: so EM_ANDAMENTO, ATRASADA ou vazio passam.</summary>
    public class FiltroDevolucaoPendenteVOValidatorTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("EM_ANDAMENTO")]
        [InlineData("ATRASADA")]
        public void StatusPermitidoOuVazio_PassaNaValidacao(string? status)
        {
            FiltroDevolucaoPendenteVO filtro = new FiltroDevolucaoPendenteVO { Status = status };

            ValidationResult resultado = new FiltroDevolucaoPendenteVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("FINALIZADA")]
        [InlineData("CANCELADA")]
        [InlineData("em_andamento")]
        [InlineData("atrasada")]
        [InlineData("ATRASADA ")]
        [InlineData("texto livre")]
        public void StatusForaDaLista_DaErro400ComAMensagemDoFiltro(string status)
        {
            FiltroDevolucaoPendenteVO filtro = new FiltroDevolucaoPendenteVO { Status = status };

            ValidationResult resultado = new FiltroDevolucaoPendenteVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroFiltroDevolucaoPendenteInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void PaginaInvalida_TambemDaErro()
        {
            FiltroDevolucaoPendenteVO filtro = new FiltroDevolucaoPendenteVO { Pagina = 0, Status = "ATRASADA" };

            ValidationResult resultado = new FiltroDevolucaoPendenteVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
        }
    }
}
