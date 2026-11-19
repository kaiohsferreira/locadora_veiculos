using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace AutoVia.Tests.Integrations.Armazenamento
{
    /// <summary>Testes do provedor DISCO_LOCAL: gravacao atomica, recusas com motivo no log, link, lote e remocao, numa pasta temporaria.</summary>
    public sealed class DiscoArmazenamentoProviderTests : IDisposable
    {
        private const string Chave = "veiculos/12/fotos/0f8fad5bd9cb469fa16570867728950e.jpg";
        private const string ChavePdf = "clientes/7/documentos/cnh-teste.pdf";
        private const string ChaveDeOutroVeiculo = "veiculos/9/fotos/outro.jpg";
        private const string ChaveInsegura = "../segredo/assinatura.key";
        private const string ChaveDeAvariaSemPasta = "vistorias/3/avarias/5/foto";
        private const string NomeDaPastaDeVistorias = "vistorias";
        private const string TipoJpeg = ConstantsValues.TipoConteudoJpeg;
        private const string TipoPng = ConstantsValues.TipoConteudoPng;
        private const string TipoPdf = "application/pdf";
        private const string UrlPublicaDeTeste = "http://localhost:5080";
        private const string NomeDaPastaDePublicacao = "publicacao";
        private const string NomeDaPastaDeArquivos = "arquivos";
        private const string NomeDoBucket = "bucket-de-teste";
        private const string NomeDoParametroExpiracao = "exp";
        private const string NomeDoParametroAssinatura = "sig";
        private const string NomeDoParametroChave = "chave";
        private const string Interrogacao = "?";
        private const string Igual = "=";
        private const string SeparadorParametros = "&";
        private const string ConteudoDeTeste = "conteudo-de-teste";
        private const int Expiracao = ConstantsArmazenamento.ExpiracaoPadraoSegundos;
        private const int QuantidadeUmaUnidade = 1;
        private const int TamanhoDoJpegDeTeste = 8;
        private const int PrimeiroIndiceDoCorpo = 3;
        private const byte MarcaJpeg1 = 0xFF;
        private const byte MarcaJpeg2 = 0xD8;
        private const byte MarcaJpeg3 = 0xFF;
        private const byte ValorAlternativoDoCorpo = 0x42;
        private const int RodadasSubstituirERemover = 10;
        private const int IntervaloDeVerificacaoMs = 5;
        private const int LimiteDeEsperaMs = 5000;
        private const FileShare CompartilhamentoDoLeitor = FileShare.ReadWrite | FileShare.Delete;

        private readonly PastaTemporariaDeTeste _pasta = new PastaTemporariaDeTeste();
        private readonly string _raiz;
        private readonly FakeLogService _log = new FakeLogService();
        private readonly FakeTimeProvider _relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.Zero));

        public DiscoArmazenamentoProviderTests()
        {
            _raiz = Path.Combine(_pasta.Caminho, "armazenamento");
        }

        public void Dispose()
        {
            _pasta.Dispose();
        }

        [Fact]
        public void Nome_EhDISCO_LOCAL()
        {
            Assert.Equal(ConstantsArmazenamento.ProvedorDiscoLocal, CriarProvedor().Nome);
        }

        [Fact]
        public async Task EnviarAsync_Jpeg_GravaOArquivoEDevolveLinkComAssinaturaCanonica()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            ArquivoArmazenado arquivo = await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);

            Assert.Equal(ConteudoJpeg(), File.ReadAllBytes(Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave)));
            long agora = _relogio.GetUtcNow().ToUnixTimeSeconds();
            long expEsperado = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(agora, Expiracao);
            byte[] segredo = File.ReadAllBytes(Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura));
            Assert.StartsWith(UrlPublicaDeTeste + ConstantsValues.CaminhoRaizEnderecoPublico + ConstantsValues.RotaLeituraArquivoArmazenado + Interrogacao, arquivo.Url);
            Assert.Equal(expEsperado.ToString(CultureInfo.InvariantCulture), Parametro(arquivo.Url, NomeDoParametroExpiracao));
            Assert.Equal(AssinaturaLinkArmazenamento.Calcular(segredo, Chave, expEsperado), Parametro(arquivo.Url, NomeDoParametroAssinatura));
            Assert.Equal(Uri.EscapeDataString(Chave), Parametro(arquivo.Url, NomeDoParametroChave));
            Assert.Equal(expEsperado, arquivo.ExpiraEm.ToUnixTimeSeconds());
            Assert.Empty(TemporariosNaPasta());
        }

        [Fact]
        public async Task EnviarAsync_Png_EhAceito()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            byte[] png = ConstantsValues.AssinaturaPng.Concat(new byte[] { 1, 2, 3 }).ToArray();

            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, png, TipoPng, Expiracao);

            Assert.Equal(png, File.ReadAllBytes(Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave)));
        }

        [Fact]
        public async Task EnviarAsync_Pdf_EhAceitoEGravaOsMesmosBytes()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            byte[] pdf = ConstantsValues.AssinaturaPdf.Concat(new byte[] { 1, 2, 3 }).ToArray();

            ArquivoArmazenado arquivo = await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, ChavePdf, pdf, TipoPdf, Expiracao);

            Assert.Equal(pdf, File.ReadAllBytes(Path.Combine(_raiz, NomeDaPastaDeArquivos, ChavePdf)));
            Assert.StartsWith(UrlPublicaDeTeste, arquivo.Url);
            Assert.Empty(_log.Entries);
        }

        [Theory]
        [InlineData("GIF89a conteudo", "image/gif")]
        [InlineData("<html>conteudo", "text/plain")]
        [InlineData("%PDF conteudo sem o hifen", TipoPdf)]
        public async Task EnviarAsync_FormatoNaoAceito_RecusaED08ComMotivoNoLogESemGravar(string conteudo, string tipo)
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, Encoding.UTF8.GetBytes(conteudo), tipo, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoTipoNaoSuportado));
            Assert.False(File.Exists(Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave)));
        }

        [Fact]
        public async Task EnviarAsync_PdfDeclaradoComoJpeg_RecusaED08()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            byte[] pdf = ConstantsValues.AssinaturaPdf.Concat(new byte[] { 1, 2, 3 }).ToArray();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, ChavePdf, pdf, TipoJpeg, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoTipoNaoSuportado));
        }

        [Fact]
        public async Task EnviarAsync_JpegDeclaradoComoPdf_RecusaED08()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, ChavePdf, ConteudoJpeg(), TipoPdf, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoTipoNaoSuportado));
        }

        [Fact]
        public async Task EnviarAsync_TipoDeclaradoDiferenteDoConteudo_RecusaED08()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoPng, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoTipoNaoSuportado));
        }

        [Fact]
        public async Task EnviarAsync_ChaveInsegura_RecusaED07SemCriarPasta()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, ChaveInsegura, ConteudoJpeg(), TipoJpeg, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoChaveInvalida));
            Assert.False(Directory.Exists(_raiz));
        }

        [Fact]
        public async Task EnviarAsync_ConteudoVazio_RecusaED09ComNomeDoTipoNoLog()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, Array.Empty<byte>(), TipoJpeg, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(nameof(ArgumentException)));
        }

        [Fact]
        public async Task EnviarAsync_AcimaDoTamanhoMaximo_RecusaED08()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            byte[] grande = new byte[ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes + 1];
            byte[] cabecalho = ConteudoJpeg();
            Array.Copy(cabecalho, grande, cabecalho.Length);

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, grande, TipoJpeg, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(ConstantsMessage.LogArmazenamentoDiscoTamanhoAcimaDoMaximo));
        }

        [Fact]
        public async Task EnviarAsync_FalhaDeEntradaESaida_RecusaED09SemCaminhoNoLogETemporarioApagado()
        {
            Directory.CreateDirectory(Path.Combine(_raiz, NomeDaPastaDeArquivos));
            File.WriteAllText(Path.Combine(_raiz, NomeDaPastaDeArquivos, "veiculos"), ConteudoDeTeste);
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IntegrationException ex = await Assert.ThrowsAsync<IntegrationException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao));

            Assert.Equal(ConstantsMessage.ErroArmazenamentoEnviar, ex.Message);
            Assert.True(RegistroContem(nameof(IOException)));
            Assert.False(RegistroContemTexto(_pasta.Caminho));
            Assert.Empty(TemporariosNaPasta());
        }

        [Fact]
        public async Task EnviarAsync_Cancelado_RelancaSemRegistro()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao, cancelamento.Token));

            Assert.Empty(_log.Entries);
        }

        [Fact]
        public async Task ObterUrlAsync_SemArquivo_DevolveNuloSemCriarPastaNemSegredo()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            ArquivoArmazenado? arquivo = await provedor.ObterUrlAsync(CriarConfiguracao(), NomeDoBucket, Chave, Expiracao);

            Assert.Null(arquivo);
            Assert.False(Directory.Exists(_raiz));
            Assert.Empty(_log.Entries);
        }

        [Fact]
        public async Task ObterUrlAsync_ArquivoSemSegredo_DevolveNuloComRegistroESemCriarSegredo()
        {
            Directory.CreateDirectory(Path.Combine(_raiz, NomeDaPastaDeArquivos, "veiculos", "12", "fotos"));
            File.WriteAllBytes(Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave), ConteudoJpeg());
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            ArquivoArmazenado? arquivo = await provedor.ObterUrlAsync(CriarConfiguracao(), NomeDoBucket, Chave, Expiracao);

            Assert.Null(arquivo);
            Assert.True(RegistroContemTexto(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoEmissaoSemSegredo, QuantidadeUmaUnidade)));
            Assert.False(File.Exists(Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura)));
        }

        [Fact]
        public async Task ObterUrlAsync_ComArquivoESegredo_DevolveLink()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);

            ArquivoArmazenado? arquivo = await provedor.ObterUrlAsync(CriarConfiguracao(), NomeDoBucket, Chave, Expiracao);

            Assert.NotNull(arquivo);
            Assert.StartsWith(UrlPublicaDeTeste, arquivo!.Url);
        }

        [Fact]
        public async Task AssinarUrlsAsync_ListaVazia_NaoTocaNaPasta()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await provedor.AssinarUrlsAsync(CriarConfiguracao(), NomeDoBucket, new List<string>(), Expiracao);

            Assert.Empty(lote);
            Assert.False(Directory.Exists(_raiz));
        }

        [Fact]
        public async Task AssinarUrlsAsync_ChaveRepetidaViraUmaEntradaEAusenteEOmitidaComUmRegistroDeLote()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            List<string> chaves = new List<string> { Chave, Chave, ChaveDeOutroVeiculo };

            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await provedor.AssinarUrlsAsync(CriarConfiguracao(), NomeDoBucket, chaves, Expiracao);

            Assert.Single(lote);
            Assert.True(lote.ContainsKey(Chave));
            Assert.Single(_log.Entries);
            Assert.True(RegistroContemTexto(string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLoteChavesOmitidas, QuantidadeUmaUnidade, 0)));
        }

        [Fact]
        public async Task RemoverAsync_ApagaOArquivoERepetirNaoFalha()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            string caminho = Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave);

            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);
            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);

            Assert.False(File.Exists(caminho));
        }

        [Fact]
        public async Task RemoverAsync_SemPasta_NaoFalhaENaoCriaNada()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);

            Assert.False(Directory.Exists(_raiz));
        }

        [Fact]
        public async Task RemoverAsync_SubpastaDaChaveAusenteComRaizEPastaDeArquivosExistentes_NaoLancaNaoRegistraENaoCriaNada()
        {
            Directory.CreateDirectory(Path.Combine(_raiz, NomeDaPastaDeArquivos));
            DiscoArmazenamentoProvider provedor = CriarProvedor();

            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);

            Assert.Empty(_log.Entries);
            Assert.False(Directory.Exists(Path.Combine(_raiz, NomeDaPastaDeArquivos, "veiculos")));
            Assert.False(Directory.Exists(Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento)));
            Assert.False(Directory.Exists(Path.Combine(_raiz, ConstantsValues.SubpastaTemporariosArmazenamento)));
        }

        [Fact]
        public async Task RemoverAsync_ChaveDeAvariaComPastaDaAvariaAusente_NaoLancaNaoRegistraENaoCriaNada()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);

            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, ChaveDeAvariaSemPasta);

            Assert.Empty(_log.Entries);
            Assert.True(File.Exists(CaminhoDoArquivo()));
            Assert.False(Directory.Exists(Path.Combine(_raiz, NomeDaPastaDeArquivos, NomeDaPastaDeVistorias)));
        }

        [Fact]
        public async Task EnviarAsync_FalhaIntermediariaDeArquivo_RegistraSoONomeDoTipoEGravaNaSegundaTentativa()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            byte[] novo = ConteudoJpeg();
            novo[novo.Length - 1] = ValorAlternativoDoCorpo;
            FileStream bloqueio = new FileStream(CaminhoDoArquivo(), FileMode.Open, FileAccess.Read, FileShare.None);
            try
            {
                Task<ArquivoArmazenado> envio = provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, novo, TipoJpeg, Expiracao);
                await EsperarPrimeiroRegistroAsync();
                bloqueio.Dispose();
                await envio;
            }
            finally
            {
                bloqueio.Dispose();
            }

            Assert.NotEmpty(_log.Entries);
            Assert.All(_log.Entries, entrada => Assert.Contains(entrada, new[] { nameof(IOException), nameof(UnauthorizedAccessException) }));
            Assert.Equal(novo, File.ReadAllBytes(CaminhoDoArquivo()));
            Assert.Empty(TemporariosNaPasta());
        }

        [Fact]
        public async Task EnviarAsync_CancelamentoNoMeioDaGravacao_NaoDeixaTemporarioNemRegistroDeErro()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            using FileStream bloqueio = new FileStream(CaminhoDoArquivo(), FileMode.Open, FileAccess.Read, FileShare.None);
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            Task<ArquivoArmazenado> envio = provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao, cancelamento.Token);

            await EsperarTemporarioOuFimAsync(envio);
            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => envio);
            Assert.Empty(TemporariosNaPasta());
            Assert.All(_log.Entries, entrada => Assert.Contains(entrada, new[] { nameof(IOException), nameof(UnauthorizedAccessException) }));
        }

        [Fact]
        public async Task EnviarAsync_DuasGravacoesSimultaneasNaMesmaChave_TerminamComUmArquivoInteiroESemTemporario()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            byte[] primeiro = ConteudoJpeg();
            byte[] segundo = ConteudoJpeg();
            segundo[segundo.Length - 1] = ValorAlternativoDoCorpo;

            Task<ArquivoArmazenado> envioA = provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, primeiro, TipoJpeg, Expiracao);
            Task<ArquivoArmazenado> envioB = provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, segundo, TipoJpeg, Expiracao);
            await Task.WhenAll(envioA, envioB);

            byte[] gravado = File.ReadAllBytes(CaminhoDoArquivo());
            Assert.True(gravado.SequenceEqual(primeiro) || gravado.SequenceEqual(segundo));
            Assert.Empty(TemporariosNaPasta());
        }

        [Fact]
        public async Task SubstituirERemover_EnquantoOutraTarefaLeOArquivo_NaoFalham()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            RaizArmazenamentoDisco raiz = CriarRaiz();
            ArmazenamentoLeituraService leitura = new ArmazenamentoLeituraService(raiz, new SegredoAssinaturaArmazenamento(raiz, _log), _relogio, _log);
            long exp = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(_relogio.GetUtcNow().ToUnixTimeSeconds(), Expiracao);
            string assinatura = AssinaturaLinkArmazenamento.Calcular(File.ReadAllBytes(CaminhoDoSegredo()), Chave, exp);

            for (int rodada = 0; rodada < RodadasSubstituirERemover; rodada++)
            {
                Task leituraDuranteASubstituicao = LerPeloServicoDuranteATrocaAsync(leitura, exp, assinatura);
                await SubstituirComLeitorAbertoAsync(provedor);
                await leituraDuranteASubstituicao;

                Task leituraDuranteAExclusao = LerPeloServicoDuranteATrocaAsync(leitura, exp, assinatura);
                await RemoverComLeitorAbertoAsync(provedor);
                await leituraDuranteAExclusao;

                await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            }

            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);
            Assert.False(File.Exists(CaminhoDoArquivo()));
            Assert.Empty(TemporariosNaPasta());
        }

        [Fact]
        public async Task AssinarUrlsAsync_SoComChavesAusentes_NaoLeEnderecoNemSegredo()
        {
            Directory.CreateDirectory(Path.Combine(_raiz, NomeDaPastaDeArquivos));
            DiscoArmazenamentoProvider provedor = CriarProvedorCom(new SegredoQueNaoDeveSerLido(), new EnderecoQueNaoDeveSerLido());

            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await provedor.AssinarUrlsAsync(CriarConfiguracao(), NomeDoBucket, new List<string> { Chave, ChaveDeOutroVeiculo }, Expiracao);

            Assert.Empty(lote);
            Assert.False(File.Exists(CaminhoDoSegredo()));
        }

        [Fact]
        public async Task AssinarUrlsAsync_DuasChavesPresentes_TodasComAMesmaExpiracao()
        {
            DiscoArmazenamentoProvider provedor = CriarProvedor();
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);
            await provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, ChaveDeOutroVeiculo, ConteudoJpeg(), TipoJpeg, Expiracao);

            IReadOnlyDictionary<string, ArquivoArmazenado> lote = await provedor.AssinarUrlsAsync(CriarConfiguracao(), NomeDoBucket, new List<string> { Chave, ChaveDeOutroVeiculo }, Expiracao);

            long expEsperado = RegrasArmazenamentoDisco.CalcularExpiracaoUnix(_relogio.GetUtcNow().ToUnixTimeSeconds(), Expiracao);
            Assert.Equal(2, lote.Count);
            Assert.Equal(expEsperado.ToString(CultureInfo.InvariantCulture), Parametro(lote[Chave].Url, NomeDoParametroExpiracao));
            Assert.Equal(expEsperado.ToString(CultureInfo.InvariantCulture), Parametro(lote[ChaveDeOutroVeiculo].Url, NomeDoParametroExpiracao));
        }

        /// <summary>Espera, com limite, o primeiro registro do log (a falha intermediaria ja aconteceu).</summary>
        private async Task EsperarPrimeiroRegistroAsync()
        {
            for (int espera = 0; _log.Entries.Count == 0 && espera < LimiteDeEsperaMs; espera += IntervaloDeVerificacaoMs)
            {
                await Task.Delay(IntervaloDeVerificacaoMs);
            }
        }

        /// <summary>Espera, com limite, o temporario aparecer (a gravacao comecou) ou a tarefa terminar.</summary>
        private async Task EsperarTemporarioOuFimAsync(Task tarefa)
        {
            for (int espera = 0; !TemporariosNaPasta().Any() && !tarefa.IsCompleted && espera < LimiteDeEsperaMs; espera += IntervaloDeVerificacaoMs)
            {
                await Task.Delay(IntervaloDeVerificacaoMs);
            }
        }

        /// <summary>
        /// Substitui o arquivo enquanto um leitor o mantem aberto no meio de uma leitura (mesmo compartilhamento do servico de leitura).
        /// So solta o leitor depois da primeira tentativa falha: a nova tentativa, com o leitor ja solto, precisa gravar.
        /// </summary>
        private async Task SubstituirComLeitorAbertoAsync(DiscoArmazenamentoProvider provedor)
        {
            using FileStream leitor = AbrirComoLeitorDoServico();
            int registrosAntes = _log.Entries.Count;
            Task<ArquivoArmazenado> envio = provedor.EnviarAsync(CriarConfiguracao(), NomeDoBucket, Chave, ConteudoJpeg(), TipoJpeg, Expiracao);

            await EsperarNovoRegistroAsync(registrosAntes, envio);
            bool houveFalhaIntermediaria = _log.Entries.Count > registrosAntes;
            leitor.Dispose();
            await envio;
            Assert.True(houveFalhaIntermediaria);
        }

        /// <summary>Remove o arquivo com um leitor aberto: a exclusao com compartilhamento de exclusao tambem nao pode falhar.</summary>
        private async Task RemoverComLeitorAbertoAsync(DiscoArmazenamentoProvider provedor)
        {
            using FileStream leitor = AbrirComoLeitorDoServico();
            await provedor.RemoverAsync(CriarConfiguracao(), NomeDoBucket, Chave);
        }

        /// <summary>
        /// Le pelo servico de leitura enquanto o arquivo e trocado ou removido. O resultado nao depende do instante: ou sai o conteudo
        /// inteiro (o mesmo nas duas versoes) ou a recusa 404. Qualquer outra excecao falha o teste.
        /// </summary>
        private async Task LerPeloServicoDuranteATrocaAsync(ArmazenamentoLeituraService leitura, long exp, string assinatura)
        {
            try
            {
                ArquivoParaLeitura lido = await leitura.AbrirAsync(Chave, exp, assinatura, new List<string>());
                Assert.Equal(ConteudoJpeg(), lido.Conteudo);
            }
            catch (NotFoundException)
            {
                // Arquivo ausente no instante da leitura: a recusa 404 e o resultado esperado.
            }
        }

        /// <summary>Abre o arquivo como o servico de leitura abre (leitura, compartilhamento de escrita e exclusao) e le tudo para a memoria; o identificador fica aberto.</summary>
        private FileStream AbrirComoLeitorDoServico()
        {
            FileStream leitor = new FileStream(CaminhoDoArquivo(), FileMode.Open, FileAccess.Read, CompartilhamentoDoLeitor);
            byte[] conteudo = new byte[(int)leitor.Length];
            leitor.ReadExactly(conteudo);
            return leitor;
        }

        /// <summary>Espera, com limite, um registro novo no log (falha intermediaria) ou o fim da tarefa, o que vier primeiro.</summary>
        private async Task EsperarNovoRegistroAsync(int registrosAntes, Task tarefa)
        {
            for (int espera = 0; _log.Entries.Count == registrosAntes && !tarefa.IsCompleted && espera < LimiteDeEsperaMs; espera += IntervaloDeVerificacaoMs)
            {
                await Task.Delay(IntervaloDeVerificacaoMs);
            }
        }

        private RaizArmazenamentoDisco CriarRaiz()
        {
            return new RaizArmazenamentoDisco(
                Options.Create(new ArmazenamentoDiscoOptions { Diretorio = _raiz }),
                false,
                _pasta.Caminho,
                Path.Combine(_pasta.Caminho, NomeDaPastaDePublicacao),
                _log,
                true);
        }

        private DiscoArmazenamentoProvider CriarProvedor()
        {
            RaizArmazenamentoDisco raiz = CriarRaiz();
            SegredoAssinaturaArmazenamento segredo = new SegredoAssinaturaArmazenamento(raiz, _log);
            EnderecoPublicoApi endereco = new EnderecoPublicoApi(
                Options.Create(new ArmazenamentoDiscoOptions { UrlPublicaApi = UrlPublicaDeTeste }),
                new HttpContextAccessor());
            return new DiscoArmazenamentoProvider(raiz, segredo, endereco, _relogio, _log);
        }

        private DiscoArmazenamentoProvider CriarProvedorCom(ISegredoAssinaturaArmazenamento segredo, IEnderecoPublicoApi endereco)
        {
            return new DiscoArmazenamentoProvider(CriarRaiz(), segredo, endereco, _relogio, _log);
        }

        private string CaminhoDoArquivo()
        {
            return Path.Combine(_raiz, NomeDaPastaDeArquivos, Chave);
        }

        private string CaminhoDoSegredo()
        {
            return Path.Combine(_raiz, ConstantsValues.SubpastaSegredoArmazenamento, ConstantsValues.ArquivoSegredoAssinatura);
        }

        /// <summary>Segredo de teste que falha se for lido: o lote so com chaves ausentes nao pode tocar nele.</summary>
        private sealed class SegredoQueNaoDeveSerLido : ISegredoAssinaturaArmazenamento
        {
            public byte[] ObterOuCriar()
            {
                throw new InvalidOperationException();
            }

            public byte[]? ObterSeExistir()
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>Endereco de teste que falha se for lido: o lote so com chaves ausentes nao pode tocar nele.</summary>
        private sealed class EnderecoQueNaoDeveSerLido : IEnderecoPublicoApi
        {
            public string Obter()
            {
                throw new InvalidOperationException();
            }
        }

        private static ConfiguracaoIntegracao CriarConfiguracao()
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Armazenamento,
                ConstantsIntegration.Armazenamento,
                AmbienteIntegracao.SANDBOX,
                "https://storage.example",
                1000,
                new Dictionary<string, IntegrationSettingValue>());
        }

        private static byte[] ConteudoJpeg()
        {
            byte[] conteudo = new byte[TamanhoDoJpegDeTeste];
            conteudo[0] = MarcaJpeg1;
            conteudo[1] = MarcaJpeg2;
            conteudo[2] = MarcaJpeg3;
            for (int indice = PrimeiroIndiceDoCorpo; indice < conteudo.Length; indice++)
            {
                conteudo[indice] = (byte)indice;
            }

            return conteudo;
        }

        private IEnumerable<string> TemporariosNaPasta()
        {
            string pasta = Path.Combine(_raiz, ConstantsValues.SubpastaTemporariosArmazenamento);
            return Directory.Exists(pasta) ? Directory.EnumerateFiles(pasta).ToList() : new List<string>();
        }

        private bool RegistroContem(string texto)
        {
            return RegistroContemTexto(texto);
        }

        private bool RegistroContemTexto(string texto)
        {
            return _log.Entries.Any(entrada => entrada.Contains(texto, StringComparison.Ordinal));
        }

        private static string Parametro(string url, string nome)
        {
            string query = url.Substring(url.IndexOf(Interrogacao, StringComparison.Ordinal) + 1);
            foreach (string par in query.Split(SeparadorParametros))
            {
                string[] partes = par.Split(Igual);
                if (partes[0] == nome)
                {
                    return partes[1];
                }
            }

            return string.Empty;
        }
    }
}
