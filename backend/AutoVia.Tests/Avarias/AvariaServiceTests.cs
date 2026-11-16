using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using AutoVia.Integrations.Interfaces;
using AutoVia.Tests.Fakes;
using SkiaSharp;
using System.Text;

namespace AutoVia.Tests.Avarias
{
    public sealed class AvariaServiceTests
    {
        private const int IdVistoriaDoTeste = 14;
        private const int IdAvariaDoTeste = 1;
        private const int LadoAcimaDoLimiteFotoPixels = 8193;
        private const string ChaveDaFoto = "vistorias/14/avarias/1/foto";
        private const string MarcadorExif = "Exif";
        private const string TextoDoErroDeArmazenamento = "Storage unavailable";
        private const string NomeTipoErroDeArmazenamento = "InvalidOperationException";

        [Fact]
        public async Task CriarAsync_CriaAvariaFazUploadEDevolveUrl()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);

            AutoVia.Communication.ViewObjects.Avarias.ReturnAvariaVO retorno = await servico.CriarAsync(14, "Arranhão", "MEDIA", 22.50m, JpegValido(), "image/jpeg");

            Assert.Equal(1, repositorio.CriacaoCount);
            Assert.Equal(1, armazenamento.UploadCount);
            Assert.Equal("vistorias/14/avarias/1/foto", armazenamento.UploadKeys[0]);
            Assert.True(armazenamento.ObjetoExiste("vistorias/14/avarias/1/foto"));
            Assert.Equal("https://storage.example/foto", retorno.Foto?.Url);
            Assert.Equal(GravidadeAvaria.MEDIA, retorno.Gravidade);
        }

        [Fact]
        public async Task UploadFalho_PermiteRetrySemCriarAvariaDuplicada()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway { FalharUpload = true };
            AvariaService servico = CriarServico(repositorio, armazenamento);

            AvariaUploadException excecao = await Assert.ThrowsAsync<AvariaUploadException>(() => servico.CriarAsync(14, "Arranhão", "LEVE", null, JpegValido(), "image/jpeg"));
            armazenamento.FalharUpload = false;
            AutoVia.Communication.ViewObjects.Avarias.ReturnAvariaVO retorno = await servico.RetryUploadAsync(excecao.IdVistoria, excecao.IdAvaria, JpegValido(), "image/jpeg");

            Assert.Equal(1, repositorio.CriacaoCount);
            Assert.Equal(2, armazenamento.UploadCount);
            Assert.Equal("vistorias/14/avarias/1/foto", armazenamento.UploadKeys[0]);
            Assert.Equal(armazenamento.UploadKeys[0], armazenamento.UploadKeys[1]);
            Assert.True(armazenamento.ObjetoExiste(armazenamento.UploadKeys[0]));
            Assert.NotNull(retorno.Foto?.Url);
        }

        [Fact]
        public async Task RefreshUrl_ConfirmaObjetoEDeleteRepetidoPreservaRegistro()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);
            await servico.CriarAsync(14, "Arranhão", "GRAVE", null, JpegValido(), "image/jpeg");

            await servico.GetFotoUrlAsync(14, 1);
            await servico.RemoverFotoAsync(14, 1);
            await servico.RemoverFotoAsync(14, 1);

            Assert.Equal(1, armazenamento.HeadCount);
            Assert.Equal(2, armazenamento.DeleteCount);
            Assert.NotNull(await repositorio.FindAtivaAsync(14, 1));
            Assert.False(armazenamento.ObjetoExiste("vistorias/14/avarias/1/foto"));
        }

        [Fact]
        public async Task FotoComAssinaturaInvalida_FalhaAntesDeCriarRegistro()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            AvariaService servico = CriarServico(repositorio, new FakeArmazenamentoGateway());

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(14, "Arranhão", "LEVE", null, new byte[] { 1, 2, 3 }, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
        }

        [Fact]
        public async Task FotoComCabecalhoMasConteudoMalformado_FalhaAntesDeCriarRegistro()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            AvariaService servico = CriarServico(repositorio, new FakeArmazenamentoGateway());

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(14, "Arranhão", "LEVE", null, new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
        }

        [Fact]
        public async Task VistoriaInexistente_ComAssinaturaJpegEConteudoQueNaoDecodifica_Responde404SemDecodificar()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository { VistoriaExiste = false };
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task IdVistoriaZeroOuNegativo_Responde404SemGravarNemEnviar(int idVistoria)
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(idVistoria, "Arranhão", "LEVE", null, JpegValido(), "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaVistoriaNaoEncontrada, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Fact]
        public async Task FotoAcimaDeOLimiteDePixels_FalhaComMensagemNoErrorsEAntesDeGravar()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, JpegComLado(LadoAcimaDoLimiteFotoPixels), "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Message);
            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Errors.First().ErrorMessage);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Fact]
        public async Task JpegTruncado_FalhaAntesDeGravarNemEnviar()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);
            byte[] jpeg = JpegGrande();
            byte[] truncado = jpeg.Take(jpeg.Length / 2).ToArray();

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, truncado, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Fact]
        public async Task ContentTypeJpegComBytesDePng_FalhaAntesDeGravarNemEnviar()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, PngValido(), "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoFormatoInvalido, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Fact]
        public async Task FotoAcimaDoTetoDeBytes_FalhaAntesDeGravarNemEnviar()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway();
            AvariaService servico = CriarServico(repositorio, armazenamento);
            byte[] acimaDoLimite = new byte[ConstantsArmazenamento.TamanhoMaximoFotoBytes + 1];
            acimaDoLimite[0] = 0xFF;
            acimaDoLimite[1] = 0xD8;
            acimaDoLimite[2] = 0xFF;

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, acimaDoLimite, "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaFotoObrigatoria, excecao.Message);
            Assert.Equal(0, repositorio.CriacaoCount);
            Assert.Equal(0, armazenamento.UploadCount);
        }

        [Fact]
        public async Task GravidadeInvalida_MensagemEspecificaChegaAoErrors()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            AvariaService servico = CriarServico(repositorio, new FakeArmazenamentoGateway());

            FluentValidation.ValidationException excecao = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "CRITICA", null, JpegValido(), "image/jpeg"));

            Assert.Equal(ConstantsMessage.ErroAvariaGravidadeInvalida, excecao.Errors.First().ErrorMessage);
            Assert.Equal(0, repositorio.CriacaoCount);
        }

        [Fact]
        public async Task RegravacaoDaFoto_NaoLevaOsMetadadosExifDoOriginal()
        {
            FakeAvariaRepository repositorio = new FakeAvariaRepository();
            CapturadorDeArmazenamento armazenamento = new CapturadorDeArmazenamento();
            AvariaService servico = new AvariaService(repositorio, armazenamento, new FakeLogService());
            byte[] comExif = JpegComExif();

            Assert.Contains(MarcadorExif, Encoding.ASCII.GetString(comExif), StringComparison.Ordinal);
            await servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, comExif, "image/jpeg");

            Assert.NotNull(armazenamento.ConteudoEnviado);
            Assert.DoesNotContain(MarcadorExif, Encoding.ASCII.GetString(armazenamento.ConteudoEnviado!), StringComparison.Ordinal);
            Assert.Equal(ChaveDaFoto, armazenamento.ChaveEnviada);
        }

        [Fact]
        public async Task LogDaVistoriaInexistenteEDaFotoInvalida_DeixaRegistro()
        {
            FakeLogService log = new FakeLogService();
            FakeAvariaRepository repositorio = new FakeAvariaRepository { VistoriaExiste = false };
            AvariaService servico = new AvariaService(repositorio, new FakeArmazenamentoGateway(), log);

            await Assert.ThrowsAsync<NotFoundException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, JpegValido(), "image/jpeg"));
            repositorio.VistoriaExiste = true;
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, new byte[] { 1, 2, 3 }, "image/jpeg"));

            Assert.NotEmpty(log.Entries);
            Assert.Equal(2, log.Entries.Count);
        }

        [Fact]
        public async Task FalhaDeEnvio_LogTrazONomeDoTipoERessalvaAMensagemNemAUrl()
        {
            FakeLogService log = new FakeLogService();
            FakeArmazenamentoGateway armazenamento = new FakeArmazenamentoGateway { FalharUpload = true };
            AvariaService servico = new AvariaService(new FakeAvariaRepository(), armazenamento, log);

            await Assert.ThrowsAsync<AvariaUploadException>(() => servico.CriarAsync(IdVistoriaDoTeste, "Arranhão", "LEVE", null, JpegValido(), "image/jpeg"));

            List<string> entradas = log.Snapshot();
            Assert.NotEmpty(entradas);
            Assert.Contains(entradas, entrada => entrada.Contains(NomeTipoErroDeArmazenamento, StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains(TextoDoErroDeArmazenamento, StringComparison.Ordinal));
            Assert.DoesNotContain(entradas, entrada => entrada.Contains("https://", StringComparison.Ordinal));
        }

        private static AvariaService CriarServico(FakeAvariaRepository repositorio, FakeArmazenamentoGateway armazenamento)
        {
            return new AvariaService(repositorio, armazenamento, new FakeLogService());
        }

        private static byte[] JpegValido()
        {
            using SKBitmap bitmap = new SKBitmap(1, 1);
            bitmap.Erase(SKColors.White);
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 90);
            return dados.ToArray();
        }

        private static byte[] PngValido()
        {
            using SKBitmap bitmap = new SKBitmap(1, 1);
            bitmap.Erase(SKColors.White);
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData dados = imagem.Encode(SKEncodedImageFormat.Png, 100);
            return dados.ToArray();
        }

        private static byte[] JpegComLado(int largura)
        {
            using SKBitmap bitmap = new SKBitmap(largura, 1);
            bitmap.Erase(SKColors.White);
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 90);
            return dados.ToArray();
        }

        /// <summary>JPEG de 256x256 com pixels variados, para que o arquivo seja grande o bastante para ser cortado ao meio.</summary>
        private static byte[] JpegGrande()
        {
            const int lado = 256;
            using SKBitmap bitmap = new SKBitmap(lado, lado);
            for (int y = 0; y < lado; y++)
            {
                for (int x = 0; x < lado; x++)
                {
                    bitmap.SetPixel(x, y, new SKColor((byte)(x * 7 + y * 3), (byte)(x * 13 ^ y * 5), (byte)(x * y)));
                }
            }
            using SKImage imagem = SKImage.FromBitmap(bitmap);
            using SKData dados = imagem.Encode(SKEncodedImageFormat.Jpeg, 90);
            return dados.ToArray();
        }

        /// <summary>JPEG valido com um segmento APP1 "Exif" logo depois do SOI (bytes FF D8), como uma foto de celular.</summary>
        private static byte[] JpegComExif()
        {
            byte[] jpeg = JpegGrande();
            byte[] cabecalhoExif = new byte[] { 0x45, 0x78, 0x69, 0x66, 0x00, 0x00 };
            byte[] tiff = new byte[] { 0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            int tamanhoSegmento = 2 + cabecalhoExif.Length + tiff.Length;
            List<byte> resultado = new List<byte> { jpeg[0], jpeg[1], 0xFF, 0xE1, (byte)(tamanhoSegmento >> 8), (byte)(tamanhoSegmento & 0xFF) };
            resultado.AddRange(cabecalhoExif);
            resultado.AddRange(tiff);
            resultado.AddRange(jpeg.Skip(2));
            return resultado.ToArray();
        }

        /// <summary>Armazenamento que guarda os bytes enviados, para conferir o que de fato foi gravado.</summary>
        private sealed class CapturadorDeArmazenamento : IArmazenamentoGateway
        {
            public byte[]? ConteudoEnviado { get; private set; }
            public string? ChaveEnviada { get; private set; }

            public Task<ArquivoArmazenado> EnviarAsync(string chave, byte[] conteudo, string contentType, CancellationToken cancellationToken = default)
            {
                ChaveEnviada = chave;
                ConteudoEnviado = conteudo;
                return Task.FromResult(new ArquivoArmazenado("https://storage.example/foto", DateTimeOffset.UtcNow.AddMinutes(5)));
            }

            public Task<ArquivoArmazenado?> ObterUrlAsync(string chave, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ArquivoArmazenado?>(null);
            }

            public Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
