using SkiaSharp;
using System;
using System.IO;
using System.Text;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Gera as imagens dos testes do processador de foto em memoria (sem arquivo binario no repositorio e sem rede).
    /// Os cabecalhos de PNG e o bloco EXIF sao montados byte a byte, para o teste controlar exatamente o que o codec le.
    /// </summary>
    internal static class ImagensDeTesteFoto
    {
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] CabecalhoExif = { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 };
        private static readonly byte[] SequenciaExif = { 0x45, 0x78, 0x69, 0x66 };
        private const int SementeRuido = 20261116;

        /// <summary>JPEG com a cor dada, qualidade 90.</summary>
        internal static byte[] GerarJpeg(int largura, int altura, SKColor cor)
        {
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            bitmap.Erase(cor);
            return Codificar(bitmap, SKEncodedImageFormat.Jpeg, 90);
        }

        /// <summary>JPEG com a metade esquerda vermelha e a direita azul (para conferir a orientacao).</summary>
        internal static byte[] GerarJpegMetadeVermelhaEsquerda(int largura, int altura)
        {
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            for (int x = 0; x < largura; x++)
            {
                SKColor cor = x < largura / 2 ? SKColors.Red : SKColors.Blue;
                for (int y = 0; y < altura; y++)
                {
                    bitmap.SetPixel(x, y, cor);
                }
            }

            return Codificar(bitmap, SKEncodedImageFormat.Jpeg, 95);
        }

        /// <summary>PNG totalmente transparente (alpha zero), para conferir o fundo branco.</summary>
        internal static byte[] GerarPngTransparente(int largura, int altura)
        {
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            bitmap.Erase(SKColors.Transparent);
            return Codificar(bitmap, SKEncodedImageFormat.Png, 100);
        }

        /// <summary>PNG opaco com a cor dada.</summary>
        internal static byte[] GerarPng(int largura, int altura, SKColor cor)
        {
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            bitmap.Erase(cor);
            return Codificar(bitmap, SKEncodedImageFormat.Png, 100);
        }

        /// <summary>
        /// Insere um segmento APP1 com EXIF de orientacao logo depois do SOI do JPEG. O bloco TIFF e little-endian, com uma entrada
        /// (tag 0x0112, SHORT, valor da orientacao) e sem GPS nem outras tags.
        /// </summary>
        internal static byte[] InserirExifOrientacao(byte[] jpeg, byte orientacao)
        {
            byte[] tiff =
            {
                0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00,
                0x01, 0x00,
                0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientacao, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00,
            };
            int comprimentoSegmento = 2 + CabecalhoExif.Length + tiff.Length;
            byte[] marcadorApp1 = { 0xFF, 0xE1, (byte)(comprimentoSegmento >> 8), (byte)(comprimentoSegmento & 0xFF) };

            return Concatenar(jpeg.AsSpan(0, 2).ToArray(), marcadorApp1, CabecalhoExif, tiff, jpeg.AsSpan(2).ToArray());
        }

        /// <summary>
        /// JPEG com ruido de semente fixa (dados que ocupam varios KB, nao cor lisa), para o corte no fim do arquivo cair nas linhas codificadas.
        /// </summary>
        internal static byte[] GerarJpegComRuido(int largura, int altura)
        {
            Random gerador = new Random(SementeRuido);
            using SKBitmap bitmap = new SKBitmap(largura, altura);
            for (int x = 0; x < largura; x++)
            {
                for (int y = 0; y < altura; y++)
                {
                    bitmap.SetPixel(x, y, new SKColor((byte)gerador.Next(256), (byte)gerador.Next(256), (byte)gerador.Next(256)));
                }
            }

            return Codificar(bitmap, SKEncodedImageFormat.Jpeg, 95);
        }

        /// <summary>JPEG cortado em 90% do tamanho: o cabecalho continua valido, mas o fim dos dados das linhas nao existe.</summary>
        internal static byte[] Truncar(byte[] dados)
        {
            return dados.AsSpan(0, dados.Length * 9 / 10).ToArray();
        }

        /// <summary>
        /// PNG que so DECLARA as dimensoes: assinatura, IHDR com largura e altura, um IDAT minimo e IEND, com CRC validos.
        /// Nenhum pixel existe; serve para provar que a recusa acontece pelo cabecalho, sem alocar o bitmap.
        /// </summary>
        internal static byte[] PngQueDeclaraDimensoes(int largura, int altura)
        {
            byte[] dadosIhdr = Concatenar(
                BigEndian(largura),
                BigEndian(altura),
                new byte[] { 0x08, 0x06, 0x00, 0x00, 0x00 });
            byte[] idatVazio = { 0x78, 0x9C, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 };

            return Concatenar(
                AssinaturaPng,
                Bloco("IHDR", dadosIhdr),
                Bloco("IDAT", idatVazio),
                Bloco("IEND", new byte[0]));
        }

        /// <summary>Confere se a sequencia de bytes aparece em qualquer ponto dos dados.</summary>
        internal static bool ContemSequencia(byte[] dados, byte[] sequencia)
        {
            for (int inicio = 0; inicio + sequencia.Length <= dados.Length; inicio++)
            {
                bool bate = true;
                for (int deslocamento = 0; deslocamento < sequencia.Length; deslocamento++)
                {
                    if (dados[inicio + deslocamento] != sequencia[deslocamento])
                    {
                        bate = false;
                        break;
                    }
                }

                if (bate)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sequencia de bytes do marcador EXIF ("Exif").</summary>
        internal static byte[] SequenciaDoExif()
        {
            return SequenciaExif;
        }

        private static byte[] Codificar(SKBitmap bitmap, SKEncodedImageFormat formato, int qualidade)
        {
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData? dados = imagem.Encode(formato, qualidade);
            if (dados == null)
            {
                throw new InvalidOperationException();
            }

            return dados.ToArray();
        }

        private static byte[] Bloco(string tipo, byte[] dados)
        {
            byte[] tipoBytes = Encoding.ASCII.GetBytes(tipo);
            byte[] corpoParaCrc = Concatenar(tipoBytes, dados);

            return Concatenar(BigEndian(dados.Length), corpoParaCrc, BigEndian((int)Crc32(corpoParaCrc)));
        }

        private static byte[] BigEndian(int valor)
        {
            return new byte[] { (byte)(valor >> 24), (byte)(valor >> 16), (byte)(valor >> 8), (byte)valor };
        }

        private static uint Crc32(byte[] dados)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte valor in dados)
            {
                crc ^= valor;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
                }
            }

            return ~crc;
        }

        private static byte[] Concatenar(params byte[][] partes)
        {
            using MemoryStream fluxo = new MemoryStream();
            foreach (byte[] parte in partes)
            {
                fluxo.Write(parte, 0, parte.Length);
            }

            return fluxo.ToArray();
        }
    }
}
