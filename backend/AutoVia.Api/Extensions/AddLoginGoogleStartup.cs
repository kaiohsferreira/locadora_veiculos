using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Api.Extensions
{
    public static class AddLoginGoogleStartup
    {
        /// <summary>
        /// Registro do login de cliente com conta Google (N3, RF-51): IGoogleVinculoRepository e IAutenticacaoGoogleService
        /// (Scoped). O IGoogleIdentityGateway e a configuracao do Google vem de AddGoogleIntegration (integracao GOOGLE no banco).
        /// Depende de IAutenticacaoRepository, IAutenticacaoService e ISenhaHasher (AddRepositories e AddServices).
        /// </summary>
        public static IServiceCollection AddLoginGoogle(this IServiceCollection services)
        {
            services.AddScoped<IGoogleVinculoRepository, GoogleVinculoRepository>();
            services.AddScoped<IAutenticacaoGoogleService, AutenticacaoGoogleService>();

            return services;
        }
    }
}
