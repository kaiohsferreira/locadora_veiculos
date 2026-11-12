using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Email;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Integrations.Extensions
{
    public static class AddEmailIntegrationStartup
    {
        /// <summary>
        /// Registro da porta de e-mail transacional (BE-3, D2-B). EmailFakeStore e IEmailFila sao singleton
        /// (a fila e o registro guardam estado entre requisicoes); IEmailGateway e IEmailProvider sao Scoped.
        /// O FAKE e o unico provedor nesta tarefa: o provedor real entra na T-INT-01 (Lucas) com um
        /// AddScoped<IEmailProvider, ...> aqui. EmailEnvioHostedService consome a fila.
        /// Depende de ILogService e IIntegrationResolver, registrados por AddServicesStartup e AddIntegrations.
        /// </summary>
        public static IServiceCollection AddEmailIntegration(this IServiceCollection services)
        {
            services.AddSingleton<EmailFakeStore>();
            services.AddSingleton<IEmailFila, EmailFila>();
            services.AddScoped<IEmailProvider, FakeEmailProvider>();
            services.AddScoped<IEmailGateway, EmailGateway>();
            services.AddHostedService<EmailEnvioHostedService>();

            return services;
        }
    }
}
