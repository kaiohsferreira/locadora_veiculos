using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.Validators.Autenticacao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do GoogleVinculoVOValidator: credencial obrigatoria (ate 4096), CPF com 11 digitos apos remover
    /// a mascara, senha de 1 a 128 caracteres. Mensagens vem de ConstantsMessage (reuso do login por CPF).
    /// </summary>
    public class GoogleVinculoVOValidatorTests
    {
        private const string CpfValido = "529.982.247-25";
        private const string SenhaValida = "SenhaCliente#2026";

        private static GoogleVinculoVO VoValido()
        {
            return new GoogleVinculoVO { Credencial = "credencial-valida", Cpf = CpfValido, Senha = SenhaValida, Lembrar = false };
        }

        private static List<string> MensagensDe(GoogleVinculoVO vo)
        {
            GoogleVinculoVOValidator validador = new GoogleVinculoVOValidator();
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
            GoogleVinculoVO vo = VoValido();
            vo.Credencial = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroGoogleCredencialObrigatoria, mensagens);
        }

        [Fact]
        public void CredencialAcimaDoLimite_RecusaComMensagemDeTamanho()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Credencial = new string('a', ConstantsValues.TamanhoMaximoCredencialGoogle + 1);

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroGoogleCredencialTamanho, mensagens);
        }

        [Fact]
        public void CpfVazio_RecusaComMensagemObrigatoria()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Cpf = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginCpfObrigatorio, mensagens);
        }

        [Fact]
        public void CpfComDezDigitos_RecusaComMensagemDeFormato()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Cpf = "1234567890";

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginCpfFormato, mensagens);
        }

        [Fact]
        public void SenhaVazia_RecusaComMensagemObrigatoria()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Senha = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginSenhaObrigatoria, mensagens);
        }

        [Fact]
        public void SenhaAcimaDoLimite_RecusaComMensagemDeTamanho()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Senha = new string('s', ConstantsValues.TamanhoMaximoSenhaLogin + 1);

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(string.Format(ConstantsMessage.ErroLoginSenhaTamanho, ConstantsValues.TamanhoMaximoSenhaLogin), mensagens);
        }

        [Fact]
        public void SenhaNoLimite_EhAceita()
        {
            GoogleVinculoVO vo = VoValido();
            vo.Senha = new string('s', ConstantsValues.TamanhoMaximoSenhaLogin);

            List<string> mensagens = MensagensDe(vo);

            Assert.Empty(mensagens);
        }
    }
}
