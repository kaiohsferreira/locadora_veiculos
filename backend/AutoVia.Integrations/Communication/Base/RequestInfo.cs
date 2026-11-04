namespace AutoVia.Integrations.Communication.Base
{
    /// <summary>
    /// Tudo o que o IComunicacaoService precisa para fazer uma chamada externa.
    /// Montado pela classe Request do provedor a partir da ConfiguracaoIntegracao
    /// (BaseUrl e TimeoutMs vem do banco, nunca de valor fixo).
    /// </summary>
    public class RequestInfo
    {
        /// <summary>URL base da integracao (integration.base_url).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Caminho relativo ao BaseUrl (ex.: "/v1/cobrancas/pix").</summary>
        public string Resource { get; set; } = string.Empty;

        public HttpMethod Method { get; set; } = HttpMethod.Get;

        public Headers Headers { get; set; } = new Headers();

        public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>Corpo da requisicao: objeto (serializado em JSON) ou string ja pronta.</summary>
        public object? Body { get; set; }

        /// <summary>Tipo do corpo quando Body e string (padrao application/json).</summary>
        public string? ContentType { get; set; }

        /// <summary>Campos de formulario (application/x-www-form-urlencoded ou multipart).</summary>
        public Dictionary<string, string> FormFields { get; set; } = new Dictionary<string, string>();

        /// <summary>Arquivos de upload; quando houver, a requisicao vai como multipart/form-data.</summary>
        public List<RequestFile> Files { get; set; } = new List<RequestFile>();

        /// <summary>Timeout em milissegundos (integration.timeout_ms).</summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Permite nova tentativa em falha de rede ou 5xx (maximo 3, espera
        /// progressiva). Para POST/PATCH so tem efeito se houver IdempotencyKey:
        /// operacao que cria cobranca nunca e repetida sem chave de idempotencia.
        /// </summary>
        public bool AllowRetry { get; set; }

        /// <summary>Chave de idempotencia enviada no cabecalho Idempotency-Key.</summary>
        public string? IdempotencyKey { get; set; }

        /// <summary>Valores que devem ser mascarados nos logs (preencher com ConfiguracaoIntegracao.ProtectedValues).</summary>
        public List<string> SensitiveValues { get; set; } = new List<string>();
    }
}
