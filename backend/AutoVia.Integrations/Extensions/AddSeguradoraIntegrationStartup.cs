using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Seguradora;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>
    /// Registro da integracao SEGURADORA (P9-INT-01). Gateway e provedores sao Scoped; o
    /// SeguradoraFakeStore e singleton, porque guarda os sinistros FAKE entre requisicoes.
    /// Provedores reais entram aqui, como mais um ISeguradoraProvider.
    /// </summary>
    public static class AddSeguradoraIntegrationStartup
    {
        public static IServiceCollection AddSeguradoraIntegration(this IServiceCollection services)
        {
            services.AddSingleton<SeguradoraFakeStore>();
            services.AddScoped<ISeguradoraProvider, FakeSeguradoraProvider>();
            services.AddScoped<ISeguradoraGateway, SeguradoraGateway>();

            return services;
        }
    }
}
