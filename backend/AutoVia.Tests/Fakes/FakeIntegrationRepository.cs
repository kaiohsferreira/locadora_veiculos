using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Tests.Fakes
{
    /// <summary>IIntegrationRepository em memoria, com contador de consultas para testar o cache.</summary>
    public class FakeIntegrationRepository : IIntegrationRepository
    {
        private readonly Dictionary<string, Integration> _integrations = new Dictionary<string, Integration>(StringComparer.OrdinalIgnoreCase);

        public int QueryCount { get; private set; }

        public void Add(Integration integration)
        {
            _integrations[integration.Codigo] = integration;
        }

        public Task<Integration?> FindByCodigoWithSettingsAsync(string codigo, CancellationToken cancellationToken = default)
        {
            QueryCount++;
            Integration? integration;
            _integrations.TryGetValue(codigo, out integration);
            return Task.FromResult(integration);
        }
    }
}
