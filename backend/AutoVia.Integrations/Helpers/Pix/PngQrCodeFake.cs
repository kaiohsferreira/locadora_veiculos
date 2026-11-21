using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Integrations.Helpers.Pix
{
    /// <summary>
    /// PNG de teste do provedor FAKE: quadriculado de DimensaoQrCodeFake x DimensaoQrCodeFake modulos (TamanhoModuloQrCodeFake px
    /// cada), em tons de cinza, sorteado pelo SHA-256 do identificador. Sem dependencia nova: cabecalho, IHDR, IDAT (ZLibStream),
    /// IEND e CRC32 sao escritos aqui. Mesmo identificador, mesmos bytes. NAO e um QR Code: nenhum app de banco le esta imagem.
    /// </summary>
    public static class PngQrCodeFake
    {
        private static readonly byte[] AssinaturaPng = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly uint[] TabelaCrc = CriarTabelaCrc();

        private const byte CorEscura = 0;
        private const byte CorClara = 255;
        private const byte FiltroSemFiltro = 0;
        private const byte ProfundidadeDeBits = 8;
        private const byte TipoTonsDeCinza = 0;
        private const byte MetodoPadrao = 0;
        private const byte SemEntrelacamento = 0;
        private const int BlocosDeHashParaModulos = 4;
        private const int BytesPorHash = 32;
        private const int BitsPorByte = 8;
        private const int TamanhoCabecalhoChunk = 8;
        private const int TamanhoCrc = 4;
        private const uint PolinomioCrc = 0xEDB88320;
        private const uint MascaraCrc = 0xFFFFFFFF;
        private const int BitsPorBytePosicao = 1;

        /// <summary>Gera o PNG (bytes) do identificador informado. Deterministico.</summary>
        public static byte[] Gerar(string identificador)
        {
            int dimensao = ConstantsPix.DimensaoQrCodeFake;
            int modulo = ConstantsPix.TamanhoModuloQrCodeFake;
            int lado = dimensao * modulo;
            bool[] modulos = SortearModulos(identificador, dimensao * dimensao);
            byte[] linhas = MontarLinhas(modulos, dimensao, modulo, lado);
            byte[] idat = Compactar(linhas);

            using MemoryStream png = new MemoryStream();
            png.Write(AssinaturaPng, 0, AssinaturaPng.Length);
            EscreverChunk(png, "IHDR", MontarIhdr(lado));
            EscreverChunk(png, "IDAT", idat);
            EscreverChunk(png, "IEND", new byte[0]);

            return png.ToArray();
        }

        /// <summary>Sorteia cada modulo (escuro ou claro) a partir dos bits de SHA-256 do identificador, em quatro blocos.</summary>
        private static bool[] SortearModulos(string identificador, int quantidade)
        {
            byte[] entrada = Encoding.UTF8.GetBytes(identificador);
            byte[] sorteio = new byte[BlocosDeHashParaModulos * BytesPorHash];

            for (int bloco = 0; bloco < BlocosDeHashParaModulos; bloco++)
            {
                byte[] mistura = new byte[entrada.Length + 1];
                Array.Copy(entrada, mistura, entrada.Length);
                mistura[entrada.Length] = (byte)bloco;
                byte[] hash = SHA256.HashData(mistura);
                Array.Copy(hash, 0, sorteio, bloco * BytesPorHash, BytesPorHash);
            }

            bool[] modulos = new bool[quantidade];

            for (int i = 0; i < quantidade; i++)
            {
                int bit = (sorteio[i / BitsPorByte] >> (i % BitsPorByte)) & BitsPorBytePosicao;
                modulos[i] = bit == 1;
            }

            return modulos;
        }

        /// <summary>Linhas do PNG: um byte de filtro (zero) e um byte de cor por pixel, com cada modulo repetido em bloco.</summary>
        private static byte[] MontarLinhas(bool[] modulos, int dimensao, int modulo, int lado)
        {
            byte[] linhas = new byte[lado * (lado + 1)];
            int posicao = 0;

            for (int y = 0; y < lado; y++)
            {
                linhas[posicao] = FiltroSemFiltro;
                posicao++;

                for (int x = 0; x < lado; x++)
                {
                    bool escuro = modulos[(y / modulo) * dimensao + (x / modulo)];
                    linhas[posicao] = escuro ? CorEscura : CorClara;
                    posicao++;
                }
            }

            return linhas;
        }

        /// <summary>Compacta as linhas no formato zlib (RFC 1950), que e o que o PNG aceita no IDAT.</summary>
        private static byte[] Compactar(byte[] linhas)
        {
            using MemoryStream saida = new MemoryStream();

            using (ZLibStream zlib = new ZLibStream(saida, CompressionLevel.Optimal, true))
            {
                zlib.Write(linhas, 0, linhas.Length);
            }

            return saida.ToArray();
        }

        /// <summary>IHDR: largura, altura (ambas iguais), 8 bits por canal, tons de cinza, metodo padrao e sem entrelacamento.</summary>
        private static byte[] MontarIhdr(int lado)
        {
            byte[] ihdr = new byte[13];
            EscreverInteiroBigEndian(ihdr, 0, lado);
            EscreverInteiroBigEndian(ihdr, 4, lado);
            ihdr[8] = ProfundidadeDeBits;
            ihdr[9] = TipoTonsDeCinza;
            ihdr[10] = MetodoPadrao;
            ihdr[11] = MetodoPadrao;
            ihdr[12] = SemEntrelacamento;

            return ihdr;
        }

        /// <summary>Escreve um chunk: tamanho, tipo, dados e CRC32 calculado sobre tipo e dados.</summary>
        private static void EscreverChunk(MemoryStream destino, string tipo, byte[] dados)
        {
            byte[] cabecalho = new byte[TamanhoCabecalhoChunk];
            EscreverInteiroBigEndian(cabecalho, 0, dados.Length);
            Encoding.ASCII.GetBytes(tipo, 0, tipo.Length, cabecalho, 4);
            destino.Write(cabecalho, 0, cabecalho.Length);
            destino.Write(dados, 0, dados.Length);

            byte[] tipoEDados = new byte[tipo.Length + dados.Length];
            Encoding.ASCII.GetBytes(tipo, 0, tipo.Length, tipoEDados, 0);
            Array.Copy(dados, 0, tipoEDados, tipo.Length, dados.Length);

            byte[] crc = new byte[TamanhoCrc];
            EscreverInteiroBigEndian(crc, 0, (int)CalcularCrc32(tipoEDados));
            destino.Write(crc, 0, crc.Length);
        }

        private static void EscreverInteiroBigEndian(byte[] destino, int posicao, int valor)
        {
            destino[posicao] = (byte)((valor >> 24) & 0xFF);
            destino[posicao + 1] = (byte)((valor >> 16) & 0xFF);
            destino[posicao + 2] = (byte)((valor >> 8) & 0xFF);
            destino[posicao + 3] = (byte)(valor & 0xFF);
        }

        /// <summary>CRC32 (polinomio 0xEDB88320) calculado a mao, com a tabela de 256 entradas.</summary>
        private static uint CalcularCrc32(byte[] dados)
        {
            uint crc = MascaraCrc;

            foreach (byte b in dados)
            {
                crc = TabelaCrc[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }

            return crc ^ MascaraCrc;
        }

        private static uint[] CriarTabelaCrc()
        {
            uint[] tabela = new uint[256];

            for (uint n = 0; n < tabela.Length; n++)
            {
                uint c = n;

                for (int k = 0; k < BitsPorByte; k++)
                {
                    c = (c & 1) != 0 ? PolinomioCrc ^ (c >> 1) : c >> 1;
                }

                tabela[n] = c;
            }

            return tabela;
        }
    }
}
