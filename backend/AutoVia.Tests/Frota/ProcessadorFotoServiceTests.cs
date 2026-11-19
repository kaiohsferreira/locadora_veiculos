using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Tests.Fakes;
using FluentValidation;
using FluentValidation.Results;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Processador de foto com imagens hostis e imagens validas, todas geradas em memoria. Cobre as recusas (ValidationException com a
    /// mensagem certa), a saida (JPEG sem EXIF, orientacao, reducao, fundo branco), o limite de 2 simultaneos, a fila de espera
    /// (teto de 8 chamadas e recusa na hora), o tempo limite da espera, a liberacao da vaga e o cancelamento.
    /// Os testes de fila e de tempo limite nao dependem do relogio: o resultado vem do estado do semaforo, que o teste controla.
    /// </summary>
    public sealed class ProcessadorFotoServiceTests
    {
        private static readonly byte[] BytesDePng = ImagensDeTesteFoto.GerarPng(16, 16, SKColors.Red);
        private static readonly byte[] BytesDeJpeg = ImagensDeTesteFoto.GerarJpeg(64, 64, SKColors.Green);
        private static readonly TimeSpan TempoDeEsperaLongo = TimeSpan.FromSeconds(ConstantsValues.TempoMaximoEsperaProcessamentoFotoSegundos);
        private const double LimiteDiferencaMediaPorCanal = 6;
        private const int TamanhoBlocoTesteFoto = 16;
        private const int SementeBlocosTesteFoto = 20261118;
        private const int VezesProcessamentoRepetido = 50;
        private const int LarguraImagemGrandeTeste = 4000;
        private const int AlturaImagemGrandeTeste = 3000;
        private const int SementeDegradeTesteFoto = 20261118;
        private const int AmplitudeRuidoLeveTesteFoto = 3;
        private const int QualidadeJpegEntradaDegradeTeste = 95;
        private const int MaximoCanalTesteFoto = 255;
        private const int CanaisRgbTesteFoto = 3;
        private const double ToleranciaCobertura = 1e-9;

        [Fact]
        public async Task JpegValido_SaiComoJpegComAsDimensoesDaImagem()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarJpeg(800, 600, SKColors.Green);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            Assert.Equal(800, foto.Largura);
            Assert.Equal(600, foto.Altura);
            Assert.True(foto.Conteudo.Length >= 3 && foto.Conteudo[0] == 0xFF && foto.Conteudo[1] == 0xD8 && foto.Conteudo[2] == 0xFF);
        }

        [Fact]
        public async Task PngValido_SaiComoJpeg()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarPng(300, 200, SKColors.Red);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimePng, CancellationToken.None);

            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            Assert.Equal(300, foto.Largura);
            Assert.Equal(200, foto.Altura);
            Assert.True(foto.Conteudo[0] == 0xFF && foto.Conteudo[1] == 0xD8 && foto.Conteudo[2] == 0xFF);
        }

        [Fact]
        public async Task ImagemGrande_SaiComLadoMaiorDeAteMilSeiscentosEProporcaoMantida()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarJpeg(4000, 3000, SKColors.Blue);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(1600, foto.Largura);
            Assert.Equal(1200, foto.Altura);
            using SKBitmap decodificada = SKBitmap.Decode(foto.Conteudo);
            Assert.Equal(1600, decodificada.Width);
            Assert.Equal(1200, decodificada.Height);
        }

        [Fact]
        public async Task ImagemPequena_NaoEAmpliada()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarJpeg(320, 240, SKColors.Blue);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(320, foto.Largura);
            Assert.Equal(240, foto.Altura);
        }

        [Fact]
        public async Task JpegComExifSemOrientacao_NaoTemMaisBlocoExifNaSaida()
        {
            byte[] jpegComExif = ImagensDeTesteFoto.InserirExifOrientacao(ImagensDeTesteFoto.GerarJpeg(64, 48, SKColors.Gray), 1);
            Assert.True(ImagensDeTesteFoto.ContemSequencia(jpegComExif, ImagensDeTesteFoto.SequenciaDoExif()));
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(jpegComExif, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.False(ImagensDeTesteFoto.ContemSequencia(foto.Conteudo, ImagensDeTesteFoto.SequenciaDoExif()));
        }

        [Fact]
        public async Task JpegComOrientacaoSeis_SaiEmPeComLarguraEAlturaTrocadasESemExif()
        {
            byte[] entrada = ImagensDeTesteFoto.InserirExifOrientacao(ImagensDeTesteFoto.GerarJpegMetadeVermelhaEsquerda(400, 200), 6);
            Assert.True(ImagensDeTesteFoto.ContemSequencia(entrada, ImagensDeTesteFoto.SequenciaDoExif()));
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(200, foto.Largura);
            Assert.Equal(400, foto.Altura);
            Assert.False(ImagensDeTesteFoto.ContemSequencia(foto.Conteudo, ImagensDeTesteFoto.SequenciaDoExif()));

            using SKBitmap decodificada = SKBitmap.Decode(foto.Conteudo);
            SKColor topo = decodificada.GetPixel(100, 50);
            SKColor inferior = decodificada.GetPixel(100, 350);
            Assert.True(topo.Red > 200 && topo.Blue < 80);
            Assert.True(inferior.Blue > 200 && inferior.Red < 80);
        }

        [Fact]
        public async Task PngComTransparencia_SaiComFundoBranco()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarPngTransparente(64, 64);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimePng, CancellationToken.None);

            using SKBitmap decodificada = SKBitmap.Decode(foto.Conteudo);
            SKColor centro = decodificada.GetPixel(32, 32);
            Assert.True(centro.Red >= 245 && centro.Green >= 245 && centro.Blue >= 245);
        }

        [Fact]
        public async Task ConteudoNulo_EValidationExceptionComArquivoObrigatorio()
        {
            FakeLogService log = new FakeLogService();
            ProcessadorFotoService processador = new ProcessadorFotoService(log);

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(null, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoObrigatoria);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task ConteudoVazio_EValidationExceptionComArquivoObrigatorio()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(Array.Empty<byte>(), ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoObrigatoria);
        }

        [Fact]
        public async Task ArquivoAcimaDeDezMiB_EValidationExceptionDeTamanho()
        {
            byte[] grande = new byte[ConstantsArmazenamento.TamanhoMaximoFotoBytes + 1];
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(grande, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoTamanho);
        }

        [Fact]
        public async Task ExeRenomeadoComTipoJpeg_EFormatoInvalido()
        {
            byte[] exe = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(exe, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task TipoJpegComBytesDePng_EFormatoInvalido()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(BytesDePng, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task TipoPdf_EFormatoInvalido()
        {
            byte[] pdf = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(pdf, "application/pdf", CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task PdfDeclaradoComoJpeg_EFormatoInvalido()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(ConstantsValues.AssinaturaPdf, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task TipoDeclaradoDiferenteDaAssinatura_EFormatoInvalido()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimePng, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task ArquivoComAssinaturaJpegMasSemImagem_EFormatoInvalidoSemDecodificar()
        {
            byte[] falso = { 0xFF, 0xD8, 0xFF, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 };
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(falso, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task JpegTruncado_CabecalhoValidoMasDecodificacaoIncompleta_EFormatoInvalido()
        {
            byte[] truncado = ImagensDeTesteFoto.Truncar(ImagensDeTesteFoto.GerarJpegComRuido(400, 300));
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            using SKData dados = SKData.CreateCopy(truncado);
            using SKCodec? codec = SKCodec.Create(dados);
            Assert.NotNull(codec);
            using SKBitmap bitmap = new SKBitmap(codec.Info);
            Assert.NotEqual(SKCodecResult.Success, codec.GetPixels(bitmap.Info, bitmap.GetPixels()));

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(truncado, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task JpegDeOitoMilEUmPixelsDeLado_EDimensoes()
        {
            byte[] largo = ImagensDeTesteFoto.GerarJpeg(8193, 1, SKColors.White);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(largo, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoDimensoes);
        }

        [Fact]
        public async Task PngQueDeclaraVinteMilPorVinte_RecusaPeloCabecalho()
        {
            byte[] declarado = ImagensDeTesteFoto.PngQueDeclaraDimensoes(20000, 20000);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(declarado, ConstantsArmazenamento.MimePng, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoDimensoes);
        }

        [Fact]
        public async Task PngComLadosDentroDoLimiteMasTotalAcimaDeDezesseisMegapixels_EDimensoes()
        {
            byte[] declarado = ImagensDeTesteFoto.PngQueDeclaraDimensoes(5000, 4000);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(declarado, ConstantsArmazenamento.MimePng, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoDimensoes);
        }

        [Fact]
        public async Task RecusaDeImagem_NaoGeraEntradaNoLog()
        {
            FakeLogService log = new FakeLogService();
            ProcessadorFotoService processador = new ProcessadorFotoService(log);

            await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(BytesDePng, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task Limite_SoDeixaDoisProcessamentosAoMesmoTempo()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());
            SemaphoreSlim limite = ObterLimite(processador);
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);

            await limite.WaitAsync();
            await limite.WaitAsync();
            Assert.Equal(0, limite.CurrentCount);

            Task<FotoProcessada> terceira = processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);
            Assert.False(terceira.IsCompleted);

            limite.Release();
            FotoProcessada foto = await terceira;

            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            limite.Release();
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
        }

        [Fact]
        public async Task FilaCheia_NonaChamadaERecusadaNaHora_ComLimiteExcedidoESemLog()
        {
            FakeLogService log = new FakeLogService();
            ProcessadorFotoService processador = new ProcessadorFotoService(log, TempoDeEsperaLongo);
            SemaphoreSlim limite = ObterLimite(processador);
            await limite.WaitAsync();
            await limite.WaitAsync();

            List<Task<FotoProcessada>> enfileiradas = new List<Task<FotoProcessada>>();
            for (int indice = 0; indice < ConstantsValues.MaximoFotosNaFilaDeProcessamento; indice++)
            {
                enfileiradas.Add(processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));
            }

            Assert.Equal(ConstantsValues.MaximoFotosNaFilaDeProcessamento, ObterAguardando(processador));

            LimiteExcedidoException excecao = await Assert.ThrowsAsync<LimiteExcedidoException>(() => processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado, excecao.Message);
            Assert.Equal(ConstantsValues.MaximoFotosNaFilaDeProcessamento, ObterAguardando(processador));
            Assert.Empty(log.Snapshot());
            Assert.All(enfileiradas, tarefa => Assert.False(tarefa.IsCompleted));

            limite.Release();
            limite.Release();
            FotoProcessada[] fotos = await Task.WhenAll(enfileiradas);

            Assert.Equal(ConstantsValues.MaximoFotosNaFilaDeProcessamento, fotos.Length);
            Assert.Equal(0, ObterAguardando(processador));
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
            Assert.Empty(log.Snapshot());
        }

        [Fact]
        public async Task TempoLimiteDaEspera_DevolveLimiteExcedido_SemTomarVagaEComContadorZerado()
        {
            FakeLogService log = new FakeLogService();
            ProcessadorFotoService processador = new ProcessadorFotoService(log, TimeSpan.FromMilliseconds(10));
            SemaphoreSlim limite = ObterLimite(processador);
            await limite.WaitAsync();
            await limite.WaitAsync();

            LimiteExcedidoException excecao = await Assert.ThrowsAsync<LimiteExcedidoException>(() => processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            Assert.Equal(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado, excecao.Message);
            Assert.Equal(0, ObterAguardando(processador));
            Assert.Equal(0, limite.CurrentCount);
            Assert.Empty(log.Snapshot());

            limite.Release();
            limite.Release();
            FotoProcessada foto = await processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            Assert.Equal(0, ObterAguardando(processador));
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
        }

        [Fact]
        public async Task LiberacaoDaVaga_EsperaTerminaComSucesso_EContadorVoltaAZero()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService(), TempoDeEsperaLongo);
            SemaphoreSlim limite = ObterLimite(processador);
            await limite.WaitAsync();
            await limite.WaitAsync();

            Task<FotoProcessada> espera = processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(1, ObterAguardando(processador));
            Assert.False(espera.IsCompleted);

            limite.Release();
            FotoProcessada foto = await espera;

            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            Assert.Equal(0, ObterAguardando(processador));
            Assert.Equal(1, limite.CurrentCount);

            limite.Release();
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
        }

        [Fact]
        public async Task RecusaDepoisDeTerPedidoAVaga_DevolveAVaga()
        {
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());
            SemaphoreSlim limite = ObterLimite(processador);
            byte[] truncado = ImagensDeTesteFoto.Truncar(ImagensDeTesteFoto.GerarJpegComRuido(400, 300));

            await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(truncado, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
            Assert.Equal(0, ObterAguardando(processador));
        }

        [Fact]
        public async Task Cancelamento_NaFilaDoLimite_EhRegistradoERelancadoEContadorVoltaAZero()
        {
            FakeLogService log = new FakeLogService();
            ProcessadorFotoService processador = new ProcessadorFotoService(log);
            SemaphoreSlim limite = ObterLimite(processador);
            await limite.WaitAsync();
            await limite.WaitAsync();
            using CancellationTokenSource cancelamento = new CancellationTokenSource();

            Task<FotoProcessada> tarefa = processador.ProcessarAsync(BytesDeJpeg, ConstantsArmazenamento.MimeJpeg, cancelamento.Token);
            cancelamento.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tarefa);
            List<string> entradas = log.Snapshot();
            Assert.Single(entradas);
            Assert.StartsWith(ConstantsMessage.ErroProcessarFotoVeiculo, entradas[0], StringComparison.Ordinal);
            Assert.Equal(0, limite.CurrentCount);
            Assert.Equal(0, ObterAguardando(processador));

            limite.Release();
            limite.Release();
            Assert.Equal(ConstantsValues.MaximoProcessamentosSimultaneosFoto, limite.CurrentCount);
        }

        [Fact]
        public async Task JpegDe4000x2000_ComOrientacaoSeisEDecodificacaoReduzida_SaiEmPeNaProporcao()
        {
            byte[] entrada = ImagensDeTesteFoto.InserirExifOrientacao(ImagensDeTesteFoto.GerarJpegMetadeVermelhaEsquerda(4000, 2000), 6);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.Equal(800, foto.Largura);
            Assert.Equal(1600, foto.Altura);
            using SKBitmap decodificada = SKBitmap.Decode(foto.Conteudo);
            SKColor topo = decodificada.GetPixel(400, 400);
            SKColor inferior = decodificada.GetPixel(400, 1200);
            Assert.True(topo.Red > 200 && topo.Blue < 80);
            Assert.True(inferior.Blue > 200 && inferior.Red < 80);
        }

        [Fact]
        public async Task PngGrande_NaoEDecodificadoReduzido_SaiComLadoMaiorDeMilSeiscentos()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarPng(4000, 3000, SKColors.Red);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimePng, CancellationToken.None);

            Assert.Equal(1600, foto.Largura);
            Assert.Equal(1200, foto.Altura);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
        }

        [Fact]
        public async Task JpegComExifEGpsDeLadoMaiorMilSeiscentos_SaiSemSegmentoExif()
        {
            await AfirmarSemExifComGps(1600, 1200);
        }

        [Fact]
        public async Task JpegComExifEGpsDe4000_SaiSemSegmentoExif()
        {
            await AfirmarSemExifComGps(4000, 3000);
        }

        [Fact]
        public void EscolherDimensoes_JpegDe4000x3000_ParaSaidaDe1600x1200_EscalaMetade()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarJpeg(4000, 3000, SKColors.Green), 1600, 1200, 2000, 1500);
        }

        [Fact]
        public void EscolherDimensoes_JpegDe8000x2000_ParaSaidaDe1600x400_EscalaQuarto()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarJpeg(8000, 2000, SKColors.Green), 1600, 400, 2000, 500);
        }

        [Fact]
        public void EscolherDimensoes_JpegDe3264x2448_ParaSaidaDe1600x1200_EscalaMetadeComLadoJustoNaSaida()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarJpeg(3264, 2448, SKColors.Green), 1600, 1200, 1632, 1224);
        }

        [Fact]
        public void EscolherDimensoes_JpegDe3000x2000_NenhumaEscalaCobreASaida_DecodificaInteiro()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarJpeg(3000, 2000, SKColors.Green), 1600, 1067, 3000, 2000);
        }

        [Fact]
        public void EscolherDimensoes_Png_DecodificaInteiro()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarPng(4000, 3000, SKColors.Green), 1600, 1200, 4000, 3000);
        }

        [Fact]
        public void EscolherDimensoes_JpegIgualASaida_NuncaFicaMenorQueASaida()
        {
            AfirmarDimensoesDeDecodificacao(ImagensDeTesteFoto.GerarJpeg(1600, 1200, SKColors.Green), 1600, 1200, 1600, 1200);
        }

        [Theory]
        [InlineData(2000, 1600, true)]
        [InlineData(3200, 1600, true)]
        [InlineData(3201, 1600, false)]
        public void UsaFiltroRapido_SoAteFatorResidualDois(int ladoMaiorDecodificado, int ladoMaiorSaida, bool esperado)
        {
            Assert.Equal(esperado, ProcessadorFotoService.UsaFiltroRapido(ladoMaiorDecodificado, ladoMaiorSaida));
        }

        [Fact]
        public async Task JpegCortadoNaDecodificacaoReduzida_EFormatoInvalido()
        {
            byte[] truncado = ImagensDeTesteFoto.Truncar(GerarJpegDeBlocos(4000, 3000));
            AfirmarDimensoesDeDecodificacao(truncado, 1600, 1200, 2000, 1500);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            ValidationException excecao = await Assert.ThrowsAsync<ValidationException>(() => processador.ProcessarAsync(truncado, ConstantsArmazenamento.MimeJpeg, CancellationToken.None));

            AfirmarRecusa(excecao, ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        /// <summary>
        /// Guarda de qualidade (nao e prova de nitidez), com as duas referencias e a saida na mesma condicao: passam pelo JPEG de saida
        /// (QualidadeJpegFotoVeiculo) antes de comparar. (a) Bordas finas: a saida contra a media de area exata da imagem inteira em
        /// ate 6 por canal. (b) Mesma imagem: a saida nao fica mais longe da media de area do que o caminho antigo (decodificacao
        /// inteira + Mitchell). (c) Imagem suave com ruido leve: a saida e o caminho antigo coincidem, em ate 6 por canal.
        /// Medido em 18/11/2026 (laboratorio de nitidez): (a) 3,5374; (b) 3,5374 contra 10,7960 do antigo; (c) 0,4192.
        /// Criterio no contrato backend/.../veiculo-foto-desempenho.contract.json (testes_obrigatorios, guarda de qualidade).
        /// </summary>
        [Fact]
        public async Task GuardaDeQualidade_MedeSaidaContraMediaDeAreaEContraCaminhoAntigo_BordasFinasESuave()
        {
            byte[] bordas = GerarJpegDeBordasFinas(LarguraImagemGrandeTeste, AlturaImagemGrandeTeste);
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada fotoBordas = await processador.ProcessarAsync(bordas, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);
            using SKBitmap saidaBordas = SKBitmap.Decode(fotoBordas.Conteudo);
            using SKBitmap mediaAreaBordas = IdaEVoltaJpeg(GerarReferenciaMediaDeArea(bordas, fotoBordas.Largura, fotoBordas.Altura));
            using SKBitmap antigoBordas = IdaEVoltaJpeg(GerarReferenciaMitchell(bordas, fotoBordas.Largura, fotoBordas.Altura));
            double saidaContraMediaDeArea = DiferencaMediaPorCanal(saidaBordas, mediaAreaBordas);
            double antigoContraMediaDeArea = DiferencaMediaPorCanal(antigoBordas, mediaAreaBordas);

            byte[] suave = GerarJpegDeDegradeComRuido(LarguraImagemGrandeTeste, AlturaImagemGrandeTeste);
            FotoProcessada fotoSuave = await processador.ProcessarAsync(suave, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);
            using SKBitmap saidaSuave = SKBitmap.Decode(fotoSuave.Conteudo);
            using SKBitmap antigoSuave = IdaEVoltaJpeg(GerarReferenciaMitchell(suave, fotoSuave.Largura, fotoSuave.Altura));
            double saidaContraAntigoSuave = DiferencaMediaPorCanal(saidaSuave, antigoSuave);

            Assert.True(saidaContraMediaDeArea <= LimiteDiferencaMediaPorCanal);
            Assert.True(saidaContraMediaDeArea <= antigoContraMediaDeArea);
            Assert.True(saidaContraAntigoSuave <= LimiteDiferencaMediaPorCanal);
        }

        [Fact]
        public async Task Memoria_EntradaNaoEAlteradaERepetirCinquentaVezesNaoLanca()
        {
            byte[] entrada = ImagensDeTesteFoto.GerarJpeg(1200, 900, SKColors.Green);
            byte[] copia = entrada.ToArray();
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            for (int indice = 0; indice < VezesProcessamentoRepetido; indice++)
            {
                FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);
                Assert.Equal(ConstantsArmazenamento.MimeJpeg, foto.ContentType);
            }

            Assert.Equal(copia, entrada);
        }

        /// <summary>Confere a escolha de decodificacao de uma imagem gerada: a escala, as dimensoes e o formato saem do codec real.</summary>
        private static void AfirmarDimensoesDeDecodificacao(byte[] imagem, int larguraSaida, int alturaSaida, int larguraEsperada, int alturaEsperada)
        {
            using SKData dados = SKData.CreateCopy(imagem);
            using SKCodec? codec = SKCodec.Create(dados);
            Assert.NotNull(codec);

            SKSizeI dimensoes = ProcessadorFotoService.EscolherDimensoesDeDecodificacao(codec, larguraSaida, alturaSaida);

            Assert.Equal(larguraEsperada, dimensoes.Width);
            Assert.Equal(alturaEsperada, dimensoes.Height);
        }

        /// <summary>Gera um JPEG com EXIF de orientacao 1 e um bloco GPS (ponteiro 0x8825 e latitude), e confere que a saida nao tem EXIF.</summary>
        private static async Task AfirmarSemExifComGps(int largura, int altura)
        {
            byte[] entrada = InserirExifComGps(ImagensDeTesteFoto.GerarJpeg(largura, altura, SKColors.Gray));
            Assert.True(ImagensDeTesteFoto.ContemSequencia(entrada, ImagensDeTesteFoto.SequenciaDoExif()));
            ProcessadorFotoService processador = new ProcessadorFotoService(new FakeLogService());

            FotoProcessada foto = await processador.ProcessarAsync(entrada, ConstantsArmazenamento.MimeJpeg, CancellationToken.None);

            Assert.False(ImagensDeTesteFoto.ContemSequencia(foto.Conteudo, ImagensDeTesteFoto.SequenciaDoExif()));
        }

        /// <summary>
        /// Insere logo depois do SOI um APP1 EXIF little-endian: IFD0 com orientacao 1 e o ponteiro GPS; o IFD GPS traz a latitude (S).
        /// Os deslocamentos sao contados a partir do cabecalho TIFF (56 bytes no total).
        /// </summary>
        private static byte[] InserirExifComGps(byte[] jpeg)
        {
            byte[] tiff =
            {
                0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
                0x02, 0x00,
                0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                0x25, 0x88, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x26, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
                0x01, 0x00,
                0x01, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00, 0x53, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
            };
            byte[] cabecalhoExif = { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 };
            int comprimentoSegmento = 2 + cabecalhoExif.Length + tiff.Length;
            byte[] marcadorApp1 = { 0xFF, 0xE1, (byte)(comprimentoSegmento >> 8), (byte)(comprimentoSegmento & 0xFF) };

            List<byte> saida = new List<byte>();
            saida.AddRange(jpeg.AsSpan(0, 2).ToArray());
            saida.AddRange(marcadorApp1);
            saida.AddRange(cabecalhoExif);
            saida.AddRange(tiff);
            saida.AddRange(jpeg.AsSpan(2).ToArray());
            return saida.ToArray();
        }

        /// <summary>JPEG de blocos de cor aleatoria (semente fixa): arquivo pequeno, com dados suficientes para o corte cair nas linhas codificadas.</summary>
        private static byte[] GerarJpegDeBlocos(int largura, int altura)
        {
            Random gerador = new Random(SementeBlocosTesteFoto);
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            using SKCanvas canvas = new SKCanvas(bitmap);
            using SKPaint pincel = new SKPaint();

            for (int x = 0; x < largura; x += TamanhoBlocoTesteFoto)
            {
                for (int y = 0; y < altura; y += TamanhoBlocoTesteFoto)
                {
                    pincel.Color = new SKColor((byte)gerador.Next(256), (byte)gerador.Next(256), (byte)gerador.Next(256));
                    canvas.DrawRect(x, y, TamanhoBlocoTesteFoto, TamanhoBlocoTesteFoto, pincel);
                }
            }

            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData? dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 85);
            Assert.NotNull(dados);
            return dados.ToArray();
        }

        /// <summary>JPEG com fundo branco e linhas pretas finas de 2 px a cada 40 px (bordas que o filtro pode perder).</summary>
        private static byte[] GerarJpegDeBordasFinas(int largura, int altura)
        {
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            using SKCanvas canvas = new SKCanvas(bitmap);
            using SKPaint linha = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = false };

            canvas.Clear(SKColors.White);
            for (int x = 0; x < largura; x += 40)
            {
                canvas.DrawLine(x, 0, x, altura, linha);
            }

            for (int y = 0; y < altura; y += 40)
            {
                canvas.DrawLine(0, y, largura, y, linha);
            }

            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData? dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 95);
            Assert.NotNull(dados);
            return dados.ToArray();
        }

        /// <summary>Referencia da guarda de qualidade: decodificacao inteira com o filtro Mitchell, sobre fundo branco, no tamanho da saida.</summary>
        private static SKBitmap GerarReferenciaMitchell(byte[] entrada, int largura, int altura)
        {
            using SKBitmap completa = SKBitmap.Decode(entrada);
            SKBitmap referencia = new SKBitmap(new SKImageInfo(largura, altura, SKColorType.Rgba8888, SKAlphaType.Opaque));
            using SKCanvas canvas = new SKCanvas(referencia);
            using SKPaint pincel = new SKPaint();

            canvas.Clear(SKColors.White);
            canvas.Scale((float)largura / completa.Width, (float)altura / completa.Height);
            canvas.DrawBitmap(completa, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell), pincel);
            return referencia;
        }

        /// <summary>
        /// Referencia de media de area exata: cada pixel de saida e a media dos pixels da imagem decodificada inteira que ele cobre,
        /// cada um pesado pela fracao coberta (cobertura fracionaria). Feita sobre os valores gravados (sRGB), sem luz linear.
        /// </summary>
        private static SKBitmap GerarReferenciaMediaDeArea(byte[] entrada, int largura, int altura)
        {
            using SKBitmap completa = SKBitmap.Decode(entrada);
            SKColor[] origem = completa.Pixels;
            int origemLargura = completa.Width;
            (int[] inicioX, double[][] pesoX) = PesosDeCobertura(origemLargura, largura);
            (int[] inicioY, double[][] pesoY) = PesosDeCobertura(completa.Height, altura);
            double[] horizontal = new double[largura * CanaisRgbTesteFoto];
            double[] acumulado = new double[largura * CanaisRgbTesteFoto];
            SKColor[] saida = new SKColor[largura * altura];

            for (int dy = 0; dy < altura; dy++)
            {
                Array.Clear(acumulado, 0, acumulado.Length);
                for (int k = 0; k < pesoY[dy].Length; k++)
                {
                    ReduzirLinhaNaHorizontal(origem, origemLargura, inicioY[dy] + k, inicioX, pesoX, horizontal);
                    double pesoDaLinha = pesoY[dy][k];
                    for (int j = 0; j < acumulado.Length; j++)
                    {
                        acumulado[j] += pesoDaLinha * horizontal[j];
                    }
                }

                for (int dx = 0; dx < largura; dx++)
                {
                    int indice = dx * CanaisRgbTesteFoto;
                    saida[dy * largura + dx] = new SKColor(
                        ParaCanal(acumulado[indice]),
                        ParaCanal(acumulado[indice + 1]),
                        ParaCanal(acumulado[indice + 2]));
                }
            }

            SKBitmap referencia = new SKBitmap(new SKImageInfo(largura, altura, SKColorType.Rgba8888, SKAlphaType.Opaque));
            referencia.Pixels = saida;
            return referencia;
        }

        /// <summary>Para cada pixel de saida: o primeiro pixel de origem que ele toca e o peso de cada um (a fracao coberta, somando 1).</summary>
        private static (int[] Inicio, double[][] Pesos) PesosDeCobertura(int origem, int saida)
        {
            double fator = (double)origem / saida;
            int[] inicio = new int[saida];
            double[][] pesos = new double[saida][];
            for (int i = 0; i < saida; i++)
            {
                double a = i * fator;
                double b = (i + 1) * fator;
                int primeiro = (int)Math.Floor(a + ToleranciaCobertura);
                int ultimo = Math.Min((int)Math.Ceiling(b - ToleranciaCobertura) - 1, origem - 1);
                double[] cobertura = new double[ultimo - primeiro + 1];
                double soma = 0;
                for (int k = 0; k < cobertura.Length; k++)
                {
                    double fracao = Math.Min(b, primeiro + k + 1) - Math.Max(a, primeiro + k);
                    cobertura[k] = fracao < 0 ? 0 : fracao;
                    soma += cobertura[k];
                }

                for (int k = 0; k < cobertura.Length; k++)
                {
                    cobertura[k] /= soma;
                }

                inicio[i] = primeiro;
                pesos[i] = cobertura;
            }

            return (inicio, pesos);
        }

        /// <summary>Reduz uma linha da origem na horizontal: para cada coluna de saida, a soma ponderada dos canais R, G e B.</summary>
        private static void ReduzirLinhaNaHorizontal(SKColor[] origem, int origemLargura, int linha, int[] inicioX, double[][] pesoX, double[] destino)
        {
            for (int dx = 0; dx < pesoX.Length; dx++)
            {
                double vermelho = 0;
                double verde = 0;
                double azul = 0;
                for (int k = 0; k < pesoX[dx].Length; k++)
                {
                    SKColor cor = origem[linha * origemLargura + inicioX[dx] + k];
                    double peso = pesoX[dx][k];
                    vermelho += peso * cor.Red;
                    verde += peso * cor.Green;
                    azul += peso * cor.Blue;
                }

                int indice = dx * CanaisRgbTesteFoto;
                destino[indice] = vermelho;
                destino[indice + 1] = verde;
                destino[indice + 2] = azul;
            }
        }

        private static byte ParaCanal(double valor)
        {
            return (byte)Math.Clamp(Math.Round(valor), 0, MaximoCanalTesteFoto);
        }

        /// <summary>Grava o bitmap como o processador grava a saida (JPEG de QualidadeJpegFotoVeiculo) e le de volta: a referencia fica na mesma condicao da saida. Consome o bitmap.</summary>
        private static SKBitmap IdaEVoltaJpeg(SKBitmap bitmap)
        {
            using SKBitmap origem = bitmap;
            using SKData? codificada = origem.Encode(SKEncodedImageFormat.Jpeg, ConstantsValues.QualidadeJpegFotoVeiculo);
            Assert.NotNull(codificada);
            return SKBitmap.Decode(codificada.ToArray());
        }

        /// <summary>Degrade de cor com ruido leve (semente fixa), em JPEG: o caso em que o caminho novo e o Mitchell devem dar quase o mesmo.</summary>
        private static byte[] GerarJpegDeDegradeComRuido(int largura, int altura)
        {
            Random gerador = new Random(SementeDegradeTesteFoto);
            SKColor[] pixels = new SKColor[largura * altura];
            for (int y = 0; y < altura; y++)
            {
                for (int x = 0; x < largura; x++)
                {
                    int vermelho = x * MaximoCanalTesteFoto / (largura - 1);
                    int verde = y * MaximoCanalTesteFoto / (altura - 1);
                    int azul = (x + y) * MaximoCanalTesteFoto / (largura + altura - 2);
                    pixels[y * largura + x] = new SKColor(
                        ComRuidoLeve(vermelho, gerador),
                        ComRuidoLeve(verde, gerador),
                        ComRuidoLeve(azul, gerador));
                }
            }

            using SKBitmap bitmap = new SKBitmap(new SKImageInfo(largura, altura, SKColorType.Rgba8888, SKAlphaType.Opaque));
            bitmap.Pixels = pixels;
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData? dados = imagem.Encode(SKEncodedImageFormat.Jpeg, QualidadeJpegEntradaDegradeTeste);
            Assert.NotNull(dados);
            return dados.ToArray();
        }

        private static byte ComRuidoLeve(int valor, Random gerador)
        {
            int comRuido = valor + gerador.Next(-AmplitudeRuidoLeveTesteFoto, AmplitudeRuidoLeveTesteFoto + 1);
            return (byte)Math.Clamp(comRuido, 0, MaximoCanalTesteFoto);
        }

        /// <summary>Diferenca absoluta media por canal (R, G e B), de 0 a 255, entre duas imagens do mesmo tamanho.</summary>
        private static double DiferencaMediaPorCanal(SKBitmap primeira, SKBitmap segunda)
        {
            long soma = 0;
            for (int x = 0; x < primeira.Width; x++)
            {
                for (int y = 0; y < primeira.Height; y++)
                {
                    SKColor a = primeira.GetPixel(x, y);
                    SKColor b = segunda.GetPixel(x, y);
                    soma += Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue);
                }
            }

            return (double)soma / ((double)primeira.Width * primeira.Height * 3);
        }

        /// <summary>
        /// Confere a recusa inteira: a mensagem da excecao e a falha no campo foto, porque o middleware responde o detail com
        /// validation.Errors.FirstOrDefault() (ErrorHandlingMiddleware.cs) e so cai na mensagem generica se a lista vier vazia.
        /// </summary>
        private static void AfirmarRecusa(ValidationException excecao, string mensagem)
        {
            Assert.Equal(mensagem, excecao.Message);
            ValidationFailure falha = Assert.Single(excecao.Errors);
            Assert.Equal(ConstantsValues.CampoFotoVeiculo, falha.PropertyName);
            Assert.Equal(mensagem, falha.ErrorMessage);
        }

        /// <summary>Le o semaforo do processador por reflexao, para o teste segurar as vagas de forma deterministica.</summary>
        private static SemaphoreSlim ObterLimite(ProcessadorFotoService processador)
        {
            FieldInfo? campo = typeof(ProcessadorFotoService).GetField("_limite", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(campo);
            SemaphoreSlim? limite = campo.GetValue(processador) as SemaphoreSlim;
            Assert.NotNull(limite);
            return limite;
        }

        /// <summary>Le, por reflexao, quantas chamadas estao aguardando vaga neste momento (contador _aguardando).</summary>
        private static int ObterAguardando(ProcessadorFotoService processador)
        {
            FieldInfo? campo = typeof(ProcessadorFotoService).GetField("_aguardando", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(campo);
            int? aguardando = campo.GetValue(processador) as int?;
            Assert.NotNull(aguardando);
            return aguardando.Value;
        }
    }
}
