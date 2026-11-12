using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Helpers.Google;

namespace AutoVia.Integrations.Requests.Google
{
    /// <summary>
    /// Monta a RequestInfo do conjunto de chaves publicas do Google (JWKS). Classe pura: nao faz IO.
    /// GET e idempotente, por isso pode repetir em falha de rede ou 5xx. Nao leva segredo: a chamada e publica.
    /// </summary>
    public static class GoogleCertificadosRequest
    {
        /// <summary>GET {base_url}/oauth2/v3/certs. Timeout vem da integracao GOOGLE (integration.timeout_ms).</summary>
        public static RequestInfo Montar(ConfiguracaoIntegracao configuracao)
        {
            return new RequestInfo
            {
                BaseUrl = configuracao.BaseUrl,
                Resource = ConstantsGoogle.CaminhoCertificados,
                Method = HttpMethod.Get,
                TimeoutMs = configuracao.TimeoutMs,
                AllowRetry = true
            };
        }
    }
}
