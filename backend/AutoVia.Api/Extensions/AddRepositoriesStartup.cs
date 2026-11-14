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

            // --- Cliente ---
            services.AddScoped<IClienteRepository, ClienteRepository>();

            // --- Locadora (cadastro publico, P10-BE-01) ---
            services.AddScoped<IEmpresaRepository, EmpresaRepository>();

            // --- Equipe da locadora (P10-BE-03) ---
            services.AddScoped<IFuncionarioRepository, FuncionarioRepository>();

            // --- Veiculo (SEAM: base comum das tarefas P1-BE-02 e P1-BE-03) ---
            services.AddScoped<IVeiculoRepository, VeiculoRepository>();

            // --- Painel da locadora (visao geral, P11-BE-01 e P11-BE-02) ---
            services.AddScoped<IPainelRepository, PainelRepository>();

            // --- Autenticacao ---
            services.AddScoped<IAutenticacaoRepository, AutenticacaoRepository>();

            return services;
        }
    }
}
