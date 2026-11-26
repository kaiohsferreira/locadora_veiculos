using AutoVia.Communication.Validators.Devolucao;
using AutoVia.Communication.ViewObjects.Devolucao;
using AutoVia.Helpers;
using FluentValidation.Results;
using Xunit;

namespace AutoVia.Tests.Devolucao;

/// <summary>Corpo do encerramento: politica e versao positivas, total com ate 2 casas e dentro do teto.</summary>
public class EncerrarLocacaoVOValidatorTests
{
    private static EncerrarLocacaoVO Valido(decimal total = 200m) => new EncerrarLocacaoVO { IdConfiguracao = 1, VersaoEsperada = 1, TotalConfirmado = total };

    [Fact]
    public void CorpoValido_PassaNaValidacao()
    {
        EncerrarLocacaoVOValidator validator = new EncerrarLocacaoVOValidator();

        ValidationResult resultado = validator.Validate(Valido());

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData(0, 1, 200)]
    [InlineData(1, 0, 200)]
    [InlineData(1, 1, -0.01)]
    [InlineData(1, 1, 10.123)]
    [InlineData(1, 1, 1000000000)]
    public void CorpoInvalido_DevolveAMensagemDeParametrosDoEncerramento(int configuracao, int versao, double total)
    {
        EncerrarLocacaoVOValidator validator = new EncerrarLocacaoVOValidator();
        EncerrarLocacaoVO modelo = new EncerrarLocacaoVO { IdConfiguracao = configuracao, VersaoEsperada = versao, TotalConfirmado = (decimal)total };

        ValidationResult resultado = validator.Validate(modelo);

        Assert.False(resultado.IsValid);
        Assert.All(resultado.Errors, erro => Assert.Equal(ConstantsMessage.ErroEncerramentoParametros, erro.ErrorMessage));
    }
}
