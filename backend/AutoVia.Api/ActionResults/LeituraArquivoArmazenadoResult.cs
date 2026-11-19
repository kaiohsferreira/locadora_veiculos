using AutoVia.Api.Middleware;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Helpers.Armazenamento;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Api.ActionResults
{
    /// <summary>
    /// Resposta da leitura publica de arquivo: 200 com os bytes ja em memoria, ou 304 sem corpo quando o ETag do cliente confere.
    /// Escreve os cabecalhos de seguranca e de cache e envia os bytes dentro de um try proprio. Cliente que encerra a conexao no
    /// meio do envio gera so um registro informativo (nao e erro). Falha de escrita gera um registro so com o nome do tipo.
    /// Nenhuma excecao sai deste resultado.
    /// </summary>
    public sealed class LeituraArquivoArmazenadoResult : IActionResult
    {
        private readonly ArquivoParaLeitura _arquivo;
        private readonly ILogService _logService;

        public LeituraArquivoArmazenadoResult(ArquivoParaLeitura arquivo, ILogService logService)
        {
            _arquivo = arquivo;
            _logService = logService;
        }

        /// <summary>Escreve 304 (sem corpo) ou 200 com os bytes; cancelamento vira registro informativo e falha vira 500 sem relancar.</summary>
        public async Task ExecuteResultAsync(ActionContext context)
        {
            HttpResponse resposta = context.HttpContext.Response;
            CancellationToken cancelamento = context.HttpContext.RequestAborted;

            try
            {
                resposta.Headers[HeaderNames.ETag] = ConstantsValues.CaractereAspasEtag + _arquivo.ETag + ConstantsValues.CaractereAspasEtag;
                resposta.Headers[HeaderNames.CacheControl] = _arquivo.DocumentoDoCliente
                    ? ConstantsValues.CacheControlSemArmazenamento
                    : ConstantsValues.CacheControlLeituraArquivo;

                if (_arquivo.NaoModificado)
                {
                    resposta.StatusCode = StatusCodes.Status304NotModified;
                    return;
                }

                resposta.StatusCode = StatusCodes.Status200OK;
                resposta.ContentType = _arquivo.ContentType;
                resposta.ContentLength = _arquivo.Conteudo.Length;
                resposta.Headers[ConstantsValues.NomeCabecalhoXContentTypeOptions] = ConstantsValues.ValorNaoFarejarTipo;
                resposta.Headers[ConstantsValues.NomeCabecalhoContentSecurityPolicy] = ConstantsValues.PoliticaConteudoLeituraArquivo;
                resposta.Headers[HeaderNames.ContentDisposition] = string.Equals(_arquivo.ContentType, ConstantsValues.TipoConteudoPdf, StringComparison.Ordinal)
                    ? ConstantsValues.ContentDispositionAnexoPdf
                    : ConstantsArmazenamento.ContentDispositionInline;

                await resposta.Body.WriteAsync(_arquivo.Conteudo, cancelamento);
            }
            catch (Exception) when (cancelamento.IsCancellationRequested)
            {
                _logService.WriteInfo(ConstantsMessage.LogArmazenamentoLeituraCancelada, GetType().ToString());
            }
            catch (Exception ex)
            {
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroArmazenamentoAbrirArquivo, GetType().ToString());

                if (resposta.HasStarted)
                {
                    context.HttpContext.Abort();
                }
                else
                {
                    await ProblemDetailsWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status500InternalServerError,
                        ConstantsMessage.TituloErroInterno,
                        ConstantsMessage.ErroInterno);
                }
            }
        }
    }
}
