using AutoVia.Infrastructure.Repositories;
using AutoVia.Integrations.Interfaces;
using AutoVia.Core.RepositoriesInterface;

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
            services.AddScoped<ICategoriaRepository, CategoriaRepository>();

            // --- Veiculo (SEAM: base comum das tarefas P1-BE-02 e P1-BE-03) ---
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();

            return services;
        }
    }
}
