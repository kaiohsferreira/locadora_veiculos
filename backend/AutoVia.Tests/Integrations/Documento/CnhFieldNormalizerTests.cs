using AutoVia.Communication.Enums;
using AutoVia.Integrations.Helpers.Documento;

namespace AutoVia.Tests.Integrations.Documento
{
    public class CnhFieldNormalizerTests
    {
        [Theory]
        [InlineData("123.456.789-01", "12345678901")]
        [InlineData(" 123 456 789 01 ", "12345678901")]
        [InlineData("1234567890", "1234567890")]
        public void NormalizarNumero_MantemSoDigitos(string entrada, string esperado)
        {
            string? resultado = CnhFieldNormalizer.NormalizarNumero(entrada);

            Assert.Equal(esperado, resultado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abc-.")]
        public void NormalizarNumero_SemDigitos_DevolveNull(string? entrada)
        {
            string? resultado = CnhFieldNormalizer.NormalizarNumero(entrada);

            Assert.Null(resultado);
        }

        [Theory]
        [InlineData("ab", CategoriaCnh.AB)]
        [InlineData(" B ", CategoriaCnh.B)]
        [InlineData("a", CategoriaCnh.A)]
        [InlineData("e", CategoriaCnh.E)]
        public void NormalizarCategoria_CategoriaValida_DevolveEnum(string entrada, CategoriaCnh esperado)
        {
            CategoriaCnh? resultado = CnhFieldNormalizer.NormalizarCategoria(entrada);

            Assert.Equal<CategoriaCnh?>(esperado, resultado);
        }

        [Theory]
        [InlineData("ACC")]
        [InlineData("1")]
        [InlineData("F")]
        [InlineData("")]
        [InlineData(null)]
        public void NormalizarCategoria_ForaDasCategorias_DevolveNull(string? entrada)
        {
            CategoriaCnh? resultado = CnhFieldNormalizer.NormalizarCategoria(entrada);

            Assert.Null(resultado);
        }

        [Theory]
        [InlineData("31/12/2030", 2030, 12, 31)]
        [InlineData("2030-12-31", 2030, 12, 31)]
        [InlineData("31/12/29", 2029, 12, 31)]
        public void NormalizarValidade_TresFormatos_DevolveData(string entrada, int ano, int mes, int dia)
        {
            DateOnly? resultado = CnhFieldNormalizer.NormalizarValidade(entrada);

            Assert.Equal<DateOnly?>(new DateOnly(ano, mes, dia), resultado);
        }

        [Theory]
        [InlineData("31/02/2030")]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        public void NormalizarValidade_InvalidaOuVazia_DevolveNull(string? entrada)
        {
            DateOnly? resultado = CnhFieldNormalizer.NormalizarValidade(entrada);

            Assert.Null(resultado);
        }
    }
}
