using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using SkiaSharp;

namespace AutoVia.Tests.Avarias
{
    public sealed class AvariaServiceTests
    {
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
    }
}
