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
