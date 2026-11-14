using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Empresas;
using AutoVia.Communication.Validators.Empresas;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Empresas
{
    /// <summary>
    /// Testes do VerificarCadastroEmpresaVOValidator (P10-BE-01): campo obrigatorio e dentro do enum, CNPJ com
    /// digito verificador (mascara aceita), e-mail valido e ate 120 caracteres, e teto de 200 caracteres no valor cru.
    /// Valor malformado nunca chega ao banco: o validador recusa antes da action.
    /// </summary>
    public class VerificarCadastroEmpresaVOValidatorTests
    {
        private readonly VerificarCadastroEmpresaVOValidator _validador = new VerificarCadastroEmpresaVOValidator();

        [Theory]
        [InlineData("11222333000181")]
        [InlineData("11.222.333/0001-81")]
        public void Cnpj_ValidoComOuSemMascara_Aceita(string valor)
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = valor };

            ValidationResult resultado = _validador.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("11222333000182")]
        [InlineData("00000000000000")]
        [InlineData("123")]
        public void Cnpj_InvalidoZeradoOuCurto_Recusa(string valor)
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = valor };

            ValidationResult resultado = _validador.Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroLocadoraCnpjInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void Cnpj_Vazio_PedeOValor()
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.CNPJ, Valor = " " };

            ValidationResult resultado = _validador.Validate(vo);

            Assert.Contains(ConstantsMessage.ErroLocadoraCnpjObrigatorio, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void Email_ValidoEAteOLimite_Aceita()
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = "  Contato@RotaSul.exemplo.com " };

            ValidationResult resultado = _validador.Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void Email_InvalidoVazioOuAcimaDe120_TemMensagemPropria()
        {
            VerificarCadastroEmpresaVO invalido = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = "sem-arroba" };
            VerificarCadastroEmpresaVO vazio = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = string.Empty };
            VerificarCadastroEmpresaVO longo = new VerificarCadastroEmpresaVO { Campo = CampoVerificacaoEmpresa.EMAIL, Valor = new string('a', 115) + "@x.com" };

            Assert.Contains(ConstantsMessage.ErroLocadoraEmailInvalido, _validador.Validate(invalido).Errors.Select(erro => erro.ErrorMessage));
            Assert.Contains(ConstantsMessage.ErroLocadoraEmailObrigatorio, _validador.Validate(vazio).Errors.Select(erro => erro.ErrorMessage));
            Assert.Contains(
                string.Format(ConstantsMessage.ErroLocadoraCampoTamanho, ConstantsMessage.RotuloEmailLocadora, ConstantsValues.TamanhoMaximoEmailLocadora),
                _validador.Validate(longo).Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void Campo_AusenteOuForaDoEnum_PedeOCampo()
        {
            VerificarCadastroEmpresaVO semCampo = new VerificarCadastroEmpresaVO { Campo = null, Valor = "11222333000181" };

            ValidationResult resultado = _validador.Validate(semCampo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroLocadoraVerificacaoCampoInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }

        [Fact]
        public void Valor_AcimaDe200CaracteresNoCru_Recusa()
        {
            VerificarCadastroEmpresaVO vo = new VerificarCadastroEmpresaVO
            {
                Campo = CampoVerificacaoEmpresa.EMAIL,
                Valor = new string('a', ConstantsValues.TamanhoMaximoValorVerificacaoCadastro + 1) + "@x.com"
            };

            ValidationResult resultado = _validador.Validate(vo);

            Assert.Contains(ConstantsMessage.ErroLocadoraVerificacaoValorInvalido, resultado.Errors.Select(erro => erro.ErrorMessage));
        }
    }
}
