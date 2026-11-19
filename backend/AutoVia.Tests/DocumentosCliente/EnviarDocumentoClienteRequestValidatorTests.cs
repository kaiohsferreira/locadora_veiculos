using AutoVia.Api.Requests.ClienteDocumento;
using AutoVia.Api.Validators;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Armazenamento;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AutoVia.Tests.DocumentosCliente
{
    /// <summary>
    /// Validador do envio de documento do cliente: tipo (ausente, fora da lista, nome exato), arquivo (ausente, vazio, tamanho
    /// no limite e acima, tipo declarado, assinatura e tipo declarado igual ao conteudo). Cada recusa traz UMA mensagem no campo
    /// certo, e PDF, JPG e PNG validos passam.
    /// </summary>
    public sealed class EnviarDocumentoClienteRequestValidatorTests
    {
        private const string NomeCampoTipo = "Tipo";
        private const string NomeCampoArquivo = "Arquivo";
        private const string NomeArquivo = "documento.pdf";
        private const string TipoCnh = "CNH";
        private const string TipoComprovante = "COMPROVANTE_RESIDENCIA";
        private const byte BytePadrao = 0x00;
        private const int TamanhoLimite = ConstantsValues.TamanhoMaximoArquivoArmazenamentoBytes;
        private const string MimeGif = "image/gif";
        private static readonly byte[] AssinaturaJpeg = { 0xFF, 0xD8, 0xFF, 0xE0 };
        private static readonly byte[] AssinaturaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] ConteudoExe = { 0x4D, 0x5A, 0x90, 0x00 };

        private static IFormFile CriarArquivo(byte[] conteudo, string contentType)
        {
            MemoryStream fluxo = new MemoryStream(conteudo);

            return new FormFile(fluxo, 0, conteudo.Length, ConstantsValues.CampoArquivoDocumentoCliente, NomeArquivo)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static byte[] ComCabecalho(byte[] cabecalho, int tamanho)
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

        private static async Task<ValidationResult> ValidarAsync(string? tipo, IFormFile? arquivo)
        {
            EnviarDocumentoClienteRequestValidator validador = new EnviarDocumentoClienteRequestValidator();
            EnviarDocumentoClienteRequest request = new EnviarDocumentoClienteRequest { Tipo = tipo, Arquivo = arquivo };

            return await validador.ValidateAsync(request);
        }

        private static async Task AssertUmaMensagemAsync(string? tipo, IFormFile? arquivo, string campoEsperado, string mensagemEsperada)
        {
            ValidationResult resultado = await ValidarAsync(tipo, arquivo);

            Assert.False(resultado.IsValid);
            Assert.Single(resultado.Errors);
            Assert.Equal(campoEsperado, resultado.Errors[0].PropertyName);
            Assert.Equal(mensagemEsperada, resultado.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task TipoAusente_RecusaComMensagemDeObrigatorio()
        {
            await AssertUmaMensagemAsync(null, CriarArquivo(ConstantsValues.AssinaturaPdf, ConstantsValues.TipoConteudoPdf), NomeCampoTipo, ConstantsMessage.ErroClienteDocumentoTipoObrigatorio);
        }

        [Fact]
        public async Task TipoEmBranco_RecusaComoAusente()
        {
            await AssertUmaMensagemAsync("   ", CriarArquivo(ConstantsValues.AssinaturaPdf, ConstantsValues.TipoConteudoPdf), NomeCampoTipo, ConstantsMessage.ErroClienteDocumentoTipoObrigatorio);
        }

        [Theory]
        [InlineData("cnh")]
        [InlineData("0")]
        [InlineData("RG")]
        [InlineData("COMPROVANTE")]
        public async Task TipoForaDaLista_RecusaComMensagemDeTipoInvalido(string tipo)
        {
            await AssertUmaMensagemAsync(tipo, CriarArquivo(ConstantsValues.AssinaturaPdf, ConstantsValues.TipoConteudoPdf), NomeCampoTipo, ConstantsMessage.ErroClienteDocumentoTipoInvalido);
        }

        [Fact]
        public async Task TipoComEspacosEmVolta_EhAceitoDepoisDoTrim()
        {
            ValidationResult resultado = await ValidarAsync("  " + TipoComprovante + " ", CriarArquivo(AssinaturaJpeg, ConstantsArmazenamento.MimeJpeg));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task ArquivoAusente_RecusaComMensagemDeObrigatorio()
        {
            await AssertUmaMensagemAsync(TipoCnh, null, NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoArquivoObrigatorio);
        }

        [Fact]
        public async Task ArquivoVazio_RecusaComMensagemDeVazio()
        {
            await AssertUmaMensagemAsync(TipoCnh, CriarArquivo(new byte[0], ConstantsValues.TipoConteudoPdf), NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoArquivoVazio);
        }

        [Fact]
        public async Task ArquivoNoLimiteDeDezMegabytes_Passa()
        {
            byte[] noLimite = ComCabecalho(ConstantsValues.AssinaturaPdf, TamanhoLimite);

            ValidationResult resultado = await ValidarAsync(TipoCnh, CriarArquivo(noLimite, ConstantsValues.TipoConteudoPdf));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task ArquivoUmBytePassaDoLimite_RecusaComMensagemDeTamanho()
        {
            byte[] acima = ComCabecalho(ConstantsValues.AssinaturaPdf, TamanhoLimite + 1);

            await AssertUmaMensagemAsync(TipoCnh, CriarArquivo(acima, ConstantsValues.TipoConteudoPdf), NomeCampoArquivo, string.Format(System.Globalization.CultureInfo.InvariantCulture, ConstantsMessage.ErroClienteDocumentoTamanho, ConstantsValues.TamanhoMaximoArquivoDocumentoClienteMb));
        }

        [Fact]
        public async Task TipoDeclaradoForaDaLista_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(TipoCnh, CriarArquivo(AssinaturaPng, MimeGif), NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoFormato);
        }

        [Fact]
        public async Task ConteudoQueNaoEPdfJpgNemPng_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(TipoCnh, CriarArquivo(ConteudoExe, ConstantsValues.TipoConteudoPdf), NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoFormato);
        }

        [Fact]
        public async Task DeclaradoDiferenteDoConteudo_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(TipoCnh, CriarArquivo(ConstantsValues.AssinaturaPdf, ConstantsArmazenamento.MimeJpeg), NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoFormato);
        }

        [Fact]
        public async Task PngDeclaradoComoJpeg_RecusaComMensagemDeFormato()
        {
            await AssertUmaMensagemAsync(TipoComprovante, CriarArquivo(AssinaturaPng, ConstantsArmazenamento.MimeJpeg), NomeCampoArquivo, ConstantsMessage.ErroClienteDocumentoFormato);
        }

        [Fact]
        public async Task PdfValido_Passa()
        {
            ValidationResult resultado = await ValidarAsync(TipoCnh, CriarArquivo(ComCabecalho(ConstantsValues.AssinaturaPdf, 64), ConstantsValues.TipoConteudoPdf));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task JpgValido_Passa()
        {
            ValidationResult resultado = await ValidarAsync(TipoComprovante, CriarArquivo(ComCabecalho(AssinaturaJpeg, 64), ConstantsArmazenamento.MimeJpeg));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task PngValido_Passa()
        {
            ValidationResult resultado = await ValidarAsync(TipoComprovante, CriarArquivo(ComCabecalho(AssinaturaPng, 64), ConstantsArmazenamento.MimePng));

            Assert.True(resultado.IsValid);
        }

        [Fact]
        public async Task PdfQueChegaUmByteDeCadaVez_AindaPassa()
        {
            byte[] conteudo = ComCabecalho(ConstantsValues.AssinaturaPdf, 64);
            FormFile arquivo = new FormFile(new FluxoDeUmByteParaTeste(conteudo), 0, conteudo.Length, ConstantsValues.CampoArquivoDocumentoCliente, NomeArquivo)
            {
                Headers = new HeaderDictionary(),
                ContentType = ConstantsValues.TipoConteudoPdf
            };

            ValidationResult resultado = await ValidarAsync(TipoCnh, arquivo);

            Assert.True(resultado.IsValid);
        }

        /// <summary>Fluxo que devolve um byte por leitura, como uma conexao lenta: o cabecalho tem de ser lido em laco.</summary>
        private sealed class FluxoDeUmByteParaTeste : MemoryStream
        {
            private const int BytesPorLeitura = 1;

            public FluxoDeUmByteParaTeste(byte[] conteudo)
                : base(conteudo)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(BytesPorLeitura, count));
            }

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return base.ReadAsync(buffer.Slice(0, Math.Min(BytesPorLeitura, buffer.Length)), cancellationToken);
            }
        }
    }
}
