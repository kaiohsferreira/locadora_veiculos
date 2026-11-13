using AutoVia.Communication.Validators.Seguros;
using AutoVia.Communication.ViewObjects.Seguros;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Seguros
{
    /// <summary>
    /// Testes do SeguroVOValidator: veiculo, seguradora, numero da apolice, vigencia
    /// (fim posterior ao inicio, igual ao ck_seguro_vigencia), premio e cobertura.
    /// </summary>
    public class SeguroVOValidatorTests
    {
        [Fact]
        public void VOValido_PassaNaValidacao()
        {
            SeguroVO vo = CriarVOValido();

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void VigenciaInvertida_FalhaComMensagemDeVigencia()
        {
            SeguroVO vo = CriarVOValido();
            vo.DataInicio = new DateOnly(2026, 12, 31);
            vo.DataFim = new DateOnly(2026, 1, 1);

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroSeguroVigenciaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void FimIgualAoInicio_FalhaComMensagemDeVigencia()
        {
            SeguroVO vo = CriarVOValido();
            vo.DataFim = vo.DataInicio;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.False(resultado.IsValid);
            Assert.Contains(ConstantsMessage.ErroSeguroVigenciaInvalida, ObterMensagens(resultado));
        }

        [Fact]
        public void DatasNaoInformadas_FalhaComMensagensDeObrigatorio()
        {
            SeguroVO vo = CriarVOValido();
            vo.DataInicio = default(DateOnly);
            vo.DataFim = default(DateOnly);

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);
            List<string> mensagens = ObterMensagens(resultado);

            Assert.Contains(ConstantsMessage.ErroSeguroDataInicioObrigatoria, mensagens);
            Assert.Contains(ConstantsMessage.ErroSeguroDataFimObrigatoria, mensagens);
            Assert.DoesNotContain(ConstantsMessage.ErroSeguroVigenciaInvalida, mensagens);
        }

        [Fact]
        public void VeiculoZero_FalhaComMensagemDeVeiculo()
        {
            SeguroVO vo = CriarVOValido();
            vo.IdVeiculo = 0;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroSeguroVeiculoObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void SeguradoraSoComEspacos_FalhaComMensagemObrigatoria()
        {
            SeguroVO vo = CriarVOValido();
            vo.Seguradora = "   ";

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroSeguroSeguradoraObrigatoria, ObterMensagens(resultado));
        }

        [Fact]
        public void SeguradoraAcimaDoLimite_FalhaComMensagemDeTamanho()
        {
            SeguroVO vo = CriarVOValido();
            vo.Seguradora = new string('a', ConstantsValues.TamanhoMaximoSeguradora + 1);

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(string.Format(ConstantsMessage.ErroSeguroSeguradoraTamanho, ConstantsValues.TamanhoMaximoSeguradora), ObterMensagens(resultado));
        }

        [Fact]
        public void NumeroApoliceVazio_FalhaComMensagemObrigatorio()
        {
            SeguroVO vo = CriarVOValido();
            vo.NumeroApolice = string.Empty;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroSeguroNumeroApoliceObrigatorio, ObterMensagens(resultado));
        }

        [Fact]
        public void NumeroApoliceAcimaDoLimite_FalhaComMensagemDeTamanho()
        {
            SeguroVO vo = CriarVOValido();
            vo.NumeroApolice = new string('1', ConstantsValues.TamanhoMaximoNumeroApolice + 1);

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(string.Format(ConstantsMessage.ErroSeguroNumeroApoliceTamanho, ConstantsValues.TamanhoMaximoNumeroApolice), ObterMensagens(resultado));
        }

        [Fact]
        public void PremioZero_FalhaComMensagemDePremio()
        {
            SeguroVO vo = CriarVOValido();
            vo.ValorPremio = 0m;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroSeguroValorPremio, ObterMensagens(resultado));
        }

        [Fact]
        public void PremioComTresCasasDecimais_FalhaComMensagemDePrecisao()
        {
            SeguroVO vo = CriarVOValido();
            vo.ValorPremio = 1200.555m;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(ConstantsMessage.ErroSeguroValorPremioPrecisao, ObterMensagens(resultado));
        }

        [Fact]
        public void CoberturaNulaEOpcional_PassaNaValidacao()
        {
            SeguroVO vo = CriarVOValido();
            vo.Cobertura = null;

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void CoberturaAcimaDoLimite_FalhaComMensagemDeTamanho()
        {
            SeguroVO vo = CriarVOValido();
            vo.Cobertura = new string('c', ConstantsValues.TamanhoMaximoCoberturaSeguro + 1);

            ValidationResult resultado = new SeguroVOValidator().Validate(vo);

            Assert.Contains(string.Format(ConstantsMessage.ErroSeguroCoberturaTamanho, ConstantsValues.TamanhoMaximoCoberturaSeguro), ObterMensagens(resultado));
        }

        private static SeguroVO CriarVOValido()
        {
            SeguroVO vo = new SeguroVO
            {
                IdVeiculo = 1,
                Seguradora = "Porto Seguro",
                NumeroApolice = "AP-2026-0001",
                DataInicio = new DateOnly(2026, 1, 1),
                DataFim = new DateOnly(2026, 12, 31),
                ValorPremio = 1200m,
                Cobertura = "Compreensiva"
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
