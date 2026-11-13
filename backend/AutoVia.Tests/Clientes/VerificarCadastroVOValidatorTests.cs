using AutoVia.Communication.Enums;
using AutoVia.Communication.Validators.Clientes;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Clientes
{
    /// <summary>
    /// Testes do VerificarCadastroVOValidator: campo obrigatorio e dentro do enum; CPF com digito
    /// verificador (com mascara); CNH com 11 digitos; e-mail aparado antes da validacao e limite de
    /// 120; teto de 200 caracteres no valor cru. Valor malformado nunca chega ao banco: estes testes
    /// garantem que o validador recusa antes da action.
    /// </summary>
    public class VerificarCadastroVOValidatorTests
    {
        private const string CpfValido = "123.456.789-09";
        private const string CnhValida = "123-456-789-01";
        private const string EmailValido = "maria.silva@exemplo.com";

        [Fact]
        public void CpfValidoComMascara_Passa()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, CpfValido);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void CampoAusente_FalhaComMensagemDeCampo()
        {
            VerificarCadastroVO vo = CriarVO(null, CpfValido);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteVerificacaoCampoInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CampoForaDoEnum_FalhaComMensagemDeCampo()
        {
            VerificarCadastroVO vo = CriarVO((CampoVerificacaoCadastro)99, CpfValido);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteVerificacaoCampoInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfVazio_FalhaComMensagemObrigatorio()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, "   ");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfComDigitoVerificadorErrado_FalhaComMensagemInvalido()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, "123.456.789-10");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhComOnzeDigitosComTracos_Passa()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CNH, CnhValida);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void CnhComDezDigitos_FalhaComMensagemDeQuantidade()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CNH, "1234567890");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            string mensagem = string.Format(ConstantsMessage.ErroClienteCnhNumeroInvalido, ConstantsValues.TamanhoCnhNumero);
            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhVazia_FalhaComMensagemObrigatoria()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CNH, string.Empty);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCnhNumeroObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void EmailValidoComEspacosNasBordas_Passa()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, "  " + EmailValido + "  ");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.True(resultado.IsValid, string.Join(" | ", ObterMensagens(resultado)));
        }

        [Fact]
        public void EmailInvalido_FalhaComMensagemDeEmail()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, "nao-e-email");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteEmailInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void EmailVazio_FalhaComMensagemObrigatorio()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, "   ");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteEmailObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void EmailAcimaDoLimiteAposTrim_FalhaComMensagemDeTamanho()
        {
            string emailLongo = new string('a', 115) + "@exemplo.com";
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, "  " + emailLongo + "  ");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            string mensagem = string.Format(ConstantsMessage.ErroClienteCampoTamanho, ConstantsMessage.RotuloEmailCliente, ConstantsValues.TamanhoMaximoEmailCliente);
            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorNuloParaCpf_FalhaComMensagemObrigatorio()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, null!);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteCpfObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorNuloParaEmail_FalhaComMensagemObrigatorio()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, null!);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroClienteEmailObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void CpfComDigitosArabicoIndicos_FalhaComMensagemInvalido()
        {
            // 52998224725 (CPF valido) com digitos arabico-indicos: nao pode passar.
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, "٥٢٩٩٨٢٢٤٧٢٥");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteCpfInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void CnhComDigitosArabicoIndicos_FalhaComMensagemDeQuantidade()
        {
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CNH, "١٢٣٤٥٦٧٨٩٠١");

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            string mensagem = string.Format(ConstantsMessage.ErroClienteCnhNumeroInvalido, ConstantsValues.TamanhoCnhNumero);
            Assert.Contains(mensagem, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorCrudoAcimaDe200Caracteres_FalhaComMensagemDeValor()
        {
            string valorLongo = new string('1', ConstantsValues.TamanhoMaximoValorVerificacaoCadastro + 1);
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.CPF, valorLongo);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroClienteVerificacaoValorInvalido, ObterMensagens(resultado));
        }

        [Fact]
        public void ValorNoLimiteDe200Caracteres_NaoFalhaPorTeto()
        {
            string valorNoLimite = new string('a', ConstantsValues.TamanhoMaximoValorVerificacaoCadastro);
            VerificarCadastroVO vo = CriarVO(CampoVerificacaoCadastro.EMAIL, valorNoLimite);

            ValidationResult resultado = new VerificarCadastroVOValidator().Validate(vo);

            Assert.DoesNotContain(ConstantsMessage.ErroClienteVerificacaoValorInvalido, ObterMensagens(resultado));
        }

        private static VerificarCadastroVO CriarVO(CampoVerificacaoCadastro? campo, string valor)
        {
            VerificarCadastroVO vo = new VerificarCadastroVO
            {
                Campo = campo,
                Valor = valor
            };

            return vo;
        }

        private static List<string> ObterMensagens(ValidationResult resultado)
        {
            List<string> mensagens = resultado.Errors.Select(erro => erro.ErrorMessage).ToList();

            return mensagens;
        }
    }
}
