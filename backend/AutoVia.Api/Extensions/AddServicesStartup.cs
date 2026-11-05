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

            // --- Veiculo (SEAM: base comum das tarefas P1-BE-02 e P1-BE-03) ---
            services.AddScoped<IVeiculoService, VeiculoService>();

            return services;
        }
    }
}
