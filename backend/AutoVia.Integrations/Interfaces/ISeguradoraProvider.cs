using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Seguradora;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Provedor concreto da seguradora (hoje so FAKE). Escolhido pelo SeguradoraGateway pelo
    /// valor da chave 'provedor' da integracao SEGURADORA. Provedores reais usam
    /// IComunicacaoService; nenhum cria HttpClient.
    /// </summary>
    public interface ISeguradoraProvider
    {
        /// <summary>Nome do provedor (ex.: FAKE). Comparado sem diferenciar maiusculas.</summary>
        public string Nome { get; }

        public Task<VigenciaApoliceResultado> ConsultarVigenciaAsync(ConfiguracaoIntegracao configuracao, string numeroApolice, DateOnly dataReferencia, CancellationToken cancellationToken = default);

        public Task<SinistroSeguradoraResultado> AbrirSinistroAsync(ConfiguracaoIntegracao configuracao, SinistroAberturaSolicitacao solicitacao, CancellationToken cancellationToken = default);

        public Task<SinistroSeguradoraResultado> ConsultarSinistroAsync(ConfiguracaoIntegracao configuracao, string protocolo, CancellationToken cancellationToken = default);
    }
}
