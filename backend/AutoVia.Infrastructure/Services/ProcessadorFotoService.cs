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
        private static readonly SKSamplingOptions AmostragemLinear = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

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

        /// <summary>
        /// Decodifica e recodifica a imagem. A decodificacao sai na menor escala reduzida que ainda cobre a saida
        /// (EscolherDimensoesDeDecodificacao). Todos os objetos nativos saem por using, inclusive nos caminhos de erro.
        /// </summary>
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

            // Decodifica so depois da conferencia do cabecalho, na menor escala que cobre a saida. JPEG cortado devolve imagem parcial sem erro: o resultado do codec decide.
            SKSizeI dimensoesDecodificacao = EscolherDimensoesDeDecodificacao(codec, larguraSaida, alturaSaida);
            SKImageInfo infoDecodificacao = new SKImageInfo(
                dimensoesDecodificacao.Width,
                dimensoesDecodificacao.Height,
                infoOriginal.ColorType,
                infoOriginal.AlphaType,
                infoOriginal.ColorSpace);
            using SKBitmap decodificada = new SKBitmap(infoDecodificacao);
            SKCodecResult resultadoDecodificacao = codec.GetPixels(decodificada.Info, decodificada.GetPixels());
            if (resultadoDecodificacao != SKCodecResult.Success)
            {
                throw CriarRecusa(ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
            }

            // Orientacao e escala usam as dimensoes DECODIFICADAS, que podem ser menores que as do arquivo.
            int larguraDecodificada = decodificada.Width;
            int alturaDecodificada = decodificada.Height;
            int larguraOrientadaDecodificada = trocaLadoELado ? alturaDecodificada : larguraDecodificada;
            int alturaOrientadaDecodificada = trocaLadoELado ? larguraDecodificada : alturaDecodificada;
            int ladoMaiorDecodificado = Math.Max(larguraOrientadaDecodificada, alturaOrientadaDecodificada);
            int ladoMaiorSaida = Math.Max(larguraSaida, alturaSaida);
            SKSamplingOptions amostragem = UsaFiltroRapido(ladoMaiorDecodificado, ladoMaiorSaida) ? AmostragemLinear : AmostragemReducao;

            SKImageInfo infoSaida = new SKImageInfo(larguraSaida, alturaSaida, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using SKBitmap saida = new SKBitmap(infoSaida);
            using SKCanvas canvas = new SKCanvas(saida);
            using SKPaint pincel = new SKPaint();

            canvas.Clear(SKColors.White);

            // Escala por ultimo: a orientacao corrige os pixels e so depois a imagem e reduzida. Sem ampliacao (ver CalcularDimensoesDeSaida).
            canvas.Scale((float)larguraSaida / larguraOrientadaDecodificada, (float)alturaSaida / alturaOrientadaDecodificada);
            SKMatrix orientacao = MontarOrientacao(origem, larguraDecodificada, alturaDecodificada);
            canvas.Concat(in orientacao);
            canvas.DrawBitmap(decodificada, 0, 0, amostragem, pincel);

            using SKData? codificada = saida.Encode(SKEncodedImageFormat.Jpeg, ConstantsValues.QualidadeJpegFotoVeiculo);
            if (codificada == null)
            {
                throw new InvalidOperationException(ConstantsMessage.ErroProcessarFotoVeiculo);
            }

            return new FotoProcessada(codificada.ToArray(), ConstantsArmazenamento.MimeJpeg, larguraSaida, alturaSaida);
        }

        /// <summary>
        /// Escolhe a escala de decodificacao do JPEG: 1/8, 1/4 e depois 1/2, nesta ordem, usando a primeira cujo lado maior e cujo lado
        /// menor ficam iguais ou maiores que os da saida (nunca amplia). PNG e qualquer outro formato sao decodificados inteiros.
        /// </summary>
        internal static SKSizeI EscolherDimensoesDeDecodificacao(SKCodec codec, int larguraOrientadaSaida, int alturaOrientadaSaida)
        {
            SKSizeI dimensoesOriginais = new SKSizeI(codec.Info.Width, codec.Info.Height);
            if (codec.EncodedFormat != SKEncodedImageFormat.Jpeg)
            {
                return dimensoesOriginais;
            }

            int ladoMaiorSaida = Math.Max(larguraOrientadaSaida, alturaOrientadaSaida);
            int ladoMenorSaida = Math.Min(larguraOrientadaSaida, alturaOrientadaSaida);
            float[] escalas = { ConstantsValues.EscalaDecodificacaoFotoOitavo, ConstantsValues.EscalaDecodificacaoFotoQuarto, ConstantsValues.EscalaDecodificacaoFotoMetade };

            foreach (float escala in escalas)
            {
                SKSizeI dimensoes = codec.GetScaledDimensions(escala);
                int ladoMaior = Math.Max(dimensoes.Width, dimensoes.Height);
                int ladoMenor = Math.Min(dimensoes.Width, dimensoes.Height);
                if (ladoMaior >= ladoMaiorSaida && ladoMenor >= ladoMenorSaida)
                {
                    return dimensoes;
                }
            }

            return dimensoesOriginais;
        }

        /// <summary>
        /// Usa o filtro linear rapido so quando o lado maior decodificado tem ate FatorResidualMaximoFiltroRapidoFoto vezes o lado maior
        /// da saida. Acima disso (reducao maior), segue o filtro Mitchell de sempre.
        /// </summary>
        internal static bool UsaFiltroRapido(int ladoMaiorDecodificado, int ladoMaiorSaida)
        {
            return ladoMaiorDecodificado <= ladoMaiorSaida * ConstantsValues.FatorResidualMaximoFiltroRapidoFoto;
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
