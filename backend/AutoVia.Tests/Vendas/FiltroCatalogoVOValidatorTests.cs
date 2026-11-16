using AutoVia.Communication.Validators.Vendas;
using AutoVia.Communication.ViewObjects.Vendas;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Vendas
{
    /// <summary>
    /// Testes do FiltroCatalogoVOValidator: filtros opcionais, valores negativos, faixas
    /// invertidas (erro no campo do maximo), tamanho da marca e paginacao herdada.
    /// </summary>
    public class FiltroCatalogoVOValidatorTests
    {
        [Fact]
        public void FiltroVazio_PassaNaValidacao()
        {
            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(new FiltroCatalogoVO());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void FiltroCompletoValido_PassaNaValidacao()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { Marca = "Jeep", PrecoMinimo = 50000m, PrecoMaximo = 50000m, AnoMinimo = 2020, AnoMaximo = 2020, KmMaximo = 0 };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PrecoMinimoMaiorQueMaximo_FalhaNoCampoPrecoMaximo()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { PrecoMinimo = 90000m, PrecoMaximo = 50000m };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            ValidationFailure erro = Assert.Single(resultado.Errors);
            Assert.Equal(ConstantsMessage.ErroCatalogoFaixaPreco, erro.ErrorMessage);
            Assert.Equal(nameof(FiltroCatalogoVO.PrecoMaximo), erro.PropertyName);
        }

        [Fact]
        public void AnoMinimoMaiorQueMaximo_FalhaNoCampoAnoMaximo()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { AnoMinimo = 2024, AnoMaximo = 2020 };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            ValidationFailure erro = Assert.Single(resultado.Errors);
            Assert.Equal(ConstantsMessage.ErroCatalogoFaixaAno, erro.ErrorMessage);
            Assert.Equal(nameof(FiltroCatalogoVO.AnoMaximo), erro.PropertyName);
        }

        [Fact]
        public void PrecoNegativo_FalhaComMensagemDePrecoNegativo()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { PrecoMinimo = -1m };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.Contains(ConstantsMessage.ErroCatalogoPrecoNegativo, ObterMensagens(resultado));
        }

        [Fact]
        public void AnoZero_FalhaComMensagemDeAnoInvalido()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { AnoMinimo = 0 };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.Contains(ConstantsMessage.ErroCatalogoAnoInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void KmNegativo_FalhaComMensagemDeKm()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { KmMaximo = -10 };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.Contains(ConstantsMessage.ErroCatalogoKmNegativo, ObterMensagens(resultado));
        }

        [Fact]
        public void MarcaAcimaDoLimite_FalhaComMensagemDeTamanho()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { Marca = new string('m', ConstantsValues.TamanhoMaximoMarcaVeiculo + 1) };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.Contains(string.Format(ConstantsMessage.ErroCatalogoMarcaTamanho, ConstantsValues.TamanhoMaximoMarcaVeiculo), ObterMensagens(resultado));
        }

        [Fact]
        public void TamanhoPaginaAcimaDoMaximo_FalhaPelaPaginacao()
        {
            FiltroCatalogoVO filtro = new FiltroCatalogoVO { TamanhoPagina = 1000 };

            ValidationResult resultado = new FiltroCatalogoVOValidator().Validate(filtro);

            Assert.False(resultado.IsValid);
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();

            return mensagens;
        }
    }
}
