using AutoVia.Communication.CustomExceptions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Integrations.Services
{
    /// <summary>
    /// Carrega integration + integration_setting pelo codigo e devolve a
    /// ConfiguracaoIntegracao pronta, com cache em memoria de curta duracao
    /// (ConstantsIntegration.CacheSegundos). So configuracao valida entra no cache.
    /// </summary>
    public class IntegrationResolver : IIntegrationResolver
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IMemoryCache _cache;
        private readonly ILogService _logService;

        public IntegrationResolver(IIntegrationRepository integrationRepository, IMemoryCache cache, ILogService logService)
        {
            _integrationRepository = integrationRepository;
            _cache = cache;
            _logService = logService;
        }

        public async Task<ConfiguracaoIntegracao> ResolveAsync(string codigo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new IntegrationException(string.Empty, ConstantsMessageIntegration.ErroCodigoNaoInformado);

            string codigoNormalizado = codigo.Trim().ToUpperInvariant();
            string cacheKey = BuildCacheKey(codigoNormalizado);

            try
            {
                ConfiguracaoIntegracao? cached;
                if (_cache.TryGetValue(cacheKey, out cached) && cached != null)
                    return cached;

                Integration? integration = await _integrationRepository.FindByCodigoWithSettingsAsync(codigoNormalizado, cancellationToken);

                if (integration == null)
                {
                    throw new IntegrationException(
                        codigoNormalizado,
                        string.Format(ConstantsMessageIntegration.ErroIntegracaoNaoEncontrada, codigoNormalizado));
                }

                if (!integration.Ativo)
                {
                    throw new IntegrationException(
                        codigoNormalizado,
                        string.Format(ConstantsMessageIntegration.ErroIntegracaoInativa, codigoNormalizado));
                }

                Uri? baseUri;
                if (!Uri.TryCreate(integration.BaseUrl, UriKind.Absolute, out baseUri))
                {
                    throw new IntegrationException(
                        codigoNormalizado,
                        string.Format(ConstantsMessageIntegration.ErroBaseUrlInvalida, codigoNormalizado));
                }

                Dictionary<string, IntegrationSettingValue> settings = integration.Settings
                    .GroupBy(x => x.Chave, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => new IntegrationSettingValue(group.First().Valor, group.First().Protegido),
                        StringComparer.OrdinalIgnoreCase);

                ConfiguracaoIntegracao configuration = new ConfiguracaoIntegracao(
                    integration.Codigo,
                    integration.Nome,
                    integration.Ambiente,
                    integration.BaseUrl,
                    integration.TimeoutMs,
                    settings);

                _cache.Set(cacheKey, configuration, TimeSpan.FromSeconds(ConstantsIntegration.CacheSegundos));

                return configuration;
            }
            catch (IntegrationException)
            {
                // Excecao de negocio ja tem a mensagem certa (qual integracao, qual problema).
                throw;
            }
            catch (Exception ex)
            {
                string friendlyMessage = string.Format(ConstantsMessageIntegration.ErroCarregarIntegracao, codigoNormalizado);
                _logService.Write(ex, friendlyMessage, GetType().ToString());
                throw new IntegrationException(codigoNormalizado, friendlyMessage, ex);
            }
        }

        public void Invalidate(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return;

                _cache.Remove(BuildCacheKey(codigo.Trim().ToUpperInvariant()));
            }
            catch (Exception ex)
            {
                _logService.Write(ex, string.Format(ConstantsMessageIntegration.ErroCarregarIntegracao, codigo), GetType().ToString());
            }
        }

        private static string BuildCacheKey(string codigoNormalizado)
        {
            return ConstantsIntegration.PrefixoCache + codigoNormalizado;
        }
    }
}
