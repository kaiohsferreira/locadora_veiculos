using AutoVia.Integrations.Entities;

namespace AutoVia.Integrations.Interfaces
{
    /// <summary>
    /// Acesso de leitura as tabelas integration/integration_setting. Definido
    /// aqui e implementado na Infrastructure (IntegrationRepository), para que a
    /// biblioteca leia o banco sem depender do DbContext.
    /// </summary>
    public interface IIntegrationRepository
    {
        /// <summary>Busca a integracao ativa (disabled_at nulo) pelo codigo, com os settings. Null se nao existir.</summary>
        public Task<Integration?> FindByCodigoWithSettingsAsync(string codigo, CancellationToken cancellationToken = default);
    }
}
