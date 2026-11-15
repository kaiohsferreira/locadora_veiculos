using AutoVia.Core.Options;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoVia.Api.Extensions
{
    public static class AddClientesStartup
    {
        /// <summary>
        /// Registro das opcoes da secao Clientes usadas pelo servico de vinculo cliente x locadora (teto de pedidos de
        /// aprovacao): ClientesOptions (Singleton, lido por IOptions). Padrao 5 por janela de 24 horas.
        /// O limite por IP da verificacao de cadastro continua na politica de limite (AddRateLimitingPublico).
        /// </summary>
        public static IServiceCollection AddClientes(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ClientesOptions>()
                .Bind(configuration.GetSection(ConstantsValues.SecaoClientes));

            services.AddSingleton<ClientesOptions>(provedor => provedor.GetRequiredService<IOptions<ClientesOptions>>().Value);

            return services;
        }
    }
}
