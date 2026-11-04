using AutoVia.Helpers;

namespace AutoVia.Api.Extensions
{
    public static class AddCorsStartup
    {
        /// <summary>
        /// Libera o frontend. As origens vem de "Cors:Origens" (appsettings do
        /// ambiente ou variavel Cors__Origens__0); sem configuracao, libera apenas
        /// o Vite local (http://localhost:5173).
        /// </summary>
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            string[] origens = configuration.GetSection("Cors:Origens").Get<string[]>() ?? Array.Empty<string>();

            if (origens.Length == 0)
                origens = new string[] { ConstantsValues.OrigemFrontendLocal };

            services.AddCors(options =>
            {
                options.AddPolicy(ConstantsValues.PoliticaCors, policy =>
                {
                    policy.WithOrigins(origens)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders("Location");
                });
            });

            return services;
        }
    }
}
