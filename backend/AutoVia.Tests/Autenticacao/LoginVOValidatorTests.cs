using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Communication.Validators.Autenticacao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Testes do LoginVOValidator: perfil obrigatorio e do enum, CPF com 11 digitos apos
    /// remover a mascara, senha de 1 a 128 caracteres. Mensagens vem de ConstantsMessage.
    /// </summary>
    public class LoginVOValidatorTests
    {
        private static LoginVO VoValido()
        {
            return new LoginVO { Perfil = PerfilUsuario.CLIENTE, Cpf = "529.982.247-25", Senha = "SenhaCliente#2026" };
        }

        private static List<string> MensagensDe(LoginVO vo)
        {
            LoginVOValidator validador = new LoginVOValidator();
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
        public void PerfilAusente_RecusaComMensagemDePerfil()
        {
            LoginVO vo = VoValido();
            vo.Perfil = null;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginPerfilInvalido, mensagens);
        }

        [Fact]
        public void PerfilForaDoEnum_RecusaComMensagemDePerfil()
        {
            LoginVO vo = VoValido();
            vo.Perfil = (PerfilUsuario)99;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginPerfilInvalido, mensagens);
        }

        [Fact]
        public void CpfVazio_RecusaComMensagemObrigatoria()
        {
            LoginVO vo = VoValido();
            vo.Cpf = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginCpfObrigatorio, mensagens);
        }

        [Fact]
        public void CpfComDezDigitos_RecusaComMensagemDeFormato()
        {
            LoginVO vo = VoValido();
            vo.Cpf = "5299822472";

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginCpfFormato, mensagens);
        }

        [Fact]
        public void CpfComMascaraERequisitosDeDigitos_Passa()
        {
            LoginVO vo = VoValido();
            vo.Cpf = "529.982.247-25";

            List<string> mensagens = MensagensDe(vo);

            Assert.Empty(mensagens);
        }

        [Fact]
        public void SenhaVazia_RecusaComMensagemObrigatoria()
        {
            LoginVO vo = VoValido();
            vo.Senha = string.Empty;

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(ConstantsMessage.ErroLoginSenhaObrigatoria, mensagens);
        }

        [Fact]
        public void Senha129Caracteres_RecusaComLimite()
        {
            LoginVO vo = VoValido();
            vo.Senha = new string('a', ConstantsValues.TamanhoMaximoSenhaLogin + 1);

            List<string> mensagens = MensagensDe(vo);

            Assert.Contains(string.Format(ConstantsMessage.ErroLoginSenhaTamanho, ConstantsValues.TamanhoMaximoSenhaLogin), mensagens);
        }

        [Fact]
        public void Senha128Caracteres_Passa()
        {
            LoginVO vo = VoValido();
            vo.Senha = new string('a', ConstantsValues.TamanhoMaximoSenhaLogin);

            List<string> mensagens = MensagensDe(vo);

            Assert.Empty(mensagens);
        }
    }
}
