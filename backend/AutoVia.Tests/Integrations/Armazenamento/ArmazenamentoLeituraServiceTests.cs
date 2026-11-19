using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>
    /// Testes do servico de leitura da rota publica: ordem de avaliacao, recusas com motivo, ETag e 304, arquivo fechado,
    /// falha nao prevista sanitizada e cancelamento. Relogio de teste; pasta temporaria propria.
    /// </summary>
    public sealed class ArmazenamentoLeituraServiceTests : IDisposable
    {
        private const string Chave = "veiculos/12/fotos/0f8fad5bd9cb469fa16570867728950e.jpg";
        private const string ChaveComQuebraDeLinha = Chave + "\n";
        private const string ChaveComTravessia = "veiculos/../segredo/assinatura.key";
        private const string NomeDaPastaDePublicacao = "publicacao";
        private const string NomeDaPastaDoArquivoFora = "fora";
        private const string NomeDaPastaRaizDeTeste = "armazenamento";
        private const string TextoNaoSuportado = "texto que nao e imagem";
        private const string PadraoDeEtag = "^[0-9a-f]+-[0-9a-f]+$";
        private const int QuantidadeDeCaracteresDaAssinaturaFalsa = ConstantsValues.TamanhoAssinaturaLinkCaracteres;
        private const char CaractereDaAssinaturaFalsa = 'A';
        private const int TamanhoDoJpegDeTeste = 12;
        private const int TamanhoDoJpegRegravado = 20;
        private const int PrimeiroIndiceDoCorpo = 3;
        private const byte MarcaJpeg1 = 0xFF;
        private const byte MarcaJpeg2 = 0xD8;
        private const byte MarcaJpeg3 = 0xFF;
        private const int ArredondamentoDeUmSegundo = 1;

        private readonly PastaTemporariaDeTeste _pasta = new PastaTemporariaDeTeste();
        private readonly string _raiz;
        private readonly FakeLogService _log = new FakeLogService();
        private readonly FakeTimeProvider _relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

        public ArmazenamentoLeituraServiceTests()
        {
            _raiz = Path.Combine(_pasta.Caminho, NomeDaPastaRaizDeTeste);
        }

        public void Dispose()
        {
            _pasta.Dispose();
        }

        [Fact]
        public async Task LinkValido_DevolveBytesEtagEFechaOArquivo()
        {
            byte[] conteudo = ConteudoJpeg(TamanhoDoJpegDeTeste);
            GravarArquivo(Chave, conteudo);
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            ArquivoParaLeitura leitura = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>());

            Assert.Equal(conteudo, leitura.Conteudo);
            Assert.Equal(ConstantsValues.TipoConteudoJpeg, leitura.ContentType);
            Assert.False(leitura.NaoModificado);
            Assert.Matches(PadraoDeEtag, leitura.ETag);
            using (FileStream exclusivo = new FileStream(CaminhoDoArquivo(Chave), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }

            Assert.Empty(_log.Entries);
        }

        [Fact]
        public async Task MesmoEtag_DevolveNaoModificadoSemBytes()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();
            ArquivoParaLeitura primeira = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>());

            ArquivoParaLeitura segunda = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string> { primeira.ETag });

            Assert.True(segunda.NaoModificado);
            Assert.Empty(segunda.Conteudo);
            Assert.Equal(primeira.ETag, segunda.ETag);
        }

        [Fact]
        public async Task CuringaOuEtagDiferente_DevolveOsBytes()
        {
            byte[] conteudo = ConteudoJpeg(TamanhoDoJpegDeTeste);
            GravarArquivo(Chave, conteudo);
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            ArquivoParaLeitura comCuringa = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string> { "*" });
            ArquivoParaLeitura comOutroEtag = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string> { "0-0" });

            Assert.False(comCuringa.NaoModificado);
            Assert.Equal(conteudo, comCuringa.Conteudo);
            Assert.False(comOutroEtag.NaoModificado);
            Assert.Equal(conteudo, comOutroEtag.Conteudo);
        }

        [Fact]
        public async Task RegravarNaMesmaChave_MudaOEtag()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();
            ArquivoParaLeitura antes = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>());

            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegRegravado));
            ArquivoParaLeitura depois = await servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string> { antes.ETag });

            Assert.False(depois.NaoModificado);
            Assert.NotEqual(antes.ETag, depois.ETag);
        }

        [Fact]
        public async Task AssinaturaErrada_RecusaComNotFoundERegistroSemChaveNemAssinatura()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();
            string assinaturaFalsa = new string(CaractereDaAssinaturaFalsa, QuantidadeDeCaracteresDaAssinaturaFalsa);

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, assinaturaFalsa, new List<string>()));

            Assert.Equal(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraAssinaturaInvalida));
            Assert.False(RegistroContem(Chave));
            Assert.False(RegistroContem(assinaturaFalsa));
        }

        [Fact]
        public async Task PrazoVencido_RecusaComNotFound()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();
            string assinatura = Assinatura(Chave, exp);
            _relogio.Avancar(TimeSpan.FromSeconds(ConstantsArmazenamento.ExpiracaoMaximaSegundos + ArredondamentoDeUmSegundo));

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, assinatura, new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraPrazoVencido));
        }

        [Fact]
        public async Task PrazoAcimaDoTeto_RecusaComNotFound()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = _relogio.GetUtcNow().ToUnixTimeSeconds() + ConstantsArmazenamento.ExpiracaoMaximaSegundos + ArredondamentoDeUmSegundo;

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraPrazoAcimaDoTeto));
        }

        [Theory]
        [InlineData(ChaveComTravessia)]
        [InlineData(ChaveComQuebraDeLinha)]
        public async Task ChaveInsegura_MesmoComAssinaturaCalculada_RecusaComNotFound(string chave)
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(chave, exp, Assinatura(chave, exp), new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraChaveInsegura));
        }

        [Fact]
        public async Task ArquivoAusente_RecusaComNotFound()
        {
            Directory.CreateDirectory(Path.Combine(_raiz, ConstantsValues.SubpastaArquivosArmazenamento));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraArquivoAusente));
        }

        [Fact]
        public async Task ConteudoNaoSuportado_RecusaComNotFound()
        {
            GravarArquivo(Chave, System.Text.Encoding.UTF8.GetBytes(TextoNaoSuportado));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraTipoNaoSuportado));
        }

        [Fact]
        public async Task ArquivoAcimaDoTamanhoMaximo_RecusaComNotFound()
        {
            byte[] grande = new byte[ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes + 1];
            byte[] cabecalho = ConteudoJpeg(TamanhoDoJpegDeTeste);
            Array.Copy(cabecalho, grande, cabecalho.Length);
            GravarArquivo(Chave, grande);
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraTamanhoAcimaDoMaximo));
        }

        [Fact]
        public async Task SemPastaNemSegredo_RecusaComNotFoundESemCriarNada()
        {
            string raizSemPasta = Path.Combine(_pasta.Caminho, NomeDaPastaDoArquivoFora);
            ArmazenamentoLeituraService servico = CriarServico(raizSemPasta);
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, new string(CaractereDaAssinaturaFalsa, QuantidadeDeCaracteresDaAssinaturaFalsa), new List<string>()));

            Assert.False(Directory.Exists(raizSemPasta));
        }

        [Fact]
        public async Task FalhaDePastaOuSegredo_VirariaNotFoundComUmRegistroSoComONomeDoTipo()
        {
            ArmazenamentoLeituraService servico = CriarServicoComSegredo(new SegredoQueLancaDisco());
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.Single(_log.Entries);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraFalhaInterna));
            Assert.True(RegistroContem(nameof(ArmazenamentoDiscoException)));
        }

        [Fact]
        public async Task FalhaNaoPrevista_LancaSanitizadaComUmRegistroSoComONomeDoTipo()
        {
            ArmazenamentoLeituraService servico = CriarServicoComSegredo(new SegredoQueLancaExcecaoComCaminho(Path.Combine(_raiz, Chave)));
            long exp = Expiracao();

            ArmazenamentoLeituraException ex = await Assert.ThrowsAsync<ArmazenamentoLeituraException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>()));

            Assert.Equal(nameof(InvalidOperationException), ex.Message);
            Assert.Null(ex.InnerException);
            Assert.Single(_log.Entries);
            Assert.True(RegistroContem(ConstantsMessage.ErroArmazenamentoAbrirArquivo));
            Assert.False(RegistroContem(_pasta.Caminho));
            Assert.False(RegistroContem(Chave));
        }

        [Fact]
        public async Task Cancelado_RelancaSemRegistro()
        {
            GravarArquivo(Chave, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();
            CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string>(), cancelamento.Token));

            Assert.Empty(_log.Entries);
        }

        [Fact]
        public async Task ArquivoPresenteSemSegredoNaMaquina_RecusaComAssinaturaInvalidaSemCriarNada()
        {
            string caminho = CaminhoDoArquivo(Chave);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            File.WriteAllBytes(caminho, ConteudoJpeg(TamanhoDoJpegDeTeste));
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            NotFoundException ex = await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, new string(CaractereDaAssinaturaFalsa, QuantidadeDeCaracteresDaAssinaturaFalsa), new List<string>()));

            Assert.Equal(ConstantsMessage.ErroArquivoArmazenadoNaoEncontrado, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraAssinaturaInvalida));
            Assert.False(File.Exists(CaminhoDoSegredo()));
            Assert.False(Directory.Exists(Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento)));
        }

        [Fact]
        public async Task PastaNoLugarDoArquivo_NaoAbreERecusaComArquivoAusente()
        {
            long exp = Expiracao();
            string assinatura = Assinatura(Chave, exp);
            Directory.CreateDirectory(CaminhoDoArquivo(Chave));
            ArmazenamentoLeituraService servico = CriarServico();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, assinatura, new List<string>()));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraArquivoAusente));
        }

        [Fact]
        public async Task ConteudoNaoSuportadoComEtagCertoNaLista_AindaRecusaComNotFound()
        {
            byte[] texto = System.Text.Encoding.UTF8.GetBytes(TextoNaoSuportado);
            GravarArquivo(Chave, texto);
            string caminho = CaminhoDoArquivo(Chave);
            // Mesmo ETag que o servico montaria para este arquivo: tamanho em hexadecimal, traco e instante da gravacao em hexadecimal.
            string etag = texto.Length.ToString(ConstantsValues.FormatoHexadecimalEtag, CultureInfo.InvariantCulture)
                + ConstantsValues.SeparadorEtagArmazenamento
                + File.GetLastWriteTimeUtc(caminho).Ticks.ToString(ConstantsValues.FormatoHexadecimalEtag, CultureInfo.InvariantCulture);
            ArmazenamentoLeituraService servico = CriarServico();
            long exp = Expiracao();

            await Assert.ThrowsAsync<NotFoundException>(() => servico.AbrirAsync(Chave, exp, Assinatura(Chave, exp), new List<string> { etag }));

            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoLeituraTipoNaoSuportado));
        }

        private string CaminhoDoSegredo()
        {
            return Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura);
        }

        private ArmazenamentoLeituraService CriarServico(string? raiz = null)
        {
            RaizArmazenamentoDisco disco = CriarRaiz(raiz ?? _raiz);
            SegredoAssinaturaArmazenamento segredo = new SegredoAssinaturaArmazenamento(disco, _log);
            return new ArmazenamentoLeituraService(disco, segredo, _relogio, _log);
        }

        private ArmazenamentoLeituraService CriarServicoComSegredo(ISegredoAssinaturaArmazenamento segredo)
        {
            RaizArmazenamentoDisco disco = CriarRaiz(_raiz);
            return new ArmazenamentoLeituraService(disco, segredo, _relogio, _log);
        }

        private RaizArmazenamentoDisco CriarRaiz(string raiz)
        {
            return new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions { Diretorio = raiz }),
                false,
                _pasta.Caminho,
                Path.Combine(_pasta.Caminho, NomeDaPastaDePublicacao),
                _log,
                true);
        }

        private void GravarArquivo(string chave, byte[] conteudo)
        {
            RaizArmazenamentoDisco disco = CriarRaiz(_raiz);
            string caminho = Path.Combine(disco.ObterPastaDosArquivos(), chave);
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            File.WriteAllBytes(caminho, conteudo);
        }

        private string CaminhoDoArquivo(string chave)
        {
            return Path.Combine(_raiz, ConstantsValues.SubpastaArquivosArmazenamento, chave);
        }

        private long Expiracao()
        {
            return RegrasArmazenamentoDisco.CalcularExpiracaoUnix(_relogio.GetUtcNow().ToUnixTimeSeconds(), ConstantsArmazenamento.ExpiracaoPadraoSegundos);
        }

        private string Assinatura(string chave, long exp)
        {
            RaizArmazenamentoDisco disco = CriarRaiz(_raiz);
            byte[] segredo = new SegredoAssinaturaArmazenamento(disco, _log).ObterOuCriar();
            return AssinaturaLinkArmazenamento.Calcular(segredo, chave, exp);
        }

        private bool RegistroContem(string texto)
        {
            return _log.Entries.Any(entrada => entrada.Contains(texto, StringComparison.Ordinal));
        }

        private static byte[] ConteudoJpeg(int tamanho)
        {
            byte[] conteudo = new byte[tamanho];
            conteudo[0] = MarcaJpeg1;
            conteudo[1] = MarcaJpeg2;
            conteudo[2] = MarcaJpeg3;
            for (int indice = PrimeiroIndiceDoCorpo; indice < conteudo.Length; indice++)
            {
                conteudo[indice] = (byte)indice;
            }

            return conteudo;
        }

        /// <summary>Segredo de teste que lanca a excecao de disco (pasta ou segredo) com o caminho na mensagem.</summary>
        private sealed class SegredoQueLancaDisco : ISegredoAssinaturaArmazenamento
        {
            public byte[] ObterOuCriar()
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura);
            }

            public byte[]? ObterSeExistir()
            {
                throw new ArmazenamentoDiscoException(ConstantsMessage.LogArmazenamentoDiscoSegredoAssinatura);
            }
        }

        /// <summary>Segredo de teste que lanca excecao nao prevista cuja mensagem tem caminho e chave.</summary>
        private sealed class SegredoQueLancaExcecaoComCaminho : ISegredoAssinaturaArmazenamento
        {
            private readonly string _mensagem;

            public SegredoQueLancaExcecaoComCaminho(string mensagem)
            {
                _mensagem = mensagem;
            }

            public byte[] ObterOuCriar()
            {
                throw new InvalidOperationException(_mensagem);
            }

            public byte[]? ObterSeExistir()
            {
                throw new InvalidOperationException(_mensagem);
            }
        }
    }
}
