using AutoVia.Api.ActionResults;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Resultado proprio da leitura publica: 200 com os cabecalhos e os bytes, 304 sem corpo, cliente que encerra a conexao
    /// no meio do envio (registro informativo, nenhuma excecao) e falha de escrita sem cancelamento (um registro so, com o
    /// nome do tipo, e 500 quando a resposta ainda nao comecou). Corpo de teste lanca na primeira escrita.
    /// </summary>
    public sealed class LeituraArquivoArmazenadoResultTests
    {
        private const string EtagAtual = "1a-2b";
        private const string CabecalhoXContentTypeOptions = "X-Content-Type-Options";
        private const string CabecalhoContentSecurityPolicy = "Content-Security-Policy";
        private const string CaminhoNaFalha = "/dados/armazenamento/arquivos/veiculos/1/fotos/abc.jpg";

        /// <summary>Corpo de resposta que lanca a falha informada na primeira escrita e depois grava normalmente.</summary>
        private sealed class CorpoComFalhaNaPrimeiraEscrita : MemoryStream
        {
            private Exception? _falha;

            public CorpoComFalhaNaPrimeiraEscrita(Exception falha)
            {
                _falha = falha;
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_falha != null)
                {
                    Exception falha = _falha;
                    _falha = null;
                    throw falha;
                }

                return base.WriteAsync(buffer, cancellationToken);
            }
        }

        /// <summary>ILogService que separa as sobrecargas (texto, excecao e informativo).</summary>
        private sealed class LogCapturado : ILogService
        {
            public List<string> Mensagens { get; } = new List<string>();

            public List<string> Amigaveis { get; } = new List<string>();

            public List<Exception> Excecoes { get; } = new List<Exception>();

            public List<string> Informativos { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                Mensagens.Add(message);
                Amigaveis.Add(friendlyMessage);
            }

            public void Write(Exception exception, string friendlyMessage, string source)
            {
                Excecoes.Add(exception);
            }

            public void WriteInfo(string message, string source)
            {
                Informativos.Add(message);
            }
        }

        private static ActionContext CriarContexto(DefaultHttpContext contexto)
        {
            return new ActionContext(contexto, new RouteData(), new ActionDescriptor());
        }

        private static DefaultHttpContext CriarHttpContext(Stream corpo)
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.Response.Body = corpo;
            return contexto;
        }

        private static string LerCorpo(DefaultHttpContext contexto)
        {
            Stream corpo = contexto.Response.Body;
            corpo.Seek(0, SeekOrigin.Begin);
            using StreamReader leitor = new StreamReader(corpo, Encoding.UTF8, false, 1024, true);
            return leitor.ReadToEnd();
        }

        [Fact]
        public async Task Duzentos_TemBytesECabecalhosDeSegurancaECache()
        {
            byte[] bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x10 };
            DefaultHttpContext contexto = CriarHttpContext(new MemoryStream());
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(bytes, ConstantsArmazenamento.MimeJpeg, EtagAtual, false), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
            Assert.Equal(ConstantsArmazenamento.MimeJpeg, contexto.Response.ContentType);
            Assert.Equal(bytes.Length, contexto.Response.ContentLength);
            Assert.Equal("\"" + EtagAtual + "\"", contexto.Response.Headers[HeaderNames.ETag].ToString());
            Assert.Equal(ConstantsValues.CacheControlLeituraArquivo, contexto.Response.Headers[HeaderNames.CacheControl].ToString());
            Assert.Equal(ConstantsValues.ValorNaoFarejarTipo, contexto.Response.Headers[CabecalhoXContentTypeOptions].ToString());
            Assert.Equal(ConstantsValues.PoliticaConteudoLeituraArquivo, contexto.Response.Headers[CabecalhoContentSecurityPolicy].ToString());
            Assert.Equal(ConstantsArmazenamento.ContentDispositionInline, contexto.Response.Headers[HeaderNames.ContentDisposition].ToString());
            Assert.Equal(bytes, ((MemoryStream)contexto.Response.Body).ToArray());
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
            Assert.Empty(log.Informativos);
        }

        [Fact]
        public async Task NaoModificado_DevolveTrezentosEQuatroSemCorpoComEtagECache()
        {
            DefaultHttpContext contexto = CriarHttpContext(new MemoryStream());
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[0], ConstantsArmazenamento.MimeJpeg, EtagAtual, true), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(StatusCodes.Status304NotModified, contexto.Response.StatusCode);
            Assert.Equal("\"" + EtagAtual + "\"", contexto.Response.Headers[HeaderNames.ETag].ToString());
            Assert.Equal(ConstantsValues.CacheControlLeituraArquivo, contexto.Response.Headers[HeaderNames.CacheControl].ToString());
            Assert.Equal(0, ((MemoryStream)contexto.Response.Body).Length);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task Pdf_SaiComoAnexoComNomeFixoECacheDeFoto()
        {
            byte[] bytes = ConstantsValues.AssinaturaPdf.Concat(new byte[] { 0x31, 0x0A }).ToArray();
            DefaultHttpContext contexto = CriarHttpContext(new MemoryStream());
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(bytes, ConstantsValues.TipoConteudoPdf, EtagAtual, false), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(StatusCodes.Status200OK, contexto.Response.StatusCode);
            Assert.Equal(ConstantsValues.TipoConteudoPdf, contexto.Response.ContentType);
            Assert.Equal(ConstantsValues.ContentDispositionAnexoPdf, contexto.Response.Headers[HeaderNames.ContentDisposition].ToString());
            Assert.Equal(ConstantsValues.CacheControlLeituraArquivo, contexto.Response.Headers[HeaderNames.CacheControl].ToString());
            Assert.Equal(bytes, ((MemoryStream)contexto.Response.Body).ToArray());
        }

        [Fact]
        public async Task Imagem_SaiComoInlineSemAnexo()
        {
            DefaultHttpContext contexto = CriarHttpContext(new MemoryStream());
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[] { 1, 2 }, ConstantsArmazenamento.MimePng, EtagAtual, false), new LogCapturado());

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(ConstantsArmazenamento.ContentDispositionInline, contexto.Response.Headers[HeaderNames.ContentDisposition].ToString());
        }

        [Fact]
        public async Task DocumentoDoCliente_200ETer304ComNoStore()
        {
            DefaultHttpContext contexto = CriarHttpContext(new MemoryStream());
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[] { 1, 2 }, ConstantsValues.TipoConteudoPdf, EtagAtual, false, true), new LogCapturado());

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(ConstantsValues.CacheControlSemArmazenamento, contexto.Response.Headers[HeaderNames.CacheControl].ToString());

            DefaultHttpContext naoModificado = CriarHttpContext(new MemoryStream());
            LeituraArquivoArmazenadoResult condicional = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(Array.Empty<byte>(), ConstantsValues.TipoConteudoPdf, EtagAtual, true, true), new LogCapturado());

            await condicional.ExecuteResultAsync(CriarContexto(naoModificado));

            Assert.Equal(StatusCodes.Status304NotModified, naoModificado.Response.StatusCode);
            Assert.Equal(ConstantsValues.CacheControlSemArmazenamento, naoModificado.Response.Headers[HeaderNames.CacheControl].ToString());
        }

        [Fact]
        public async Task ClienteEncerraNoMeioDoEnvio_NaoSaiExcecaoERegistraSoInformativo()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            DefaultHttpContext contexto = CriarHttpContext(new CorpoComFalhaNaPrimeiraEscrita(new OperationCanceledException(cancelamento.Token)));
            contexto.RequestAborted = cancelamento.Token;
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[] { 1, 2 }, ConstantsArmazenamento.MimeJpeg, EtagAtual, false), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Single(log.Informativos);
            Assert.Equal(ConstantsMessage.LogArmazenamentoLeituraCancelada, log.Informativos[0]);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task ClienteEncerraNoMeioDoEnvioComIOException_NaoSaiExcecaoERegistraSoInformativo()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();
            DefaultHttpContext contexto = CriarHttpContext(new CorpoComFalhaNaPrimeiraEscrita(new IOException(CaminhoNaFalha)));
            contexto.RequestAborted = cancelamento.Token;
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[] { 1, 2 }, ConstantsArmazenamento.MimeJpeg, EtagAtual, false), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Single(log.Informativos);
            Assert.Equal(ConstantsMessage.LogArmazenamentoLeituraCancelada, log.Informativos[0]);
            Assert.Empty(log.Mensagens);
            Assert.Empty(log.Excecoes);
        }

        [Fact]
        public async Task FalhaDeEscritaSemCancelamento_RegistraUmaVezSoComONomeDoTipoERespondeOs500()
        {
            DefaultHttpContext contexto = CriarHttpContext(new CorpoComFalhaNaPrimeiraEscrita(new IOException(CaminhoNaFalha)));
            LogCapturado log = new LogCapturado();
            LeituraArquivoArmazenadoResult resultado = new LeituraArquivoArmazenadoResult(new ArquivoParaLeitura(new byte[] { 1, 2 }, ConstantsArmazenamento.MimeJpeg, EtagAtual, false), log);

            await resultado.ExecuteResultAsync(CriarContexto(contexto));

            Assert.Equal(StatusCodes.Status500InternalServerError, contexto.Response.StatusCode);
            Assert.Single(log.Mensagens);
            Assert.Equal(nameof(IOException), log.Mensagens[0]);
            Assert.Equal(ConstantsMessage.ErroArmazenamentoAbrirArquivo, log.Amigaveis[0]);
            Assert.Empty(log.Excecoes);
            Assert.Empty(log.Informativos);
            Assert.DoesNotContain(CaminhoNaFalha, LerCorpo(contexto));
        }
    }
}
