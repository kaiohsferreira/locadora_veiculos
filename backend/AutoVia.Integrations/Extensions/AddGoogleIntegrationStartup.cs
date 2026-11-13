using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Google;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    public static class AddGoogleIntegrationStartup
    {
        /// <summary>
        /// Registro da validacao de identidade do Google (BE-5, N3). IGoogleIdentityGateway e os provedores
        /// GOOGLE e FAKE sao Scoped. As chaves publicas ficam no IMemoryCache, registrado por AddIntegrations,
        /// e as chamadas HTTP passam pelo IComunicacaoService (singleton, tambem de AddIntegrations).
        /// Depende de ILogService (AddServicesStartup) e IIntegrationResolver (AddIntegrations).
        /// </summary>
        public static IServiceCollection AddGoogleIntegration(this IServiceCollection services)
        {
            services.AddScoped<IGoogleIdentityProvider, GoogleIdentityProvider>();
            services.AddScoped<IGoogleIdentityProvider, FakeGoogleIdentityProvider>();
            services.AddScoped<IGoogleIdentityGateway, GoogleIdentityGateway>();

            return services;
        }
    }
}
