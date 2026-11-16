using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>Registro do gateway de storage e do provider S3-compatible.</summary>
    public static class AddArmazenamentoIntegrationStartup
    {
        public static IServiceCollection AddArmazenamentoIntegration(this IServiceCollection services)
        {
            services.AddScoped<IArmazenamentoProvider, S3CompatStorageProvider>();
            services.AddScoped<IArmazenamentoGateway, ArmazenamentoGateway>();
            return services;
        }
    }
}
