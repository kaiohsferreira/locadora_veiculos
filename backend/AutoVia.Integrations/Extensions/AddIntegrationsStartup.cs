using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>
    /// Registro da biblioteca de integracoes. Unico arquivo de registro dos
    /// provedores: cada Prompt 2 acrescenta aqui o seu Request e o seu Service.
    ///
    /// Depende de ILogService e IIntegrationRepository, registrados pela Api
    /// (AddServicesStartup e AddRepositoriesStartup).
    /// </summary>
    public static class AddIntegrationsStartup
    {
        public static IServiceCollection AddIntegrations(this IServiceCollection services)
        {
            services.AddMemoryCache();

            // Gateway HTTP unico (um RestClient reaproveitado).
            services.AddSingleton<IComunicacaoService, ComunicacaoService>();

            // Resolver da configuracao dinamica (le o banco: escopo da requisicao).
            services.AddScoped<IIntegrationResolver, IntegrationResolver>();

            services.AddDocumentoIntegration();

            // --- Provedores (Prompt 2) ---
            // services.AddScoped<IPagamentoRequest, PagamentoRequest>();
            // services.AddScoped<IPagamentoService, PagamentoService>();
            services.AddPixIntegration();
            services.AddCaucaoIntegration();
            services.AddEmailIntegration();
            services.AddGoogleIntegration();

            return services;
        }
    }
}
