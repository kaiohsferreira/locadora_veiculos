using AutoVia.Communication.Validators.Locacoes;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Linq;

namespace AutoVia.Tests.Locacoes
{
    public class LocacaoVOValidatorTests
    {
        private const string TokenValido = "FAKE-0123456789abcdef0123456789abcdef";
        private const string BandeiraValida = "visa";

        [Fact]
        public void IdReservaPositivo_PassaNaValidacao()
        {
            LocacaoVO vo = new LocacaoVO { IdReserva = 1 };
            LocacaoVOValidator validator = new LocacaoVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void IdReservaNaoPositivo_FalhaComMensagemObrigatoria(int idReserva)
        {
            LocacaoVO vo = new LocacaoVO { IdReserva = idReserva };
            LocacaoVOValidator validator = new LocacaoVOValidator();

            ValidationResult resultado = validator.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroLocacaoReservaObrigatoria, resultado.Errors.Select(x => x.ErrorMessage));
        }

        // --- Campos do cartao da caucao (contrato caucao-retirada v1.1, entrada_do_cartao) ---

        private static ValidationResult Validar(string? token, string? bandeira, long? emissor = null, string? dispositivo = null)
        {
            LocacaoVO vo = new LocacaoVO
            {
                IdReserva = 1,
                CaucaoTokenCartao = token,
                CaucaoMetodoPagamentoId = bandeira,
                CaucaoEmissorId = emissor,
                CaucaoIdentificadorDispositivo = dispositivo
            };

            return new LocacaoVOValidator().Validate(vo);
        }

        /// <summary>Menor valor enviado em que faz sentido conferir que a mensagem nao o repete (uma letra solta aparece em qualquer texto).</summary>
        private const int TamanhoMinimoParaConferirEco = 4;

        /// <summary>Falha do cartao: so ErroCaucaoCartaoInvalido, no campo informado, sem repetir o valor enviado.</summary>
        private static void AfirmarCartaoInvalidoNoCampo(ValidationResult resultado, string campo, string? valorEnviado)
        {
            Assert.False(resultado.IsValid);
            ValidationFailure falha = Assert.Single(resultado.Errors);
            Assert.Equal(campo, falha.PropertyName);
            Assert.Equal(ConstantsMessage.ErroCaucaoCartaoInvalido, falha.ErrorMessage);

            if (valorEnviado != null && valorEnviado.Trim().Length >= TamanhoMinimoParaConferirEco)
            {
                Assert.DoesNotContain(valorEnviado, falha.ErrorMessage);
            }
        }

        [Fact]
        public void CartaoCompletoValido_Passa()
        {
            ValidationResult resultado = Validar(TokenValido, BandeiraValida, 2147483647L, "dispositivo-01:abc.def");

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("FAKE-RECUSADO")]
        [InlineData("FAKE-ANALISE")]
        [InlineData("tok_cartao_9876543210")]
        [InlineData("12345678901234567890")]
        [InlineData("4111111111a11111")]
        public void TokenNoFormatoEQueNaoPareceCartao_Passa(string token)
        {
            ValidationResult resultado = Validar(token, BandeiraValida);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("abc1234")]
        [InlineData("token com espaco")]
        [InlineData("tokenção12345")]
        [InlineData("")]
        public void TokenForaDoFormato_400NoCampoDoToken(string token)
        {
            ValidationResult resultado = Validar(token, BandeiraValida);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoTokenCartao), token);
        }

        [Fact]
        public void TokenCom129Caracteres_400()
        {
            string token = new string('a', 129);

            ValidationResult resultado = Validar(token, BandeiraValida);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoTokenCartao), token);
        }

        [Fact]
        public void TokenCom128Caracteres_Passa()
        {
            ValidationResult resultado = Validar(new string('a', 128), BandeiraValida);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("123456789012")]
        [InlineData("4111111111111111")]
        [InlineData("1234567890123456789")]
        [InlineData("4111-1111-1111-1111")]
        [InlineData("4111_1111_1111_1111")]
        public void TokenQuePareceNumeroDeCartao_400NoCampoDoToken(string token)
        {
            ValidationResult resultado = Validar(token, BandeiraValida);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoTokenCartao), token);
        }

        [Theory]
        [InlineData("VISA")]
        [InlineData("v")]
        [InlineData("visa-credito")]
        public void BandeiraForaDoFormato_400NoCampoDaBandeira(string bandeira)
        {
            ValidationResult resultado = Validar(TokenValido, bandeira);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoMetodoPagamentoId), bandeira);
        }

        [Fact]
        public void BandeiraCom31Caracteres_400()
        {
            ValidationResult resultado = Validar(TokenValido, new string('a', 31));

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoMetodoPagamentoId), null);
        }

        [Fact]
        public void TokenSemBandeira_400NoCampoDaBandeira()
        {
            ValidationResult resultado = Validar(TokenValido, null);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoMetodoPagamentoId), null);
        }

        [Fact]
        public void BandeiraSemToken_400NoCampoDaBandeira()
        {
            ValidationResult resultado = Validar(null, BandeiraValida);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoMetodoPagamentoId), null);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        [InlineData(2147483648L)]
        [InlineData(9999999999999999L)]
        public void EmissorForaDaFaixa_400NoCampoDoEmissor(long emissor)
        {
            ValidationResult resultado = Validar(TokenValido, BandeiraValida, emissor);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoEmissorId), null);
        }

        [Theory]
        [InlineData(1L)]
        [InlineData(2147483647L)]
        public void EmissorNaFaixa_Passa(long emissor)
        {
            ValidationResult resultado = Validar(TokenValido, BandeiraValida, emissor);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("dispositivo com espaco")]
        [InlineData("4111111111111111")]
        [InlineData("4111.1111.1111.1111")]
        [InlineData("4111:1111:1111:1111")]
        public void DispositivoForaDoFormatoOuParecidoComCartao_400NoCampoDoDispositivo(string dispositivo)
        {
            ValidationResult resultado = Validar(TokenValido, BandeiraValida, null, dispositivo);

            AfirmarCartaoInvalidoNoCampo(resultado, nameof(LocacaoVO.CaucaoIdentificadorDispositivo), dispositivo);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void DispositivoEmBranco_Passa(string? dispositivo)
        {
            ValidationResult resultado = Validar(TokenValido, BandeiraValida, null, dispositivo);

            Assert.True(resultado.IsValid);
        }

        /// <summary>Sem caucao exigida o front manda so { idReserva }; os campos que vierem sao so conferidos no formato.</summary>
        [Fact]
        public void SemNenhumCampoDoCartao_Passa()
        {
            ValidationResult resultado = Validar(null, null, null, null);

            Assert.True(resultado.IsValid);
        }
    }
}
