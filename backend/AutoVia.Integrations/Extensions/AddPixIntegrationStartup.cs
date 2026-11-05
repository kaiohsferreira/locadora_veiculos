using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Pix;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>
    /// Registro da cobranca PIX (P7-INT-01). IPixGateway e os provedores sao Scoped; o
    /// PixFakeStore e singleton, porque guarda as cobrancas FAKE entre requisicoes.
    /// Depende de IComunicacaoService e IIntegrationResolver, registrados por AddIntegrations.
    /// </summary>
    public static class AddPixIntegrationStartup
    {
        public static IServiceCollection AddPixIntegration(this IServiceCollection services)
        {
            services.AddSingleton<PixFakeStore>();
            services.AddScoped<IPixProvider, MercadoPagoPixProvider>();
            services.AddScoped<IPixProvider, FakePixProvider>();
            services.AddScoped<IPixGateway, PixGateway>();

            return services;
        }
    }
}
