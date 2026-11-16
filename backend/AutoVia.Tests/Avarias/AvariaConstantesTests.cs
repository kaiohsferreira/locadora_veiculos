using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;

namespace AutoVia.Tests.Avarias
{
    /// <summary>Coerencia das constantes de avaria com o contrato (teto do corpo e chaves do 502 de upload).</summary>
    public sealed class AvariaConstantesTests
    {
        [Fact]
        public void TetoDoCorpo_EhATamanhoDaFotoMaisAFolgaDoMultipart()
        {
            Assert.Equal(ConstantsArmazenamento.TamanhoMaximoFotoBytes + ConstantsValues.FolgaCorpoMultipartAvaria, ConstantsValues.TamanhoMaximoCorpoFotoAvaria);
        }

        [Fact]
        public void ValoresDoContrato_SaoOsDoAvariaContract()
        {
            Assert.Equal(10551296, ConstantsValues.TamanhoMaximoCorpoFotoAvaria);
            Assert.Equal(65536, ConstantsValues.FolgaCorpoMultipartAvaria);
            Assert.Equal("idAvaria", ConstantsValues.ExtensaoProblemaIdAvaria);
            Assert.Equal("retryUploadUrl", ConstantsValues.ExtensaoProblemaRetryUploadUrl);
            Assert.Equal("/api/vistorias/{0}/avarias/{1}/foto", ConstantsValues.FormatoRotaRetryFotoAvaria);
        }
    }
}
