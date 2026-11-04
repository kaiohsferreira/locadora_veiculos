using AutoVia.Api.Filters;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace AutoVia.Api.Extensions
{
    public static class AddCustomResponseStartup
    {
        /// <summary>
        /// Controllers, JSON e formato de erro.
        /// - enums trafegam como texto ("DISPONIVEL"), igual ao banco;
        /// - todo erro sai como ProblemDetails (RFC 7807) com mensagem em portugues;
        /// - o FluentValidationActionFilter roda antes de cada action.
        /// </summary>
        public static IServiceCollection AddCustomResponse(this IServiceCollection services)
        {
            services
                .AddControllers(options =>
                {
                    options.Filters.AddService<FluentValidationActionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    // Erro de model binding (JSON malformado, tipo errado): mesmo
                    // formato ProblemDetails, com mensagens em portugues.
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        Dictionary<string, string[]> errors = new Dictionary<string, string[]>();

                        foreach (KeyValuePair<string, ModelStateEntry> entry in context.ModelState)
                        {
                            if (entry.Value.Errors.Count == 0)
                                continue;

                            string field = string.IsNullOrWhiteSpace(entry.Key) ? "corpo" : entry.Key.TrimStart('$', '.');
                            errors[field] = new string[] { string.Format(ConstantsMessage.ErroCampoInvalido, field) };
                        }

                        ValidationProblemDetails problem = new ValidationProblemDetails(errors)
                        {
                            Title = ConstantsMessage.TituloErroValidacao,
                            Detail = ConstantsMessage.ErroRequisicaoInvalida,
                            Status = StatusCodes.Status400BadRequest,
                            Instance = context.HttpContext.Request.Path
                        };

                        BadRequestObjectResult result = new BadRequestObjectResult(problem);
                        result.ContentTypes.Add("application/problem+json");
                        return result;
                    };
                });

            services.AddProblemDetails();

            return services;
        }
    }
}
