using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do AlterarStatusVeiculoVOValidator: obrigatorio (nulo, vazio, so espacos), nome exato depois do Trim (numero,
    /// minusculas, texto estranho e caractere nulo recusados) e os seis nomes aceitos. A mensagem de status invalido lista os seis.
    /// </summary>
    public class AlterarStatusVeiculoVOValidatorTests
    {
        private const string SeisNomes = "DISPONIVEL, RESERVADO, LOCADO, MANUTENCAO, VENDIDO, BAIXADO";

        private static ValidationResult Validar(string? status)
        {
            AlterarStatusVeiculoVOValidator validador = new AlterarStatusVeiculoVOValidator();
            AlterarStatusVeiculoVO vo = new AlterarStatusVeiculoVO { Status = status };

            return validador.Validate(vo);
        }

        private static List<string> Mensagens(ValidationResult resultado)
        {
            return resultado.Errors.Select(erro => erro.ErrorMessage).ToList();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AusenteVazioOuSoEspacos_PedeOStatusObrigatorio(string? status)
        {
            ValidationResult resultado = Validar(status);

            Assert.False(resultado.IsValid);
            Assert.Equal(new List<string> { ConstantsMessage.ErroVeiculoStatusObrigatorio }, Mensagens(resultado));
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("manutencao")]
        [InlineData("3")]
        [InlineData("MANUTENCAO\0")]
        public void TextoQueNaoEUmNomeExato_RecusaComOsSeisNomes(string status)
        {
            ValidationResult resultado = Validar(status);

            Assert.False(resultado.IsValid);
            Assert.Equal(new List<string> { string.Format(ConstantsMessage.ErroVeiculoStatusInvalido, SeisNomes) }, Mensagens(resultado));
        }

        [Theory]
        [InlineData("DISPONIVEL")]
        [InlineData("RESERVADO")]
        [InlineData("LOCADO")]
        [InlineData("MANUTENCAO")]
        [InlineData("VENDIDO")]
        [InlineData("BAIXADO")]
        public void SeisNomesAceitos_PassamNoValidador(string status)
        {
            ValidationResult resultado = Validar(status);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorComEspacosNasPontas_EAceitoPeloTrim()
        {
            ValidationResult resultado = Validar("  BAIXADO  ");

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void SeisNomesDoEnum_SaoOsMesmosDaMensagem()
        {
            Assert.Equal(SeisNomes, string.Join(", ", Enum.GetNames<StatusVeiculo>()));
        }
    }
}
