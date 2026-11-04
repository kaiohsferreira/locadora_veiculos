using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Interfaces;
using RestSharp;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace AutoVia.Integrations.Services
{
    /// <summary>
    /// Gateway HTTP generico implementado com RestSharp. Um unico RestClient
    /// reaproveitado (singleton), sem URL base: cada RequestInfo traz a sua, lida
    /// do banco.
    ///
    /// Regras aplicadas aqui, para valerem em todos os provedores:
    /// - timeout sempre o do RequestInfo (integration.timeout_ms);
    /// - nova tentativa so em falha de rede e 5xx, no maximo 3, com espera
    ///   progressiva, e nunca em POST/PATCH sem chave de idempotencia;
    /// - excecao do RestSharp nunca sobe: vira RequestResponse com ErrorMessage;
    /// - log sem valores protegidos (RequestInfo.SensitiveValues sao mascarados).
    /// </summary>
    public sealed class ComunicacaoService : IComunicacaoService, IDisposable
    {
        private readonly RestClient _client;
        private readonly ILogService _logService;

        public ComunicacaoService(ILogService logService)
        {
            _logService = logService;

            RestClientOptions options = new RestClientOptions
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            _client = new RestClient(options);
        }

        public async Task<RequestResponse<T>> SendAsync<T>(RequestInfo request, CancellationToken cancellationToken = default)
        {
            RequestResponse<T> response = new RequestResponse<T>();
            string urlForLog = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(request.BaseUrl) || request.Method == null)
                {
                    response.ErrorMessage = ConstantsMessageIntegration.ErroComunicacaoRequisicaoInvalida;
                    return response;
                }

                string url = BuildUrl(request.BaseUrl, request.Resource);
                urlForLog = SensitiveDataMasker.Mask(url, request.SensitiveValues);

                int maxAttempts = CanRetry(request) ? ConstantsIntegration.MaximoTentativas : 1;
                Stopwatch stopwatch = Stopwatch.StartNew();

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    RestRequest restRequest = BuildRestRequest(url, request);
                    RestResponse restResponse = await _client.ExecuteAsync(restRequest, cancellationToken);

                    bool isNetworkFailure = restResponse.ResponseStatus != ResponseStatus.Completed;
                    bool isServerError = (int)restResponse.StatusCode >= 500;

                    if ((isNetworkFailure || isServerError) && attempt < maxAttempts)
                    {
                        _logService.WriteInfo(
                            $"{request.Method} {urlForLog} falhou (status {(int)restResponse.StatusCode}, {restResponse.ResponseStatus}); nova tentativa {attempt + 1} de {maxAttempts}.",
                            GetType().ToString());

                        await Task.Delay(CalculateDelay(attempt), cancellationToken);
                        continue;
                    }

                    stopwatch.Stop();
                    response = MapResponse<T>(restResponse, attempt, stopwatch.ElapsedMilliseconds);
                    LogResult(request, urlForLog, response);
                    return response;
                }

                return response;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Cancelamento pedido pelo chamador (requisicao abortada): nao e falha do provedor.
                throw;
            }
            catch (Exception ex)
            {
                string friendlyMessage = ConstantsMessageIntegration.ErroComunicacaoInesperado;
                _logService.Write(
                    SensitiveDataMasker.Mask($"{request.Method} {urlForLog}: {ex.Message}", request.SensitiveValues),
                    friendlyMessage,
                    GetType().ToString());

                response.IsSuccess = false;
                response.ErrorMessage = friendlyMessage;
                return response;
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        // ------------------------------------------------------------------
        // Montagem da requisicao
        // ------------------------------------------------------------------

        private static string BuildUrl(string baseUrl, string resource)
        {
            if (string.IsNullOrWhiteSpace(resource))
                return baseUrl;

            return $"{baseUrl.TrimEnd('/')}/{resource.TrimStart('/')}";
        }

        private static RestRequest BuildRestRequest(string url, RequestInfo request)
        {
            RestRequest restRequest = new RestRequest(url, MapMethod(request.Method));

            if (request.TimeoutMs > 0)
                restRequest.Timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);

            foreach (KeyValuePair<string, string> header in request.Headers)
            {
                restRequest.AddHeader(header.Key, header.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                restRequest.AddHeader(ConstantsIntegration.CabecalhoIdempotencia, request.IdempotencyKey);

            foreach (KeyValuePair<string, string> query in request.QueryParameters)
            {
                restRequest.AddQueryParameter(query.Key, query.Value);
            }

            foreach (KeyValuePair<string, string> field in request.FormFields)
            {
                restRequest.AddParameter(field.Key, field.Value);
            }

            if (request.Files.Count > 0)
            {
                restRequest.AlwaysMultipartFormData = true;
                foreach (RequestFile file in request.Files)
                {
                    restRequest.AddFile(file.FieldName, file.Content, file.FileName, file.ContentType);
                }
            }

            if (request.Body != null)
            {
                string body = request.Body as string ?? IntegrationSerializer.Serialize(request.Body);
                string contentType = string.IsNullOrWhiteSpace(request.ContentType) ? ContentType.Json : request.ContentType;
                restRequest.AddStringBody(body, contentType);
            }

            return restRequest;
        }

        private static Method MapMethod(HttpMethod method)
        {
            if (method == HttpMethod.Post) return Method.Post;
            if (method == HttpMethod.Put) return Method.Put;
            if (method == HttpMethod.Patch) return Method.Patch;
            if (method == HttpMethod.Delete) return Method.Delete;
            if (method == HttpMethod.Head) return Method.Head;
            if (method == HttpMethod.Options) return Method.Options;
            return Method.Get;
        }

        /// <summary>
        /// Metodos idempotentes podem ser repetidos. POST e PATCH so com chave de
        /// idempotencia, para nunca criar duas cobrancas na nova tentativa.
        /// </summary>
        private static bool CanRetry(RequestInfo request)
        {
            if (!request.AllowRetry)
                return false;

            bool isIdempotentMethod = request.Method == HttpMethod.Get
                || request.Method == HttpMethod.Head
                || request.Method == HttpMethod.Put
                || request.Method == HttpMethod.Delete
                || request.Method == HttpMethod.Options;

            return isIdempotentMethod || !string.IsNullOrWhiteSpace(request.IdempotencyKey);
        }

        /// <summary>Espera progressiva: 500 ms, depois 1000 ms.</summary>
        private static TimeSpan CalculateDelay(int attempt)
        {
            int delayMs = ConstantsIntegration.EsperaBaseMs * (int)Math.Pow(2, attempt - 1);
            return TimeSpan.FromMilliseconds(delayMs);
        }

        // ------------------------------------------------------------------
        // Leitura da resposta
        // ------------------------------------------------------------------

        private static RequestResponse<T> MapResponse<T>(RestResponse restResponse, int attempts, long elapsedMilliseconds)
        {
            RequestResponse<T> response = new RequestResponse<T>
            {
                StatusCode = (int)restResponse.StatusCode,
                RawContent = restResponse.Content,
                RawBytes = restResponse.RawBytes,
                Attempts = attempts,
                ElapsedMilliseconds = elapsedMilliseconds,
                IsTimeout = restResponse.ResponseStatus == ResponseStatus.TimedOut,
                IsNetworkFailure = restResponse.ResponseStatus != ResponseStatus.Completed
            };

            if (restResponse.Headers != null)
            {
                foreach (HeaderParameter header in restResponse.Headers)
                {
                    if (!string.IsNullOrEmpty(header.Name))
                        response.Headers[header.Name] = header.Value ?? string.Empty;
                }
            }

            if (response.IsTimeout)
            {
                response.ErrorMessage = ConstantsMessageIntegration.ErroComunicacaoTimeout;
                return response;
            }

            if (response.IsNetworkFailure)
            {
                response.ErrorMessage = ConstantsMessageIntegration.ErroComunicacaoFalhaRede;
                return response;
            }

            bool isSuccessStatus = restResponse.StatusCode >= HttpStatusCode.OK && (int)restResponse.StatusCode <= 299;

            if (!isSuccessStatus)
            {
                // O Service do provedor le RawContent para montar a mensagem de negocio.
                response.ErrorMessage = ConstantsMessage.ErroIntegracaoExterna;
                return response;
            }

            try
            {
                response.Data = ReadData<T>(restResponse);
                response.IsSuccess = true;
            }
            catch (JsonException)
            {
                response.IsSuccess = false;
                response.ErrorMessage = ConstantsMessageIntegration.ErroComunicacaoDesserializar;
            }

            return response;
        }

        private static T? ReadData<T>(RestResponse restResponse)
        {
            if (typeof(T) == typeof(byte[]))
                return (T?)(object?)restResponse.RawBytes;

            if (typeof(T) == typeof(string))
                return (T?)(object?)restResponse.Content;

            if (string.IsNullOrWhiteSpace(restResponse.Content))
                return default;

            return IntegrationSerializer.Deserialize<T>(restResponse.Content);
        }

        private void LogResult<T>(RequestInfo request, string urlForLog, RequestResponse<T> response)
        {
            string summary = $"{request.Method} {urlForLog} -> {response.StatusCode} em {response.ElapsedMilliseconds} ms ({response.Attempts} tentativa(s)).";

            if (response.IsSuccess)
            {
                _logService.WriteInfo(summary, GetType().ToString());
                return;
            }

            _logService.Write(summary, response.ErrorMessage ?? ConstantsMessage.ErroIntegracaoExterna, GetType().ToString());
        }
    }
}
