using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Autenticacao;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Validador do pedido de recuperacao de senha: perfil obrigatorio (CLIENTE ou FUNCIONARIO) e CPF com
    /// 11 digitos apos remover a mascara. As mensagens sao as do login (ConstantsMessage).
    /// </summary>
    public class SolicitarRecuperacaoSenhaVOValidatorTests
    {
        private const string CpfValido = "529.982.247-25";
        private const string CpfCurto = "1234567890";

        private static SolicitarRecuperacaoSenhaVO VoValido()
        {
            return new SolicitarRecuperacaoSenhaVO { Perfil = PerfilUsuario.CLIENTE, Cpf = CpfValido };
        }

        [Fact]
        public void PedidoValido_PassaSemErro()
        {
            SolicitarRecuperacaoSenhaVOValidator validador = new SolicitarRecuperacaoSenhaVOValidator();

            ValidationResult resultado = validador.Validate(VoValido());

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void PerfilAusente_DevolveMensagemDePerfil()
        {
            SolicitarRecuperacaoSenhaVOValidator validador = new SolicitarRecuperacaoSenhaVOValidator();
            SolicitarRecuperacaoSenhaVO vo = VoValido();
            vo.Perfil = null;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroLoginPerfilInvalido);
        }

        [Fact]
        public void PerfilForaDoEnum_DevolveMensagemDePerfil()
        {
            SolicitarRecuperacaoSenhaVOValidator validador = new SolicitarRecuperacaoSenhaVOValidator();
            SolicitarRecuperacaoSenhaVO vo = VoValido();
            vo.Perfil = (PerfilUsuario)99;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroLoginPerfilInvalido);
        }

        [Fact]
        public void CpfVazio_DevolveMensagemDeCpfObrigatorio()
        {
            SolicitarRecuperacaoSenhaVOValidator validador = new SolicitarRecuperacaoSenhaVOValidator();
            SolicitarRecuperacaoSenhaVO vo = VoValido();
            vo.Cpf = string.Empty;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroLoginCpfObrigatorio);
        }

        [Fact]
        public void CpfComMenosDeOnzeDigitos_DevolveMensagemDeFormato()
        {
            SolicitarRecuperacaoSenhaVOValidator validador = new SolicitarRecuperacaoSenhaVOValidator();
            SolicitarRecuperacaoSenhaVO vo = VoValido();
            vo.Cpf = CpfCurto;

            ValidationResult resultado = validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(resultado.Errors, erro => erro.ErrorMessage == ConstantsMessage.ErroLoginCpfFormato);
        }
    }
}
