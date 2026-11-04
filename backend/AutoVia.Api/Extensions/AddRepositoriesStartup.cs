using AutoVia.Infrastructure.Repositories;
using AutoVia.Integrations.Interfaces;

namespace AutoVia.Api.Extensions
{
    public static class AddRepositoriesStartup
    {
        /// <summary>
        /// Registro dos repositorios (Scoped: um por requisicao, junto com o DbContext).
        /// Cada modulo acrescenta aqui: services.AddScoped&lt;IVeiculoRepository, VeiculoRepository&gt;();
        /// </summary>
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // --- Biblioteca de integracoes ---
            services.AddScoped<IIntegrationRepository, IntegrationRepository>();

            // --- Modulos (Prompt 3) ---

            return services;
        }
    }
}
