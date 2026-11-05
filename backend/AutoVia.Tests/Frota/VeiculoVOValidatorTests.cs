using AutoVia.Communication.Validators.Frota;
using AutoVia.Communication.ViewObjects.Frota;
using AutoVia.Helpers;
using FluentValidation.Results;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Testes do VeiculoVOValidator: placa, chassi, renavam, marca, modelo, anos, cor,
    /// quilometragem, valores (diaria e venda), data de aquisicao e categoria.
    /// </summary>
    public class VeiculoVOValidatorTests
    {
        [Fact]
        public void VOValido_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Theory]
        [InlineData("ABC1234")]
        [InlineData("ABC1D23")]
        [InlineData("abc1d23")]
        [InlineData("ABC-1D23")]
        [InlineData("abc-1234")]
        public void PlacaAntigaOuMercosulEmQualquerCaixaOuComHifen_PassaNaValidacao(string placa)
        {
            VeiculoVO vo = CriarVOValido();
            vo.Placa = placa;

            ValidationResult resultado = Validar(vo);

            Assert.False(ContemErro(resultado, nameof(VeiculoVO.Placa), ConstantsMessage.ErroVeiculoPlacaInvalida));
        }

        [Theory]
        [InlineData("ABC12345")]
        [InlineData("AB1234")]
        [InlineData("ABCD123")]
        [InlineData("1BC1D23")]
        [InlineData("")]
        public void PlacaForaDoPadrao_FalhaComMensagemPlaca(string placa)
        {
            VeiculoVO vo = CriarVOValido();
            vo.Placa = placa;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Placa), ConstantsMessage.ErroVeiculoPlacaInvalida));
        }

        [Theory]
        [InlineData("9BWZZZ377VT004251")]
        [InlineData("9bwzzz377vt004251")]
        public void ChassiValido_PassaNaValidacao(string chassi)
        {
            VeiculoVO vo = CriarVOValido();
            vo.Chassi = chassi;

            ValidationResult resultado = Validar(vo);

            Assert.False(ContemErro(resultado, nameof(VeiculoVO.Chassi), ConstantsMessage.ErroVeiculoChassiInvalido));
        }

        [Theory]
        [InlineData("9BWZZZ377VT00425")]
        [InlineData("9BWZZZ377VT0042511")]
        [InlineData("9BWZZZ377IT004251")]
        [InlineData("9BWZZZ377OT004251")]
        [InlineData("9BWZZZ377QT004251")]
        public void ChassiComTamanhoErradoOuComIOQ_FalhaComMensagemChassi(string chassi)
        {
            VeiculoVO vo = CriarVOValido();
            vo.Chassi = chassi;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Chassi), ConstantsMessage.ErroVeiculoChassiInvalido));
        }

        [Fact]
        public void RenavamComOnzeDigitos_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Renavam = "12345678901";

            ValidationResult resultado = Validar(vo);

            Assert.False(ContemErro(resultado, nameof(VeiculoVO.Renavam), ConstantsMessage.ErroVeiculoRenavamInvalido));
        }

        [Theory]
        [InlineData("1234567890")]
        [InlineData("123456789012")]
        [InlineData("1234567890A")]
        public void RenavamComOutroTamanhoOuLetra_FalhaComMensagemRenavam(string renavam)
        {
            VeiculoVO vo = CriarVOValido();
            vo.Renavam = renavam;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Renavam), ConstantsMessage.ErroVeiculoRenavamInvalido));
        }

        [Fact]
        public void AnoFabricacaoAbaixoDoMinimo_FalhaComIntervaloNoCampoFabricacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = ConstantsValues.AnoMinimoFabricacaoVeiculo - 1;
            vo.AnoModelo = ConstantsValues.AnoMinimoFabricacaoVeiculo - 1;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.AnoFabricacao), MensagemIntervaloFabricacao()));
        }

        [Fact]
        public void AnoFabricacaoNoMinimoEAnoModeloIgual_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = ConstantsValues.AnoMinimoFabricacaoVeiculo;
            vo.AnoModelo = ConstantsValues.AnoMinimoFabricacaoVeiculo;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void AnoFabricacaoAteAnoSeguinteAoAtual_PassaNaValidacao()
        {
            int anoLimite = DateTime.Now.Year + ConstantsValues.AnosAdiantamentoVeiculo;
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = anoLimite;
            vo.AnoModelo = anoLimite;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void AnoFabricacaoDoisAnosAposOAtual_FalhaComIntervaloNoCampoFabricacao()
        {
            int anoAcima = DateTime.Now.Year + ConstantsValues.AnosAdiantamentoVeiculo + 1;
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = anoAcima;
            vo.AnoModelo = anoAcima;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.AnoFabricacao), MensagemIntervaloFabricacao()));
        }

        [Fact]
        public void AnoModeloAnteriorAoDeFabricacao_FalhaComMensagemNoCampoAnoModelo()
        {
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = 2022;
            vo.AnoModelo = 2021;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.AnoModelo), ConstantsMessage.ErroVeiculoAnoModeloAnterior));
        }

        [Fact]
        public void AnoModeloDoisAnosAposOAtual_FalhaComMensagemAnoMaximo()
        {
            int anoLimite = DateTime.Now.Year + ConstantsValues.AnosAdiantamentoVeiculo;
            VeiculoVO vo = CriarVOValido();
            vo.AnoFabricacao = anoLimite;
            vo.AnoModelo = anoLimite + 1;
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoAnoModeloMaximo, anoLimite);

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.AnoModelo), mensagemEsperada));
        }

        [Fact]
        public void KmNegativo_FalhaComMensagemKm()
        {
            VeiculoVO vo = CriarVOValido();
            vo.KmAtual = -1;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.KmAtual), ConstantsMessage.ErroVeiculoKmNegativo));
        }

        [Fact]
        public void KmZero_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.KmAtual = 0;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorDiariaZero_FalhaComMensagemValorDiaria()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorDiaria = 0m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorDiaria), ConstantsMessage.ErroVeiculoValorDiaria));
        }

        [Fact]
        public void ValorDiariaComTresCasasDecimais_FalhaComMensagemPrecisao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorDiaria = 150.555m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorDiaria), ConstantsMessage.ErroVeiculoValorDiariaPrecisao));
        }

        [Fact]
        public void ValorDiariaComNoveDigitosInteiros_FalhaComMensagemPrecisao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorDiaria = 100000000m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorDiaria), ConstantsMessage.ErroVeiculoValorDiariaPrecisao));
        }

        [Fact]
        public void ValorDiariaNoLimiteEComZerosADireita_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorDiaria = 99999999.99m;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorDiariaComZerosADireitaEmTresCasas_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorDiaria = 150.500m;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorVendaNulo_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorVenda = null;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorVendaZero_FalhaComMensagemValorVenda()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorVenda = 0m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorVenda), ConstantsMessage.ErroVeiculoValorVenda));
        }

        [Fact]
        public void ValorVendaComDezDigitosInteiros_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorVenda = 1234567890.12m;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void ValorVendaComOnzeDigitosInteiros_FalhaComMensagemPrecisao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorVenda = 12345678901.00m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorVenda), ConstantsMessage.ErroVeiculoValorVendaPrecisao));
        }

        [Fact]
        public void ValorVendaComTresCasasDecimais_FalhaComMensagemPrecisao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.ValorVenda = 45000.555m;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.ValorVenda), ConstantsMessage.ErroVeiculoValorVendaPrecisao));
        }

        [Fact]
        public void DataAquisicaoFutura_FalhaComMensagemData()
        {
            VeiculoVO vo = CriarVOValido();
            vo.DataAquisicao = DateOnly.FromDateTime(DateTime.Now).AddDays(1);

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.DataAquisicao), ConstantsMessage.ErroVeiculoDataAquisicaoFutura));
        }

        [Fact]
        public void DataAquisicaoDeHojeOuNula_PassaNaValidacao()
        {
            VeiculoVO comHoje = CriarVOValido();
            comHoje.DataAquisicao = DateOnly.FromDateTime(DateTime.Now);
            VeiculoVO semData = CriarVOValido();
            semData.DataAquisicao = null;

            ValidationResult resultadoHoje = Validar(comHoje);
            ValidationResult resultadoNulo = Validar(semData);

            Assert.True(resultadoHoje.IsValid);
            Assert.True(resultadoNulo.IsValid);
        }

        [Fact]
        public void IdCategoriaZero_FalhaComMensagemCategoria()
        {
            VeiculoVO vo = CriarVOValido();
            vo.IdCategoria = 0;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.IdCategoria), ConstantsMessage.ErroVeiculoCategoriaObrigatoria));
        }

        [Fact]
        public void MarcaSoComEspacos_FalhaComMensagemObrigatoria()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Marca = "   ";

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Marca), ConstantsMessage.ErroVeiculoMarcaObrigatoria));
        }

        [Fact]
        public void MarcaComQuarentaEUmCaracteres_FalhaComMensagemTamanho()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Marca = new string('m', ConstantsValues.TamanhoMaximoMarcaVeiculo + 1);
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoMarcaTamanho, ConstantsValues.TamanhoMaximoMarcaVeiculo);

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Marca), mensagemEsperada));
        }

        [Fact]
        public void ModeloVazio_FalhaComMensagemObrigatorio()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Modelo = string.Empty;

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Modelo), ConstantsMessage.ErroVeiculoModeloObrigatorio));
        }

        [Fact]
        public void ModeloComQuarentaEUmCaracteres_FalhaComMensagemTamanho()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Modelo = new string('o', ConstantsValues.TamanhoMaximoModeloVeiculo + 1);
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoModeloTamanho, ConstantsValues.TamanhoMaximoModeloVeiculo);

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Modelo), mensagemEsperada));
        }

        [Fact]
        public void CorNula_PassaNaValidacao()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Cor = null;

            ValidationResult resultado = Validar(vo);

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public void CorComTrintaEUmCaracteres_FalhaComMensagemTamanho()
        {
            VeiculoVO vo = CriarVOValido();
            vo.Cor = new string('c', ConstantsValues.TamanhoMaximoCorVeiculo + 1);
            string mensagemEsperada = string.Format(ConstantsMessage.ErroVeiculoCorTamanho, ConstantsValues.TamanhoMaximoCorVeiculo);

            ValidationResult resultado = Validar(vo);

            Assert.True(ContemErro(resultado, nameof(VeiculoVO.Cor), mensagemEsperada));
        }

        private static VeiculoVO CriarVOValido()
        {
            VeiculoVO vo = new VeiculoVO
            {
                IdCategoria = 1,
                Placa = "ABC1D23",
                Chassi = "9BWZZZ377VT004251",
                Renavam = "12345678901",
                Marca = "Fiat",
                Modelo = "Toro",
                AnoFabricacao = 2022,
                AnoModelo = 2023,
                Cor = "Branco",
                KmAtual = 0,
                ValorDiaria = 150m,
                ValorVenda = null,
                DataAquisicao = null
            };

            return vo;
        }

        private static ValidationResult Validar(VeiculoVO vo)
        {
            VeiculoVOValidator validator = new VeiculoVOValidator();
            ValidationResult resultado = validator.Validate(vo);

            return resultado;
        }

        private static bool ContemErro(ValidationResult resultado, string propriedade, string mensagem)
        {
            bool contem = resultado.Errors.Any(erro => erro.PropertyName == propriedade && erro.ErrorMessage == mensagem);

            return contem;
        }

        private static string MensagemIntervaloFabricacao()
        {
            string mensagem = string.Format(
                ConstantsMessage.ErroVeiculoAnoFabricacaoIntervalo,
                ConstantsValues.AnoMinimoFabricacaoVeiculo,
                DateTime.Now.Year + ConstantsValues.AnosAdiantamentoVeiculo);

            return mensagem;
        }
    }
}
