using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Multas;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>Registro do gateway e do provedor FAKE de multas.</summary>
    public static class AddMultasIntegrationStartup
    {
        public static IServiceCollection AddMultasIntegration(this IServiceCollection services)
        {
            services.AddScoped<IMultasProvider, FakeMultasProvider>();
            services.AddScoped<IMultasGateway, MultasGateway>();
            return services;
        }
    }
}