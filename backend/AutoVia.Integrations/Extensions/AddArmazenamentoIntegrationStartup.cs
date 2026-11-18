using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Armazenamento;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoVia.Integrations.Extensions
{
    /// <summary>Registro do gateway de storage, dos provedores S3-compatible e DISCO_LOCAL e da leitura por link (contrato armazenamento-disco v1).</summary>
    public static class AddArmazenamentoIntegrationStartup
    {
        public static IServiceCollection AddArmazenamentoIntegration(this IServiceCollection services)
        {
            services.AddScoped<IArmazenamentoProvider, S3CompatStorageProvider>();
            services.AddScoped<IArmazenamentoProvider, DiscoArmazenamentoProvider>();
            services.AddScoped<IArmazenamentoGateway, ArmazenamentoGateway>();
            services.AddSingleton<IRaizArmazenamentoDisco, RaizArmazenamentoDisco>();
            services.AddSingleton<ISegredoAssinaturaArmazenamento, SegredoAssinaturaArmazenamento>();
            services.AddSingleton<IEnderecoPublicoApi, EnderecoPublicoApi>();
            services.AddScoped<IArmazenamentoLeituraService, ArmazenamentoLeituraService>();
            services.AddHttpContextAccessor();
            services.TryAddSingleton(TimeProvider.System);
            services.AddOptions<ArmazenamentoDiscoOptions>().BindConfiguration(ConstantsValues.SecaoArmazenamento);
            return services;
        }
    }
}
