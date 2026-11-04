using Microsoft.OpenApi;
using System.Reflection;

namespace AutoVia.Api.Extensions
{
    public static class AddSwaggerStartup
    {
        /// <summary>
        /// Swagger/OpenAPI com os /// &lt;summary&gt; dos controllers e dos VOs.
        /// O OpenAPI gerado aqui e o contrato que alimenta o frontend (passo 4 do guia).
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "AutoVia API",
                    Version = "v1",
                    Description = "API da plataforma AutoVia: locação e venda de veículos da frota. Erros no formato ProblemDetails (RFC 7807)."
                });

                // XML da Api (controllers) e do Communication (VOs).
                string[] xmlFiles = new string[]
                {
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
                    "AutoVia.Communication.xml"
                };

                foreach (string xmlFile in xmlFiles)
                {
                    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    if (File.Exists(xmlPath))
                        options.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }
    }
}
