using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;

namespace AutoVia.Api.Extensions
{
    public static class AddServicesStartup
    {
        /// <summary>
        /// Registro dos servicos de aplicacao (Scoped, salvo indicacao).
        /// Cada modulo acrescenta aqui: services.AddScoped&lt;IVeiculoService, VeiculoService&gt;();
        /// </summary>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Singleton: sem estado, usado tambem pelo gateway HTTP (singleton).
            services.AddSingleton<ILogService, LogService>();

            // --- Modulos (Prompt 3) ---
            services.AddScoped<ICategoriaService, CategoriaService>();

            return services;
        }
    }
}
