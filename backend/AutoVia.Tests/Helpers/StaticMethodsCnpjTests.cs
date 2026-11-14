using AutoVia.Helpers;

namespace AutoVia.Tests.Helpers
{
    /// <summary>
    /// Testes do CNPJ em StaticMethods (P10-BE-01): digito verificador, mascara, sequencias repetidas, zeros e formatacao.
    /// Os vetores sao CNPJs sinteticos com digito verificador conferido a mao.
    /// </summary>
    public class StaticMethodsCnpjTests
    {
        private const string CnpjValido = "11222333000181";
        private const string CnpjValidoOutro = "11444777000161";

        [Theory]
        [InlineData("11222333000181")]
        [InlineData("11.222.333/0001-81")]
        [InlineData("11444777000161")]
        public void IsCnpjValido_DigitoVerificadorCorreto_Aceita(string cnpj)
        {
            Assert.True(StaticMethods.IsCnpjValido(cnpj));
        }

        [Theory]
        [InlineData("11222333000182")]
        [InlineData("11222333000191")]
        [InlineData("1122233300018")]
        [InlineData("112223330001811")]
        [InlineData("")]
        [InlineData("abc")]
        public void IsCnpjValido_DigitoErradoOuTamanhoErrado_Recusa(string cnpj)
        {
            Assert.False(StaticMethods.IsCnpjValido(cnpj));
        }

        [Theory]
        [InlineData("00000000000000")]
        [InlineData("11111111111111")]
        [InlineData("99999999999999")]
        public void IsCnpjValido_SequenciaDeUmDigitoRepetido_Recusa(string cnpj)
        {
            Assert.False(StaticMethods.IsCnpjValido(cnpj));
        }

        [Fact]
        public void IsCnpjValido_Nulo_Recusa()
        {
            Assert.False(StaticMethods.IsCnpjValido(null));
        }

        [Fact]
        public void FormateCnpj_CatorzeDigitos_FormataComMascara()
        {
            Assert.Equal("11.222.333/0001-81", StaticMethods.FormateCnpj(CnpjValido));
        }

        [Fact]
        public void FormateCnpj_SemCatorzeDigitos_DevolveOValorRecebido()
        {
            Assert.Equal("123", StaticMethods.FormateCnpj("123"));
            Assert.Equal(string.Empty, StaticMethods.FormateCnpj(null));
        }

        [Fact]
        public void IsCnpjValido_OutroVetorValido_AceitaOMesmoFormatoDeMascara()
        {
            Assert.Equal("11.444.777/0001-61", StaticMethods.FormateCnpj(CnpjValidoOutro));
            Assert.True(StaticMethods.IsCnpjValido("11.444.777/0001-61"));
        }
    }
}
