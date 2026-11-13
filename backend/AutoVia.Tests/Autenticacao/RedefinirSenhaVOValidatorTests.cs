using AutoVia.Communication.Validators.Autenticacao;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Validador da redefinicao de senha: token de 1 a 128 caracteres; nova senha com a mesma regra do cadastro
    /// (8 a 64 caracteres, letra e numero); confirmacao igual a nova senha. Mensagens de ConstantsMessage.
    /// </summary>
    public class RedefinirSenhaVOValidatorTests
    {
        private const string TokenValido = "tokenValido-abcdefghijklmnopqrstuvwxyz0123456789ABC";
        private const string SenhaValida = "Senha1234";
        private const int TamanhoTokenAcima = ConstantsValues.TamanhoMaximoToken + 1;
        private const char CaractereDoTokenLongo = 'a';

        private static RedefinirSenhaVO VoValido()
        {
            return new RedefinirSenhaVO { Token = TokenValido, NovaSenha = SenhaValida, ConfirmacaoSenha = SenhaValida };
        }

        private static bool ContemMensagem(ValidationResult resultado, string mensagem)
        {
            return resultado.Errors.Any(erro => erro.ErrorMessage == mensagem);
        }

        [Fact]
        public void RedefinicaoValida_PassaSemErro()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();

            ValidationResult resultado = validador.Validate(VoValido());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void TokenVazio_DevolveMensagemDeLinkIncompleto()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.Token = string.Empty;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoTokenObrigatorio));
        }

        [Fact]
        public void TokenAcimaDe128Caracteres_EhRecusado()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.Token = new string(CaractereDoTokenLongo, TamanhoTokenAcima);

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoLinkInvalido));
        }

        [Fact]
        public void NovaSenhaVazia_DevolveMensagemDeSenhaObrigatoria()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.NovaSenha = string.Empty;
            vo.ConfirmacaoSenha = string.Empty;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoSenhaObrigatoria));
        }

        [Fact]
        public void NovaSenhaComSeteCaracteres_DevolveMensagemDeTamanhoComOsLimitesDoCadastro()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.NovaSenha = "Abc1234";
            vo.ConfirmacaoSenha = "Abc1234";

            ValidationResult resultado = validador.Validate(vo);

            string mensagemEsperada = string.Format(ConstantsMessage.ErroRedefinicaoSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, mensagemEsperada));
        }

        [Fact]
        public void NovaSenhaComSessentaECincoCaracteres_EhRecusadaPeloTamanho()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.NovaSenha = "a1" + new string('b', ConstantsValues.TamanhoMaximoSenhaCadastro - 1);
            vo.ConfirmacaoSenha = vo.NovaSenha;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, string.Format(ConstantsMessage.ErroRedefinicaoSenhaTamanho, ConstantsValues.TamanhoMinimoSenhaCadastro, ConstantsValues.TamanhoMaximoSenhaCadastro)));
        }

        [Fact]
        public void NovaSenhaSoComLetras_DevolveMensagemDeSenhaFraca()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.NovaSenha = "SomenteLetras";
            vo.ConfirmacaoSenha = "SomenteLetras";

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoSenhaFraca));
        }

        [Fact]
        public void NovaSenhaSoComNumeros_DevolveMensagemDeSenhaFraca()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.NovaSenha = "12345678";
            vo.ConfirmacaoSenha = "12345678";

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoSenhaFraca));
        }

        [Fact]
        public void ConfirmacaoVazia_DevolveMensagemDeConfirmacaoObrigatoria()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.ConfirmacaoSenha = string.Empty;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoConfirmacaoObrigatoria));
        }

        [Fact]
        public void ConfirmacaoDiferente_DevolveMensagemDeConfirmacaoDiferente()
        {
            RedefinirSenhaVOValidator validador = new RedefinirSenhaVOValidator();
            RedefinirSenhaVO vo = VoValido();
            vo.ConfirmacaoSenha = "Senha9999";

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.True(ContemMensagem(resultado, ConstantsMessage.ErroRedefinicaoConfirmacaoDiferente));
        }
    }
}
