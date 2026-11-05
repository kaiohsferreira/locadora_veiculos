using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do CategoriaVOValidator: regras de nome, descricao e valor da diaria base.
    /// </summary>
    public class CategoriaVOValidatorTests
    {
        [Fact]
        public void VOValido_PassaNaValidacao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "Economico", Descricao = "Carros compactos", ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void NomeVazio_FalhaComMensagemObrigatorio()
        {
            CategoriaVO vo = new CategoriaVO { Nome = string.Empty, ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaNomeObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void NomeSoComEspacos_FalhaComMensagemObrigatorio()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "   ", ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaNomeObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void NomeComCinquentaCaracteres_PassaNaValidacao()
        {
            string nomeNoLimite = new string('a', ConstantsValues.TamanhoMaximoNomeCategoria);
            CategoriaVO vo = new CategoriaVO { Nome = nomeNoLimite, ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void NomeComCinquentaEUmCaracteres_FalhaComMensagemTamanho()
        {
            string nomeLongo = new string('a', ConstantsValues.TamanhoMaximoNomeCategoria + 1);
            CategoriaVO vo = new CategoriaVO { Nome = nomeLongo, ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroCategoriaNomeTamanho, ConstantsValues.TamanhoMaximoNomeCategoria);

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void DescricaoComDuzentosEUmCaracteres_FalhaComMensagemTamanho()
        {
            string descricaoLonga = new string('d', ConstantsValues.TamanhoMaximoDescricaoCategoria + 1);
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", Descricao = descricaoLonga, ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();
            string mensagemEsperada = string.Format(ConstantsMessage.ErroCategoriaDescricaoTamanho, ConstantsValues.TamanhoMaximoDescricaoCategoria);

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(mensagemEsperada, ObterMensagens(resultado));
        }

        [Fact]
        public void DescricaoNula_PassaNaValidacao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", Descricao = null, ValorDiariaBase = 100m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorDiariaBaseZero_FalhaComMensagemValor()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 0m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaValorDiaria, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorDiariaBaseNegativo_FalhaComMensagemValor()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = -10m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaValorDiaria, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorDiariaBaseComTresCasasDecimais_FalhaComMensagemPrecisao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 150.555m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaValorDiariaPrecisao, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorDiariaBaseComNoveDigitosInteiros_FalhaComMensagemPrecisao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 100000000m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroCategoriaValorDiariaPrecisao, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorDiariaBaseNoLimiteMaximo_PassaNaValidacao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 99999999.99m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorDiariaBaseComZerosADireita_PassaNaValidacao()
        {
            CategoriaVO vo = new CategoriaVO { Nome = "SUV", ValorDiariaBase = 150.500m };
            CategoriaVOValidator validator = new CategoriaVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();
            return mensagens;
        }
    }
}
