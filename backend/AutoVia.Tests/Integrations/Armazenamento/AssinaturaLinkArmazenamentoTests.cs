using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using System.Text.RegularExpressions;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes da assinatura do link (HMAC-SHA256, forma canonica, comparacao do texto), sem disco e sem relogio.</summary>
    public sealed class AssinaturaLinkArmazenamentoTests
    {
        private const string ChaveFoto = "veiculos/12/fotos/0f8fad5bd9cb469fa16570867728950e.jpg";
        private const long Expiracao = 1_700_000_000L;
        private const string AlfabetoBase64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private const string PadraoBase64Url = "^[A-Za-z0-9_-]{43}$";
        private const char CaractereForaDoAlfabeto = '!';
        private const int IndiceParaFlagDoBit = 1;
        private const char QuebraDeLinha = '\n';

        private static readonly byte[] Segredo = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };

        [Fact]
        public void Calcular_MesmaEntrada_MesmaAssinaturaDe43CaracteresBase64Url()
        {
            string primeira = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);
            string segunda = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);

            Assert.Equal(primeira, segunda);
            Assert.Matches(PadraoBase64Url, primeira);
            Assert.Equal(ConstantsValues.TamanhoAssinaturaLinkCaracteres, primeira.Length);
        }

        [Fact]
        public void Calcular_TrocarChaveExpiracaoOuSegredo_MudaAAssinatura()
        {
            string referencia = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);
            byte[] outroSegredo = (byte[])Segredo.Clone();
            outroSegredo[0] = 99;

            Assert.NotEqual(referencia, AssinaturaLinkArmazenamento.Calcular(Segredo, "veiculos/13/fotos/x.jpg", Expiracao));
            Assert.NotEqual(referencia, AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao + 1));
            Assert.NotEqual(referencia, AssinaturaLinkArmazenamento.Calcular(outroSegredo, ChaveFoto, Expiracao));
        }

        [Fact]
        public void Conferir_AssinaturaCanonica_Confere()
        {
            string assinatura = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);

            Assert.True(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, assinatura));
        }

        [Fact]
        public void Conferir_CaractereTrocado_TamanhoErrado_ForaDoAlfabeto_OuNula_NaoConfere()
        {
            string assinatura = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);
            string primeiroTrocado = (assinatura[0] == 'A' ? 'B' : 'A') + assinatura.Substring(1);
            string foraDoAlfabeto = CaractereForaDoAlfabeto + assinatura.Substring(1);

            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, primeiroTrocado));
            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, assinatura.Substring(1)));
            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, foraDoAlfabeto));
            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, null));
        }

        [Fact]
        public void Conferir_UltimoCaractereComOsDoisBitsQueSobram_NaoConfere()
        {
            string assinatura = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);
            int indiceUltimo = AlfabetoBase64Url.IndexOf(assinatura[assinatura.Length - 1]);
            char alternativa = AlfabetoBase64Url[indiceUltimo ^ IndiceParaFlagDoBit];
            string escritaNaoCanonica = assinatura.Substring(0, assinatura.Length - 1) + alternativa;

            Assert.NotEqual(assinatura, escritaNaoCanonica);
            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, escritaNaoCanonica));
        }

        [Fact]
        public void Conferir_AssinaturaCorretaSeguidaDeQuebraDeLinha_NaoConfere()
        {
            string assinatura = AssinaturaLinkArmazenamento.Calcular(Segredo, ChaveFoto, Expiracao);

            Assert.False(AssinaturaLinkArmazenamento.Conferir(Segredo, ChaveFoto, Expiracao, assinatura + QuebraDeLinha));
        }
    }
}
