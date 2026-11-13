using AutoVia.Api.Hosting;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            // --- Cliente ---
            services.AddScoped<IClienteService, ClienteService>();

            // --- Veiculo (SEAM: base comum das tarefas P1-BE-02 e P1-BE-03) ---
            services.AddScoped<IVeiculoService, VeiculoService>();

            // --- Autenticacao ---
            // Singleton: sem estado (hash, emissao de token); as opcoes sao registradas em AddAuthenticationJwt.
            services.AddSingleton<ISenhaHasher, SenhaHasher>();
            services.AddSingleton<ITokenService, TokenService>();
            services.AddScoped<IAutenticacaoService, AutenticacaoService>();
            services.AddScoped<IAdminInicialService, AdminInicialService>();

            // Garante o administrador inicial na subida (idempotente; falha so vai para o log).
            services.AddHostedService<AdminInicialHostedService>();

            // --- Seguros (P9-BE-01) ---
            services.AddScoped<ISeguroService, SeguroService>();

            return services;
        }
    }
}
