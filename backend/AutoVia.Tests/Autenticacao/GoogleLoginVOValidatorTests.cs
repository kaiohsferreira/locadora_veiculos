using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.Validators.Autenticacao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do GoogleLoginVOValidator: a credencial e obrigatoria e tem no maximo 4096 caracteres.
    /// Mensagens vem de ConstantsMessage.
    /// </summary>
    public class GoogleLoginVOValidatorTests
    {
        private static GoogleLoginVO VoValido()
        {
            return new GoogleLoginVO { Credencial = "credencial-valida", Lembrar = true };
        }

        private static List<string> MensagensDe(GoogleLoginVO vo)
        {
            GoogleLoginVOValidator validador = new GoogleLoginVOValidator();
            ValidationResult resultado = validador.Validate(vo);

            return resultado.Errors.Select(erro => erro.ErrorMessage).ToList();
        }

        [Fact]
        public void VoValido_NaoTemErros()
        {
            List<string> mensagens = MensagensDe(VoValido());

            Assert.Empty(mensagens);
        }

        [Fact]
        public void CredencialVazia_RecusaComMensagemObrigatoria()
        {
            GoogleLoginVO vo = VoValido();
            vo.Credencial = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroGoogleCredencialObrigatoria, mensagens);
        }

        [Fact]
        public void CredencialAcimaDoLimite_RecusaComMensagemDeTamanho()
        {
            GoogleLoginVO vo = VoValido();
            vo.Credencial = new string('a', ConstantsValues.TamanhoMaximoCredencialGoogle + 1);

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroGoogleCredencialTamanho, mensagens);
        }

        [Fact]
        public void CredencialNoLimite_EhAceita()
        {
            GoogleLoginVO vo = VoValido();
            vo.Credencial = new string('a', ConstantsValues.TamanhoMaximoCredencialGoogle);

            List<string> mensagens = MensagensDe(vo);

            Assert.Empty(mensagens);
        }

        [Fact]
        public void Lembrar_NaoTemRegraDeValidacao()
        {
            GoogleLoginVO vo = VoValido();
            vo.Lembrar = false;

            List<string> mensagens = MensagensDe(vo);

            Assert.Empty(mensagens);
        }
    }
}
