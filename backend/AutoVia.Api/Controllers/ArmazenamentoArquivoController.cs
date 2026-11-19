using AutoVia.Api.ActionResults;
using AutoVia.Api.Extensions;
using AutoVia.Api.Middleware;
using AutoVia.Api.Requests.Armazenamento;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Armazenamento;
using AutoVia.Integrations.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoVia.Api.Controllers
{
    /// <summary>
    /// Leitura publica de arquivo do armazenamento em pasta, por link assinado (chave, exp e sig). Sem login: a imagem da tela
    /// nao envia o cabecalho Authorization; a protecao e o link assinado e curto. Sem regra de negocio aqui: o servico de
    /// leitura confere o link e o arquivo. Toda recusa de link (400 e 404) e o 429 geram registro (ver a acao).
    /// </summary>
    [ApiController]
    [Route(ConstantsValues.RotaLeituraArquivoArmazenado)]
    [AllowAnonymous]
    public sealed class ArmazenamentoArquivoController : ControllerBase
    {
        private readonly IArmazenamentoLeituraService _leituraService;
        private readonly IValidator<LerArquivoArmazenadoRequest> _validator;
        private readonly TetoLeituraArquivo _tetoLeitura;
        private readonly RegistroRecusasLimiteLeitura _registroRecusas;
        private readonly ILogService _logService;

        public ArmazenamentoArquivoController(
            IArmazenamentoLeituraService leituraService,
            IValidator<LerArquivoArmazenadoRequest> validator,
            TetoLeituraArquivo tetoLeitura,
            RegistroRecusasLimiteLeitura registroRecusas,
            ILogService logService)
        {
            _leituraService = leituraService;
            _validator = validator;
            _tetoLeitura = tetoLeitura;
            _registroRecusas = registroRecusas;
            _logService = logService;
        }

        /// <summary>
        /// Ler - Devolve o arquivo quando o link (chave, exp e sig) confere e nao venceu: 200 com os bytes, ou 304 quando o
        /// If-None-Match do navegador confere. Recusas: 400 para link malformado; 404 para as demais (mesmo corpo); 429 pelo
        /// limite por IP (antes da acao) ou pelo teto da rota; 500 para falha nao prevista.
        /// </summary>
        [HttpGet]
        [EnableRateLimiting(ConstantsValues.PoliticaLimiteLeituraArquivo)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Ler(
            [FromQuery] string? chave,
            [FromQuery] string? exp,
            [FromQuery] string? sig)
        {
            CabecalhoReferrerLeituraArquivo.Registrar(HttpContext.Response);
            try
            {
                if (!_tetoLeitura.TentarEntrar())
                {
                    _registroRecusas.RegistrarTeto();
                    throw new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes);
                }

                LerArquivoArmazenadoRequest request = new LerArquivoArmazenadoRequest
                {
                    Chave = chave,
                    Exp = exp,
                    Sig = sig,
                    QuantidadeChave = ContarParametro(ConstantsValues.ParametroChaveLink),
                    QuantidadeExp = ContarParametro(ConstantsValues.ParametroExpiracaoLink),
                    QuantidadeSig = ContarParametro(ConstantsValues.ParametroAssinaturaLink)
                };

                ValidationResult resultado = await _validator.ValidateAsync(request, HttpContext.RequestAborted);
                if (!resultado.IsValid)
                {
                    _logService.Write(ConstantsMessage.LogArmazenamentoLeituraLinkMalformado, ConstantsMessage.ErroArmazenamentoLinkInvalido, GetType().ToString());
                    throw new FluentValidation.ValidationException(resultado.Errors);
                }

                long expiraEmUnix = long.Parse(exp!, NumberStyles.None, CultureInfo.InvariantCulture);
                List<string> etagsDoCliente = LerEtagsDoCliente(HttpContext.Request);

                ArquivoParaLeitura arquivo = await _leituraService.AbrirAsync(
                    chave!,
                    expiraEmUnix,
                    sig!,
                    etagsDoCliente,
                    HttpContext.RequestAborted);

                return new LeituraArquivoArmazenadoResult(arquivo, _logService);
            }
            catch (NotFoundException)
            {
                throw;
            }
            catch (FluentValidation.ValidationException)
            {
                throw;
            }
            catch (LimiteExcedidoException)
            {
                throw;
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                _logService.WriteInfo(ConstantsMessage.LogArmazenamentoLeituraCancelada, GetType().ToString());
                return new EmptyResult();
            }
            catch (ArmazenamentoLeituraException)
            {
                // Falha ja registrada pelo servico de leitura: aqui so responde o 500, sem registro novo e sem relancar.
                await ProblemDetailsWriter.WriteAsync(HttpContext, StatusCodes.Status500InternalServerError, ConstantsMessage.TituloErroInterno, ConstantsMessage.ErroInterno);
                return new EmptyResult();
            }
            catch (Exception ex)
            {
                // Falha da propria acao: um registro, so com o nome do tipo (sem mensagem, caminho, chave ou pilha).
                _logService.Write(ex.GetType().Name, ConstantsMessage.ErroArmazenamentoAbrirArquivo, GetType().ToString());
                await ProblemDetailsWriter.WriteAsync(HttpContext, StatusCodes.Status500InternalServerError, ConstantsMessage.TituloErroInterno, ConstantsMessage.ErroInterno);
                return new EmptyResult();
            }
        }

        /// <summary>Conta quantas vezes o parametro veio na query (repetido e recusado pelo validador).</summary>
        private int ContarParametro(string nome)
        {
            return HttpContext.Request.Query[nome].Count;
        }

        /// <summary>
        /// Le o If-None-Match: cada valor entra sem aspas e sem o prefixo W/. Cabecalho ausente, ilegivel ou com o curinga '*'
        /// (que nao e um ETag) vira lista vazia, e o servico devolve o arquivo.
        /// </summary>
        private static List<string> LerEtagsDoCliente(HttpRequest requisicao)
        {
            List<string> etags = new List<string>();
            List<string> valores = new List<string>();

            foreach (string? valor in requisicao.Headers[HeaderNames.IfNoneMatch])
            {
                if (valor != null && valor.Trim() != ConstantsValues.CuringaIfNoneMatch)
                {
                    valores.Add(valor);
                }
            }

            if (valores.Count > 0 && EntityTagHeaderValue.TryParseList(valores, out IList<EntityTagHeaderValue>? lidos) && lidos != null)
            {
                foreach (EntityTagHeaderValue etag in lidos)
                {
                    etags.Add(RemoverAspas(etag.Tag.Value ?? string.Empty));
                }
            }

            return etags;
        }

        /// <summary>Tira as aspas que cercam o ETag, quando existem.</summary>
        private static string RemoverAspas(string valor)
        {
            bool entreAspas = valor.Length >= 2 && valor[0] == ConstantsValues.CaractereAspasEtag && valor[valor.Length - 1] == ConstantsValues.CaractereAspasEtag;
            return entreAspas ? valor.Substring(1, valor.Length - 2) : valor;
        }
    }
}
