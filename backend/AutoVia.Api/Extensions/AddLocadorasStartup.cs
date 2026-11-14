using AutoVia.Core.Options;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoVia.Api.Extensions
{
    public static class AddLocadorasStartup
    {
        /// <summary>
        /// Registro das opcoes do cadastro publico de locadora (secao Locadoras): LocadorasOptions (Singleton,
        /// lido por IOptions) usado pelo CadastroLocadoraHabilitadoAttribute. Padrao desligado.
        /// Repositorio, servico e politica de limite sao registrados em AddRepositories, AddServices e AddRateLimitingPublico.
        /// </summary>
        public static IServiceCollection AddLocadoras(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<LocadorasOptions>()
                .Bind(configuration.GetSection(ConstantsValues.SecaoLocadoras));

            services.AddSingleton<LocadorasOptions>(provedor => provedor.GetRequiredService<IOptions<LocadorasOptions>>().Value);

            return services;
        }
    }
}
