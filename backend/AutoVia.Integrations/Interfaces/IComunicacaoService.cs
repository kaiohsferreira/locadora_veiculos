using AutoVia.Integrations.Communication.Base;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Gateway HTTP generico (RestSharp). TODA chamada externa passa por aqui:
    /// nenhuma classe de provedor instancia HttpClient ou RestClient.
    /// </summary>
    public interface IComunicacaoService
    {
        /// <summary>
        /// Executa a chamada e devolve status, corpo desserializado e corpo bruto.
        /// Nao lanca excecao do RestSharp: falhas voltam em RequestResponse.
        /// </summary>
        public Task<RequestResponse<T>> SendAsync<T>(RequestInfo request, CancellationToken cancellationToken = default);
    }
}
