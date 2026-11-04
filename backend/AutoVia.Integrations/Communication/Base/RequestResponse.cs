namespace AutoVia.Integrations.Communication.Base
{
    /// <summary>
    /// Resultado de uma chamada externa. O gateway nunca deixa excecao do
    /// RestSharp subir: falha de rede, timeout e erro HTTP chegam aqui, e o
    /// Service do provedor decide como converter em excecao de negocio.
    /// </summary>
    public class RequestResponse<T>
    {
        /// <summary>Codigo HTTP; 0 quando nao houve resposta (falha de rede ou timeout).</summary>
        public int StatusCode { get; set; }

        /// <summary>true quando houve resposta 2xx e o corpo foi lido sem erro.</summary>
        public bool IsSuccess { get; set; }

        /// <summary>Corpo desserializado (null em erro ou corpo vazio).</summary>
        public T? Data { get; set; }

        /// <summary>Corpo bruto em texto, util para ler a mensagem de erro do provedor.</summary>
        public string? RawContent { get; set; }

        /// <summary>Corpo bruto em bytes (download de arquivo, PDF de infracao).</summary>
        public byte[]? RawBytes { get; set; }

        public Headers Headers { get; set; } = new Headers();

        /// <summary>Mensagem amigavel em portugues quando a chamada falhou.</summary>
        public string? ErrorMessage { get; set; }

        public bool IsNetworkFailure { get; set; }

        public bool IsTimeout { get; set; }

        public long ElapsedMilliseconds { get; set; }

        /// <summary>Quantas tentativas foram feitas (1 sem nova tentativa).</summary>
        public int Attempts { get; set; }
    }
}
