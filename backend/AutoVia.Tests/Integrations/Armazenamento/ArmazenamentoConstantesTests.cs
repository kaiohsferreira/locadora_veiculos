using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Prende os valores de ConstantsValues.Armazenamento, a igualdade com ConstantsArmazenamento e o formato do link (sem RegexOptions).</summary>
    public sealed class ArmazenamentoConstantesTests
    {
        [Fact]
        public void ProvedorDiscoLocal_TemOValorDoContrato()
        {
            Assert.Equal("DISCO_LOCAL", ConstantsArmazenamento.ProvedorDiscoLocal);
        }

        [Fact]
        public void TiposEMaximoDoProvedorDeDiscoSaoIguaisAosDaIntegracao()
        {
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, ConstantsValues.TipoConteudoJpeg);
            Assert.Equal(ConstantsArmazenamento.MimePng, ConstantsValues.TipoConteudoPng);
            Assert.Equal(ConstantsArmazenamento.TamanhoMaximoFotoBytes, ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes);
        }

        [Fact]
        public void LimitesDaRotaEPrazoDoLinkTemOsValoresDoContrato()
        {
            Assert.Equal(600, ConstantsValues.LimiteLeituraArquivoPorMinutoPadrao);
            Assert.Equal(3000, ConstantsValues.LimiteLeituraArquivoGlobalPorMinutoPadrao);
            Assert.Equal(60, ConstantsValues.IntervaloRegistroRecusaLimiteSegundos);
            Assert.Equal(43, ConstantsValues.TamanhoAssinaturaLinkCaracteres);
            Assert.Equal(10, ConstantsValues.MaximoDigitosExpiracaoLink);
            Assert.Equal(2, ConstantsValues.DivisorJanelaLinkArmazenamento);
            Assert.Equal(200, ConstantsValues.TamanhoMaximoChaveArmazenamento);
            Assert.Equal(32, ConstantsValues.TamanhoSegredoAssinaturaBytes);
        }

        [Fact]
        public void ArquivosTemporariosTemOsValoresDoContrato()
        {
            Assert.Equal(3, ConstantsValues.TentativasOperacaoArquivo);
            Assert.Equal(100, ConstantsValues.EsperaNovaTentativaArquivoMs);
            Assert.Equal(60, ConstantsValues.IdadeMinimaTemporarioOrfaoMinutos);
            Assert.Equal(81920, ConstantsValues.TamanhoBufferArquivoArmazenamento);
        }

        [Theory]
        [InlineData("1700000000")]
        [InlineData("1")]
        [InlineData("9999999999")]
        public void PadraoExpiracaoLink_CasaDigitosSemZeroAEsquerda(string valor)
        {
            Assert.Matches(ConstantsValues.PadraoExpiracaoLink, valor);
        }

        [Theory]
        [InlineData("1700000000\n")]
        [InlineData("1700000000\r\n")]
        [InlineData("01700000000")]
        [InlineData(" 1700000000")]
        [InlineData("1700000000 ")]
        [InlineData("17000000000")]
        [InlineData("0")]
        [InlineData("")]
        [InlineData("17a")]
        public void PadraoExpiracaoLink_NaoCasaQuebraDeLinhaZeroAEsquerdaNemOutroCaractere(string valor)
        {
            Assert.DoesNotMatch(ConstantsValues.PadraoExpiracaoLink, valor);
        }

        [Fact]
        public void MensagensDaRespostaHttpProvisorias_TemOTextoAprovadoAteAAprovacaoDoKaio()
        {
            Assert.Equal("Arquivo não encontrado.", ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado);
            Assert.Equal("O link do arquivo é inválido.", ConstantsMessage.ErroArmazenamentoLinkInvalido);
        }

        [Fact]
        public void ArmazenamentoDiscoOptions_ComecaVazio()
        {
            ArmazenamentoDiscoOptions opcoes = new ArmazenamentoDiscoOptions();

            Assert.Equal(string.Empty, opcoes.Diretorio);
            Assert.Equal(string.Empty, opcoes.UrlPublicaApi);
        }
    }
}
