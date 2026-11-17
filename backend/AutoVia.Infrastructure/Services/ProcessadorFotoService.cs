using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation;
using FluentValidation.Results;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Processa a foto do veiculo com SkiaSharp: confere tipo e assinatura, le as dimensoes antes de decodificar, aplica a orientacao
    /// do EXIF, reduz para ate LadoMaximoFotoVeiculoPixels, pinta fundo branco e grava JPEG sem metadados (a saida nasce dos pixels).
    /// Singleton: _limite restringe a MaximoProcessamentosSimultaneosFoto decodificacoes ao mesmo tempo no processo. Quem espera
    /// pela vaga conta em _aguardando (no maximo MaximoFotosNaFilaDeProcessamento); a espera tem tempo limite (_tempoMaximoEspera).
    /// </summary>
    public sealed class ProcessadorFotoService : IProcessadorFotoService
    {
        private static readonly byte[] AssinaturaJpeg = { 0xFF, 0xD8, 0xFF };
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly SKSamplingOptions AmostragemReducao = new SKSamplingOptions(SKCubicResampler.Mitchell);

        private readonly ILogService _logService;
        private readonly TimeSpan _tempoMaximoEspera;
        private readonly SemaphoreSlim _limite = new SemaphoreSlim(
            ConstantsValues.MaximoProcessamentosSimultaneosFoto,
            ConstantsValues.MaximoProcessamentosSimultaneosFoto);
        private int _aguardando;

        /// <summary>Cria o processador com o log do projeto e o tempo maximo de espera pela vaga do ConstantsValues.</summary>
        public ProcessadorFotoService(ILogService logService)
            : this(logService, TimeSpan.FromSeconds(ConstantsValues.TempoMaximoEsperaProcessamentoFotoSegundos))
        {
        }

        /// <summary>Construtor de teste: permite informar outro tempo maximo de espera (o publico usa o do ConstantsValues).</summary>
        internal ProcessadorFotoService(ILogService logService, TimeSpan tempoMaximoEspera)
        {
            _logService = logService;
            _tempoMaximoEspera = tempoMaximoEspera;
        }

        /// <summary>
        /// Valida a foto e devolve a versao JPEG recodificada. As recusas de imagem saem como ValidationException (sem log);
        /// a fila cheia e a espera estourada saem como LimiteExcedidoException (sem log); qualquer outra falha, inclusive o
        /// cancelamento, e registrada com ErroProcessarFotoVeiculo e relancada.
        /// </summary>
        public async Task<FotoProcessada> ProcessarAsync(byte[]? conteudo, string? contentType, CancellationToken cancellationToken)
        {
            try
            {
                byte[] fotoBytes = ValidarEntradaRapida(conteudo, contentType);

                bool obteveVaga = await AguardarVagaAsync(cancellationToken);
                if (!obteveVaga)
                {
                    throw new LimiteExcedidoException(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado);
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Processar(fotoBytes);
                }
                finally
                {
                    _limite.Release();
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroProcessarFotoVeiculo, GetType().ToString());
                throw;
            }
        }

        /// <summary>
        /// Pede a vaga do limite. Recusa na hora quando ja ha MaximoFotosNaFilaDeProcessamento chamadas esperando; devolve falso
        /// quando a espera estoura o tempo limite. O contador de espera volta em todos os caminhos (vaga, tempo limite ou cancelamento).
        /// </summary>
        private async Task<bool> AguardarVagaAsync(CancellationToken cancellationToken)
        {
            int aguardandoComEsta = Interlocked.Increment(ref _aguardando);
            if (aguardandoComEsta > ConstantsValues.MaximoFotosNaFilaDeProcessamento)
            {
                Interlocked.Decrement(ref _aguardando);
                throw new LimiteExcedidoException(ConstantsMessage.ErroVeiculoFotoProcessamentoOcupado);
            }

            try
            {
                return await _limite.WaitAsync(_tempoMaximoEspera, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _aguardando);
            }
        }

        /// <summary>Confere, antes de pedir a vaga do limite, o que custa pouco: tamanho, tipo declarado e assinatura dos primeiros bytes.</summary>
        private static byte[] ValidarEntradaRapida(byte[]? conteudo, string? contentType)
        {
            if (conteudo == null || conteudo.Length == 0)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoObrigatoria);
            }

            if (conteudo.Length > ConstantsArmazenamento.TamanhoMaximoFotoBytes)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoTamanho);
            }

            bool tipoJpeg = string.Equals(contentType, ConstantsArmazenamento.MimeJpeg, StringComparison.OrdinalIgnoreCase);
            bool tipoPng = string.Equals(contentType, ConstantsArmazenamento.MimePng, StringComparison.OrdinalIgnoreCase);
            bool assinaturaJpeg = ComecaCom(conteudo, AssinaturaJpeg);
            bool assinaturaPng = ComecaCom(conteudo, AssinaturaPng);

            if (!(tipoJpeg && assinaturaJpeg) && !(tipoPng && assinaturaPng))
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
            }

            return conteudo;
        }

        /// <summary>Decodifica e recodifica a imagem. Todos os objetos nativos saem por using, inclusive nos caminhos de erro.</summary>
        private static FotoProcessada Processar(byte[] fotoBytes)
        {
            using SKData dadosOriginais = SKData.CreateCopy(fotoBytes);
            using SKCodec? codec = SKCodec.Create(dadosOriginais);
            if (codec == null)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
            }

            SKImageInfo infoOriginal = codec.Info;
            ValidarDimensoes(infoOriginal);
            SKEncodedOrigin origem = codec.EncodedOrigin;

            // Decodifica so depois da conferencia do cabecalho. JPEG cortado devolve imagem parcial sem erro: o resultado do codec decide.
            using SKBitmap original = new SKBitmap(infoOriginal);
            SKCodecResult resultadoDecodificacao = codec.GetPixels(original.Info, original.GetPixels());
            if (resultadoDecodificacao != SKCodecResult.Success)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
            }

            bool trocaLadoELado = origem == SKEncodedOrigin.LeftTop
                || origem == SKEncodedOrigin.RightTop
                || origem == SKEncodedOrigin.RightBottom
                || origem == SKEncodedOrigin.LeftBottom;
            int larguraOrientada = trocaLadoELado ? infoOriginal.Height : infoOriginal.Width;
            int alturaOrientada = trocaLadoELado ? infoOriginal.Width : infoOriginal.Height;

            (int larguraSaida, int alturaSaida) = RegrasVeiculoFoto.CalcularDimensoesDeSaida(
                larguraOrientada,
                alturaOrientada,
                ConstantsValues.LadoMaximoFotoVeiculoPixels);

            SKImageInfo infoSaida = new SKImageInfo(larguraSaida, alturaSaida, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using SKBitmap saida = new SKBitmap(infoSaida);
            using SKCanvas canvas = new SKCanvas(saida);
            using SKPaint pincel = new SKPaint();

            canvas.Clear(SKColors.White);

            // Escala por ultimo: a orientacao corrige os pixels e so depois a imagem e reduzida. Sem ampliacao (ver CalcularDimensoesDeSaida).
            canvas.Scale((float)larguraSaida / larguraOrientada, (float)alturaSaida / alturaOrientada);
            SKMatrix orientacao = MontarOrientacao(origem, infoOriginal.Width, infoOriginal.Height);
            canvas.Concat(in orientacao);
            canvas.DrawBitmap(original, 0, 0, AmostragemReducao, pincel);

            using SKData? codificada = saida.Encode(SKEncodedImageFormat.Jpeg, ConstantsValues.QualidadeJpegFotoVeiculo);
            if (codificada == null)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroProcessarFotoVeiculo);
            }

            return new FotoProcessada(codificada.ToArray(), ConstantsArmazenamento.MimeJpeg, larguraSaida, alturaSaida);
        }

        /// <summary>Recusa se o cabecalho nao tem area valida, ou se algum lado ou o total de pixels passa do limite. Nada e decodificado antes.</summary>
        private static void ValidarDimensoes(SKImageInfo informacao)
        {
            if (informacao.Width <= 0 || informacao.Height <= 0)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
            }

            if (informacao.Width > ConstantsArmazenamento.DimensaoMaximaFotoPixels
                || informacao.Height > ConstantsArmazenamento.DimensaoMaximaFotoPixels
                || (long)informacao.Width * informacao.Height > ConstantsArmazenamento.TotalMaximoPixelsFoto)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoDimensoes);
            }
        }

        /// <summary>
        /// Matriz que leva os pixels da imagem gravada para a posicao em pe, conforme a orientacao do EXIF (valores 1 a 8).
        /// largura e altura sao as dimensoes da imagem como esta gravada (antes de girar).
        /// </summary>
        private static SKMatrix MontarOrientacao(SKEncodedOrigin origem, int largura, int altura)
        {
            switch (origem)
            {
                case SKEncodedOrigin.TopRight:
                    return new SKMatrix(-1, 0, largura, 0, 1, 0, 0, 0, 1);
                case SKEncodedOrigin.BottomRight:
                    return new SKMatrix(-1, 0, largura, 0, -1, altura, 0, 0, 1);
                case SKEncodedOrigin.BottomLeft:
                    return new SKMatrix(1, 0, 0, 0, -1, altura, 0, 0, 1);
                case SKEncodedOrigin.LeftTop:
                    return new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1);
                case SKEncodedOrigin.RightTop:
                    return new SKMatrix(0, -1, altura, 1, 0, 0, 0, 0, 1);
                case SKEncodedOrigin.RightBottom:
                    return new SKMatrix(0, -1, altura, -1, 0, largura, 0, 0, 1);
                case SKEncodedOrigin.LeftBottom:
                    return new SKMatrix(0, 1, 0, -1, 0, largura, 0, 0, 1);
                default:
                    return SKMatrix.CreateIdentity();
            }
        }

        /// <summary>Confere se os primeiros bytes batem com a assinatura.</summary>
        private static bool ComecaCom(byte[] dados, byte[] assinatura)
        {
            if (dados.Length < assinatura.Length)
            {
                return false;
            }

            for (int indice = 0; indice < assinatura.Length; indice++)
            {
                if (dados[indice] != assinatura[indice])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Monta a recusa de imagem: ValidationException com a mensagem especifica e a falha no campo foto (o middleware responde 400).</summary>
        private static ValidationException CriarRecusa(string mensagem)
        {
            List<ValidationFailure> falhas = new List<ValidationFailure> { new ValidationFailure(ConstantsValues.CampoFotoVeiculo, mensagem) };

            return new ValidationException(mensagem, falhas);
        }
    }
}
