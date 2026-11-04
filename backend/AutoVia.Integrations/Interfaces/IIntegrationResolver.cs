using AutoVia.Integrations.Communication.Base;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Ponto unico de configuracao das integracoes: recebe o codigo, carrega
    /// integration + integration_setting e devolve a configuracao pronta, com
    /// cache curto em memoria.
    /// </summary>
    public interface IIntegrationResolver
    {
        /// <summary>
        /// Resolve a configuracao da integracao. Lanca IntegrationException se ela
        /// nao existir, estiver inativa ou com base_url invalida.
        /// </summary>
        public Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default);

        /// <summary>Remove a integracao do cache (apos alterar a configuracao no banco).</summary>
        public void Invalidate(string codigo);
    }
}
