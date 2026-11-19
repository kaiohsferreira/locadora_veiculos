using AutoVia.Core.Regras;
using AutoVia.Helpers;
using System;
using System.IO;
using System.Text;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes diretos das regras puras do armazenamento em pasta (RegrasArmazenamentoDisco), sem disco e sem relogio real.</summary>
    public sealed class RegrasArmazenamentoDiscoTests
    {
        private const string ChaveFoto = "veiculos/12/fotos/0f8fad5bd9cb469fa16570867728950e.jpg";
        private const string ChaveAvaria = "vistorias/3/avarias/5/foto";
        private const long AgoraBase = 1_700_000_000L;
        private const long JanelaDeTesteNoInicio = 450L * 3_777_777L;

        [Theory]
        [InlineData(ChaveFoto)]
        [InlineData(ChaveAvaria)]
        public void ChaveSegura_ChavesEmUsoPassam(string chave)
        {
            Assert.True(RegrasArmazenamentoDisco.ChaveSegura(chave));
        }

        [Fact]
        public void ChaveSegura_ChaveNulaOuVaziaRecusada()
        {
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(null));
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(string.Empty));
        }

        [Fact]
        public void ChaveSegura_DuzentosCaracteresPassamYDuzentosEUmRecusa()
        {
            Assert.True(RegrasArmazenamentoDisco.ChaveSegura(new string('a', 200)));
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(new string('a', 201)));
        }

        [Theory]
        [InlineData("Veiculos/1/a.jpg")]
        [InlineData("veiculos/1 /a.jpg")]
        [InlineData("veiculos\\1\\a.jpg")]
        [InlineData("c:/a")]
        [InlineData("veiculos/%2e%2e/a")]
        [InlineData("/veiculos/1")]
        [InlineData("veiculos/1/")]
        [InlineData("veiculos//1")]
        [InlineData("..")]
        [InlineData("a/../b")]
        [InlineData("../a")]
        [InlineData("a/..")]
        [InlineData("a/.oculto")]
        [InlineData("a/fim.")]
        [InlineData("nul")]
        [InlineData("a/con.txt")]
        [InlineData("com1")]
        [InlineData("a/lpt9.jpg")]
        [InlineData("a/b\u0001c")]
        [InlineData("a/\u00e7.jpg")]
        [InlineData("a/b..c")]
        [InlineData("a/b\tc")]
        public void ChaveSegura_CasosDaListaRecusadaPassamRecusados(string chave)
        {
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(chave));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("\t")]
        [InlineData(" ")]
        public void ChaveSegura_ChaveValidaSeguidaDeSeparadorOuEspacoERecusada(string sufixo)
        {
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(ChaveFoto + sufixo));
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura(ChaveAvaria + sufixo));
            Assert.False(RegrasArmazenamentoDisco.ChaveSegura("veiculos" + sufixo + "/1/fotos/a.jpg"));
        }

        [Fact]
        public void ResolverCaminho_ChaveDentroDaPasta_DevolveCaminhoSobAPasta()
        {
            string pasta = Path.Combine(Path.GetTempPath(), "autovia-teste-regras", "arquivos");

            string? caminho = RegrasArmazenamentoDisco.ResolverCaminho(pasta, ChaveFoto);

            Assert.NotNull(caminho);
            Assert.StartsWith(Path.GetFullPath(pasta) + Path.DirectorySeparatorChar, caminho);
        }

        [Theory]
        [InlineData("../segredo/assinatura.key")]
        [InlineData("/etc/passwd")]
        [InlineData("a/../../x")]
        public void ResolverCaminho_ChaveQueEscaparia_DevolveNull(string chave)
        {
            string pasta = Path.Combine(Path.GetTempPath(), "autovia-teste-regras", "arquivos");

            Assert.Null(RegrasArmazenamentoDisco.ResolverCaminho(pasta, chave));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(60)]
        [InlineData(900)]
        public void CalcularExpiracaoUnix_RestanteFicaNaJanelaDeMetadeAteOPrazo(int prazo)
        {
            int janela = Math.Max(1, prazo / ConstantsValues.DivisorJanelaLinkArmazenamento);
            long minimo = prazo - janela + 1L;

            for (long agora = AgoraBase; agora < AgoraBase + 2000L; agora += 7L)
            {
                long expiracao = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(agora, prazo);
                long restante = expiracao - agora;

                Assert.InRange(restante, minimo, (long)prazo);
            }
        }

        [Fact]
        public void CalcularExpiracaoUnix_P900_MesmaJanelaDaMesmaExpiracaoEJanelaSeguinteOutra()
        {
            long inicio = JanelaDeTesteNoInicio;

            long expiracaoNoInicio = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(inicio, 900);
            long expiracaoNoFimDaJanela = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(inicio + 449L, 900);
            long expiracaoNaJanelaSeguinte = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(inicio + 450L, 900);

            Assert.Equal(expiracaoNoInicio, expiracaoNoFimDaJanela);
            Assert.NotEqual(expiracaoNoInicio, expiracaoNaJanelaSeguinte);
            Assert.True(expiracaoNaJanelaSeguinte > expiracaoNoInicio);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(60)]
        [InlineData(899)]
        [InlineData(900)]
        public void CalcularExpiracaoUnix_ExpiracaoNovaEMaiorQueAEmitidaAntes(int prazo)
        {
            long expiracaoEmitida = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(AgoraBase, prazo);

            for (long agora = AgoraBase; agora < AgoraBase + 5000L; agora++)
            {
                if (agora < expiracaoEmitida)
                {
                    continue;
                }

                long novaExpiracao = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(agora, prazo);
                Assert.True(novaExpiracao > expiracaoEmitida);
                expiracaoEmitida = novaExpiracao;
            }
        }

        [Theory]
        [InlineData("https://api.exemplo.com")]
        [InlineData("http://localhost:5080")]
        [InlineData("http://127.0.0.1:5080")]
        [InlineData("http://[::1]:5080")]
        public void EnderecoPublicoAceito_OrigemValidaPassa(string endereco)
        {
            Assert.True(RegrasArmazenamentoDisco.EnderecoPublicoAceito(endereco));
        }

        [Theory]
        [InlineData("http://api.exemplo.com")]
        [InlineData("https://api.exemplo.com/caminho")]
        [InlineData("https://api.exemplo.com?x=1")]
        [InlineData("https://api.exemplo.com#fragmento")]
        [InlineData("https://usuario@api.exemplo.com")]
        [InlineData("ftp://api.exemplo.com")]
        [InlineData("/relativo")]
        [InlineData("")]
        [InlineData(null)]
        public void EnderecoPublicoAceito_FormaOuEsquemaInvalidoRecusado(string? endereco)
        {
            Assert.False(RegrasArmazenamentoDisco.EnderecoPublicoAceito(endereco));
        }

        [Theory]
        [InlineData("localhost")]
        [InlineData("localhost.")]
        [InlineData("a.localhost")]
        [InlineData("a.b.localhost")]
        [InlineData("127.0.0.1")]
        [InlineData("127.5.5.5")]
        [InlineData("::1")]
        [InlineData("[::1]")]
        public void HostEhLoopback_EnderecosDeLoopbackVerdadeiros(string host)
        {
            Assert.True(RegrasArmazenamentoDisco.HostEhLoopback(host));
        }

        [Theory]
        [InlineData("x.localhostx.com")]
        [InlineData("localhost.exemplo.com")]
        [InlineData("api.exemplo.com")]
        [InlineData("")]
        public void HostEhLoopback_OutrosHostsFalsos(string host)
        {
            Assert.False(RegrasArmazenamentoDisco.HostEhLoopback(host));
        }

        [Fact]
        public void TipoPeloConteudo_JpegPngEPdfPelosPrimeirosBytes()
        {
            byte[] jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
            byte[] png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
            byte[] pdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };

            Assert.Equal(ConstantsValues.TipoConteudoJpeg, RegrasArmazenamentoDisco.TipoPeloConteudo(jpeg));
            Assert.Equal(ConstantsValues.TipoConteudoPng, RegrasArmazenamentoDisco.TipoPeloConteudo(png));
            Assert.Equal(ConstantsValues.TipoConteudoPdf, RegrasArmazenamentoDisco.TipoPeloConteudo(pdf));
        }

        [Fact]
        public void TipoPeloConteudo_PdfSoComOsQuatroPrimeirosBytesOuSemHifenNaoEPdf()
        {
            byte[] pdfCurto = new byte[] { 0x25, 0x50, 0x44, 0x46 };
            byte[] pdfSemHifen = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x31, 0x2E, 0x34 };

            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(pdfCurto));
            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(pdfSemHifen));
        }

        [Fact]
        public void TipoPeloConteudo_FormatosNaoAceitosEVetorCurtoDevolvemNull()
        {
            byte[] gif = new byte[] { 0x47, 0x49, 0x46, 0x38 };
            byte[] texto = Encoding.UTF8.GetBytes("<html>");
            byte[] curto = new byte[] { 0xFF, 0xD8 };

            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(gif));
            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(texto));
            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(curto));
            Assert.Null(RegrasArmazenamentoDisco.TipoPeloConteudo(Array.Empty<byte>()));
        }

        [Fact]
        public void CabecalhoLidoCobreAsTresAssinaturas()
        {
            Assert.True(ConstantsValues.AssinaturaPng.Length >= ConstantsValues.AssinaturaJpeg.Length);
            Assert.True(ConstantsValues.AssinaturaPng.Length >= ConstantsValues.AssinaturaPdf.Length);
        }

        [Fact]
        public void PastaRaizAceita_IgualDentroOuContendoAPublicacaoRecusadas()
        {
            string publicacao = Path.Combine(Path.GetTempPath(), "autovia-publicacao");
            string dentro = Path.Combine(publicacao, "arquivos");
            string contendo = Path.GetFullPath(Path.Combine(publicacao, ".."));

            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(publicacao, publicacao, _ => false));
            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(dentro, publicacao, _ => false));
            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(contendo, publicacao, _ => false));
        }

        [Fact]
        public void PastaRaizAceita_RaizDeDiscoRecusada()
        {
            string raiz = Path.GetPathRoot(Path.GetTempPath()) ?? string.Empty;
            string publicacao = Path.Combine(Path.GetTempPath(), "autovia-publicacao");

            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(raiz, publicacao, _ => false));
        }

        [Fact]
        public void PastaRaizAceita_PastaIrmaAceita()
        {
            string publicacao = Path.Combine(Path.GetTempPath(), "autovia-publicacao");
            string irma = Path.Combine(Path.GetTempPath(), "autovia-irma", "armazenamento");

            Assert.True(RegrasArmazenamentoDisco.PastaRaizAceita(irma, publicacao, _ => false));
        }

        [Fact]
        public void PastaRaizAceita_PastaComGitNaPropriaOuAcimaRecusada()
        {
            string publicacao = Path.Combine(Path.GetTempPath(), "autovia-publicacao");
            string repositorio = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "autovia-repo"));
            string raiz = Path.Combine(repositorio, "pasta", "armazenamento");
            string gitDoRepositorio = Path.Combine(repositorio, ".git");

            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(raiz, publicacao, caminho => string.Equals(caminho, gitDoRepositorio, StringComparison.OrdinalIgnoreCase)));
            Assert.False(RegrasArmazenamentoDisco.PastaRaizAceita(repositorio, publicacao, caminho => string.Equals(caminho, gitDoRepositorio, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
