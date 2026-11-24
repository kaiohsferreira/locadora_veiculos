using AutoVia.Communication.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Caucao;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Regra pura RegrasEntradaCaucao.PareceNumeroDeCartao (contrato caucao-retirada v1.1, CS-2) e o espelho dos limites do cartao:
    /// o validador (AutoVia.Communication) nao enxerga ConstantsCaucao (AutoVia.Integrations), entao ConstantsValues repete os
    /// valores e este teste compara um a um.
    /// </summary>
    public class RegrasEntradaCaucaoTests
    {
        [Theory]
        [InlineData("4111111111111111")]
        [InlineData("4111-1111-1111-1111")]
        [InlineData("4111_1111_1111_1111")]
        [InlineData("4111.1111.1111.1111")]
        [InlineData("4111:1111:1111:1111")]
        [InlineData("4111 1111 1111 1111")]
        [InlineData("123456789012")]
        [InlineData("1234567890123456789")]
        public void PareceNumeroDeCartao_SoDigitosDe12A19DepoisDosSeparadores_Verdadeiro(string texto)
        {
            Assert.True(RegrasEntradaCaucao.PareceNumeroDeCartao(texto));
        }

        [Theory]
        [InlineData("12345678901")]
        [InlineData("12345678901234567890")]
        [InlineData("FAKE-0123456789abcdef0123456789abcdef")]
        [InlineData("FAKE-RECUSADO")]
        [InlineData("FAKE-ANALISE")]
        [InlineData("4111111111a11111")]
        [InlineData("FAKE-4111111111111111")]
        [InlineData("")]
        [InlineData("-_.: ")]
        [InlineData(null)]
        public void PareceNumeroDeCartao_ForaDaRegra_Falso(string? texto)
        {
            Assert.False(RegrasEntradaCaucao.PareceNumeroDeCartao(texto));
        }

        /// <summary>Digito de outro alfabeto (arabe-indico) nao conta como digito: a regra so olha 0 a 9.</summary>
        [Fact]
        public void PareceNumeroDeCartao_DigitosForaDoAscii_Falso()
        {
            Assert.False(RegrasEntradaCaucao.PareceNumeroDeCartao("١٢٣٤٥٦٧٨٩١٢٣"));
        }

        [Fact]
        public void LimitesDoCartaoNaEntrada_IguaisAosDoGateway()
        {
            Assert.Equal(ConstantsCaucao.TokenTamanhoMinimo, ConstantsValues.CaucaoTokenTamanhoMinimo);
            Assert.Equal(ConstantsCaucao.TokenTamanhoMaximo, ConstantsValues.CaucaoTokenTamanhoMaximo);
            Assert.Equal(ConstantsCaucao.PadraoToken, ConstantsValues.CaucaoPadraoToken);
            Assert.Equal(ConstantsCaucao.MetodoPagamentoTamanhoMinimo, ConstantsValues.CaucaoMetodoPagamentoTamanhoMinimo);
            Assert.Equal(ConstantsCaucao.MetodoPagamentoTamanhoMaximo, ConstantsValues.CaucaoMetodoPagamentoTamanhoMaximo);
            Assert.Equal(ConstantsCaucao.PadraoMetodoPagamento, ConstantsValues.CaucaoPadraoMetodoPagamento);
            Assert.Equal(ConstantsCaucao.PadraoDispositivo, ConstantsValues.CaucaoPadraoDispositivo);
        }

        [Fact]
        public void EmissorMaximo_EhOMaiorInt()
        {
            Assert.Equal(2147483647L, ConstantsValues.CaucaoEmissorIdMaximo);
        }
    }
}
