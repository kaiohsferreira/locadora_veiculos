using AutoVia.Api.Requests.VeiculoFoto;
using AutoVia.Api.Validators;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Validador do arquivo do envio de foto: ausente, vazio, tamanho, tipo declarado e assinatura. Cada recusa traz UMA
    /// mensagem em errors.foto (campo Foto), e JPEG e PNG validos passam.
    /// </summary>
    public sealed class EnviarFotoVeiculoRequestValidatorTests
    {
        private const string NomeCampo = "Foto";
        private const string NomeArquivo = "foto.jpg";
        private const byte BytePadrao = 0x00;
        private static readonly byte[] AssinaturaJpeg = { 0xFF, 0xD8, 0xFF, 0xE0 };
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] ConteudoExe = { 0x4D, 0x5A, 0x90, 0x00 };

        private static IFormFile CriarArquivo(byte[] conteudo, string contentType)
        {
            MemoryStream fluxo = new MemoryStream(conteudo);
            return new FormFile(fluxo, 0, conteudo.Length, ConstantsValues.CampoFotoVeiculo, NomeArquivo)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] ComTamanho(byte[] cabecalho, int tamanho)
        {
            byte[] conteudo = new byte[tamanho];
            for (int i = 0; i < cabecalho.Length && i < tamanho; i++)
            {
                conteudo[i] = cabecalho[i];
            }

            for (int i = cabecalho.Length; i < tamanho; i++)
            {
                conteudo[i] = BytePadrao;
            }

            return conteudo;
        }

        private static async Task<ValidationResult> ValidarAsync(IFormFile? foto)
        {
            EnviarFotoVeiculoRequestValidator validador = new EnviarFotoVeiculoRequestValidator();
            EnviarFotoVeiculoRequest request = new EnviarFotoVeiculoRequest { Foto = foto };

            return await validador.ValidateAsync(request);
        }

        private static async Task AssertUmaMensagemAsync(IFormFile? foto, string mensagemEsperada)
        {
            ValidationResult resultado = await ValidarAsync(foto);

            Assert.False(resultado.IsValid);
            Assert.Single(resultado.Errors);
            Assert.Equal(NomeCampo, resultado.Errors[0].PropertyName);
            Assert.Equal(mensagemEsperada, resultado.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task SemArquivo_RecusaComMensagemDeObrigatoria()
        {
            await AssertUmaMensagemAsync(null, ConstantsMessage.ErroVeiculoFotoObrigatoria);
        }

        [Fact]
        public async Task ArquivoVazio_RecusaComMensagemDeObrigatoria()
        {
            await AssertUmaMensagemAsync(CriarArquivo(new byte[0], ConstantsArmazenamento.MimeJpeg), ConstantsMessage.ErroVeiculoFotoObrigatoria);
        }

        [Fact]
        public async Task ArquivoAcimaDoTamanhoMaximo_RecusaComMensagemDeTamanho()
        {
            byte[] acima = ComTamanho(AssinaturaJpeg, ConstantsArmazenamento.TamanhoMaximoFotoBytes + 1);

            await AssertUmaMensagemAsync(CriarArquivo(acima, ConstantsArmazenamento.MimeJpeg), ConstantsMessage.ErroVeiculoFotoTamanho);
        }

        [Fact]
        public async Task ArquivoExeRenomeadoComTipoJpeg_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(CriarArquivo(ConteudoExe, ConstantsArmazenamento.MimeJpeg), ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task TipoJpegComBytesDePng_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(CriarArquivo(AssinaturaPng, ConstantsArmazenamento.MimeJpeg), ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task TipoPdf_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(CriarArquivo(AssinaturaJpeg, "application/pdf"), ConstantsMessage.ErroVeiculoFotoFormatoInvalido);
        }

        [Fact]
        public async Task JpegValido_Passa()
        {
            ValidationResult resultado = await ValidarAsync(CriarArquivo(AssinaturaJpeg, ConstantsArmazenamento.MimeJpeg));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task PngValido_Passa()
        {
            ValidationResult resultado = await ValidarAsync(CriarArquivo(AssinaturaPng, ConstantsArmazenamento.MimePng));

            Assert.True(resultado.IsValid);
        }
    }
}
