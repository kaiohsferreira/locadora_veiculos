using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Documento;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>
    /// Registro da leitura de documentos (CNH). Registra o gateway e o provedor FAKE.
    /// Provedores reais (fase 2) entram aqui, como mais um IDocumentoOcrProvider.
    /// </summary>
    public static class AddDocumentoIntegrationStartup
    {
        public static IServiceCollection AddDocumentoIntegration(this IServiceCollection services)
        {
            services.AddScoped<IDocumentoOcrGateway, DocumentoOcrGateway>();
            services.AddScoped<IDocumentoOcrProvider, FakeDocumentoOcrProvider>();

            return services;
        }
    }
}
