using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;

namespace AutoVia.Tests.Frota
{
    /// <summary>Coerencia das constantes do modulo de fotos do veiculo com o contrato veiculo-foto.</summary>
    public sealed class VeiculoFotoConstantesTests
    {
        [Fact]
        public void TetoDoCorpo_EhATamanhoDaFotoMaisAFolgaDoMultipart()
        {
            Assert.Equal(ConstantsArmazenamento.TamanhoMaximoFotoBytes + ConstantsValues.FolgaCorpoMultipartFotoVeiculo, ConstantsValues.TamanhoMaximoCorpoFotoVeiculo);
        }

        [Fact]
        public void ValoresDoContrato_SaoOsDoVeiculoFotoContract()
        {
            Assert.Equal(15, ConstantsValues.MaximoFotosPorVeiculo);
            Assert.Equal(65536, ConstantsValues.FolgaCorpoMultipartFotoVeiculo);
            Assert.Equal(10551296, ConstantsValues.TamanhoMaximoCorpoFotoVeiculo);
            Assert.Equal(1600, ConstantsValues.LadoMaximoFotoVeiculoPixels);
            Assert.Equal(85, ConstantsValues.QualidadeJpegFotoVeiculo);
            Assert.Equal(2, ConstantsValues.MaximoProcessamentosSimultaneosFoto);
            Assert.Equal(8, ConstantsValues.MaximoFotosNaFilaDeProcessamento);
            Assert.Equal(30, ConstantsValues.TempoMaximoEsperaProcessamentoFotoSegundos);
            Assert.Equal("veiculos/{0}/fotos/{1}.jpg", ConstantsValues.FormatoChaveFotoVeiculo);
            Assert.Equal("N", ConstantsValues.FormatoGuidChaveFotoVeiculo);
            Assert.Equal("O", ConstantsValues.FormatoExpiracaoUrlFoto);
            Assert.Equal("AutoViaEnvioFotoVeiculo", ConstantsValues.PoliticaLimiteEnvioFotoVeiculo);
            Assert.Equal("Veiculos:LimiteEnvioFotoPorMinuto", ConstantsValues.ChaveLimiteEnvioFotoVeiculo);
            Assert.Equal(60, ConstantsValues.LimiteEnvioFotoVeiculoPorMinutoPadrao);
            Assert.Equal("foto", ConstantsValues.CampoFotoVeiculo);
        }

        [Fact]
        public void LadoDaSaida_NaoPassaDoLimiteDaImagemAceita()
        {
            Assert.True(ConstantsValues.LadoMaximoFotoVeiculoPixels <= ConstantsArmazenamento.DimensaoMaximaFotoPixels);
        }
    }
}
