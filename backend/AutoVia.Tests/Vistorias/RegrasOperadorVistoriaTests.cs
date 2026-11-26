using AutoVia.Core.Regras;
using Xunit;

namespace AutoVia.Tests.Vistorias;

/// <summary>Nome abreviado do operador da vistoria (N10, item 6; vistoria-saida v4): primeiro nome e inicial do ultimo sobrenome.</summary>
public class RegrasOperadorVistoriaTests
{
    [Theory]
    [InlineData("Rogerio Vieira", "Rogerio V.")]
    [InlineData("Ana Maria Souza Lima", "Ana L.")]
    [InlineData("  Carlos   Pereira  ", "Carlos P.")]
    [InlineData("joao silva", "joao S.")]
    public void Abreviar_PrimeiroNomeEInicialDoUltimoSobrenome(string nome, string esperado)
    {
        string? abreviado = RegrasOperadorVistoria.Abreviar(nome);

        Assert.Equal(esperado, abreviado);
    }

    [Fact]
    public void Abreviar_UmaPalavraSoDevolveOPrimeiroNome()
    {
        string? abreviado = RegrasOperadorVistoria.Abreviar("Rogerio");

        Assert.Equal("Rogerio", abreviado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Abreviar_NomeVazioOuNuloDevolveNulo(string? nome)
    {
        string? abreviado = RegrasOperadorVistoria.Abreviar(nome);

        Assert.Null(abreviado);
    }
}
