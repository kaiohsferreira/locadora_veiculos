using AutoVia.Api.Requests.Armazenamento;
using AutoVia.Api.Validators;
using AutoVia.Helpers;
using FluentValidation.Results;
using System.Linq;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Validador do link de arquivo: formato de chave, exp e sig, quantidade de cada parametro e a quebra de linha final.
    /// Chave com quebra de linha final PASSA o validador (quem recusa e o servico, com 404). Toda falha de formato usa a mesma mensagem.
    /// </summary>
    public sealed class LerArquivoArmazenadoRequestValidatorTests
    {
        private const string ChaveValida = "veiculos/1/fotos/abc.jpg";
        private const string ExpValida = "1700000000";
        private const int QuantidadeUmaVez = 1;
        private const int QuantidadeDuasVezes = 2;
        private const int QuantidadeNenhuma = 0;
        private const int TamanhoChaveAcimaDoLimite = 201;

        private static readonly string SigValida = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres);

        private static LerArquivoArmazenadoRequest PedidoValido()
        {
            return new LerArquivoArmazenadoRequest
            {
                Chave = ChaveValida,
                Exp = ExpValida,
                Sig = SigValida,
                QuantidadeChave = QuantidadeUmaVez,
                QuantidadeExp = QuantidadeUmaVez,
                QuantidadeSig = QuantidadeUmaVez
            };
        }

        private static ValidationResult Validar(LerArquivoArmazenadoRequest pedido)
        {
            return new LerArquivoArmazenadoRequestValidator().Validate(pedido);
        }

        private static void AssertFalhaComMensagemDoLink(ValidationResult resultado)
        {
            Assert.False(resultado.IsValid);
            Assert.All(resultado.Errors, erro => Assert.Equal(ConstantsMessage.ErroArmazenamentoLinkInvalido, erro.ErrorMessage));
        }

        [Fact]
        public void PedidoValido_PassaNoValidador()
        {
            Assert.True(Validar(PedidoValido()).IsValid);
        }

        [Fact]
        public void ChaveComDuzentosEUmCaracteres_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Chave = new string('a', TamanhoChaveAcimaDoLimite);

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void ChaveVazia_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Chave = string.Empty;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void ChaveNula_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Chave = null;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void ChaveComQuebraDeLinhaFinal_PassaOValidadorPorqueQuemRecusaEOServico()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Chave = ChaveValida + "\n";

            Assert.True(Validar(pedido).IsValid);
        }

        [Theory]
        [InlineData("1700000000", true)]
        [InlineData("9999999999", true)]
        [InlineData("01700000000", false)]
        [InlineData("17000000000", false)]
        [InlineData("1700000000\n", false)]
        [InlineData("1700000000\r\n", false)]
        [InlineData(" 1700000000", false)]
        [InlineData("1700000000 ", false)]
        [InlineData("170000000a", false)]
        [InlineData("0", false)]
        [InlineData("", false)]
        public void Exp_SoDigitosSemZeroAEsquerdaSemQuebraDeLinha(string exp, bool esperadoValido)
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Exp = exp;

            ValidationResult resultado = Validar(pedido);

            Assert.Equal(esperadoValido, resultado.IsValid);
            if (!esperadoValido)
            {
                AssertFalhaComMensagemDoLink(resultado);
            }
        }

        [Fact]
        public void ExpNulo_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Exp = null;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void SigComQuarentaEDoisCaracteres_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 1);

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void SigComQuarentaEQuatroCaracteres_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres + 1);

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Theory]
        [InlineData('+')]
        [InlineData('/')]
        [InlineData('=')]
        [InlineData('\n')]
        public void SigComCaractereForaDoAlfabetoBase64Url_Falha(char caractere)
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = caractere + new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 1);

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void SigComQuarentaETresCaracteresSeguidosDeQuebraDeLinha_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = SigValida + "\n";

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void SigComQuarentaEDoisCaracteresMaisQuebraDeLinha_Falha()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = new string('A', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 1) + "\n";

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void SigComAlfabetoValidoEQuarentaETresCaracteres_Passa()
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.Sig = new string('a', ConstantsValues.TamanhoAssinaturaLinkCaracteres - 2) + "-_";

            Assert.True(Validar(pedido).IsValid);
        }

        [Theory]
        [InlineData(QuantidadeNenhuma)]
        [InlineData(QuantidadeDuasVezes)]
        public void ChaveRepetidaOuAusente_Falha(int quantidade)
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.QuantidadeChave = quantidade;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Theory]
        [InlineData(QuantidadeNenhuma)]
        [InlineData(QuantidadeDuasVezes)]
        public void ExpRepetidoOuAusente_Falha(int quantidade)
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.QuantidadeExp = quantidade;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Theory]
        [InlineData(QuantidadeNenhuma)]
        [InlineData(QuantidadeDuasVezes)]
        public void SigRepetidaOuAusente_Falha(int quantidade)
        {
            LerArquivoArmazenadoRequest pedido = PedidoValido();
            pedido.QuantidadeSig = quantidade;

            AssertFalhaComMensagemDoLink(Validar(pedido));
        }

        [Fact]
        public void TodasAsFalhasDeFormato_TemUmaMensagemSo()
        {
            LerArquivoArmazenadoRequest pedido = new LerArquivoArmazenadoRequest
            {
                Chave = null,
                Exp = "01",
                Sig = "x",
                QuantidadeChave = QuantidadeDuasVezes,
                QuantidadeExp = QuantidadeNenhuma,
                QuantidadeSig = QuantidadeDuasVezes
            };

            ValidationResult resultado = Validar(pedido);

            Assert.False(resultado.IsValid);
            Assert.Single(resultado.Errors.Select(erro => erro.ErrorMessage).Distinct());
        }
    }
}
