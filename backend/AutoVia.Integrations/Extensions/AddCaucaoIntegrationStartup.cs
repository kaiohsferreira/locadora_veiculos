using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Caucao;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>
    /// Registro da caucao no cartao (P7-INT-01, RF-20). ICaucaoGateway e os provedores sao Scoped; o
    /// CaucaoFakeStore e singleton, porque guarda as caucoes FAKE entre requisicoes.
    /// Depende de IComunicacaoService e IIntegrationResolver, registrados por AddIntegrations.
    /// </summary>
    public static class AddCaucaoIntegrationStartup
    {
        public static IServiceCollection AddCaucaoIntegration(this IServiceCollection services)
        {
            services.AddSingleton<CaucaoFakeStore>();
            services.AddScoped<ICaucaoProvider, MercadoPagoCaucaoProvider>();
            services.AddScoped<ICaucaoProvider, FakeCaucaoProvider>();
            services.AddScoped<ICaucaoGateway, CaucaoGateway>();

            return services;
        }
    }
}
