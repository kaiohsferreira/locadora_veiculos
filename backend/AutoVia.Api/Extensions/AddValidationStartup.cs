using AutoVia.Api.Filters;
using AutoVia.Communication.Validators.Utils;
using AutoVia.Helpers;
using FluentValidation;
using System.Globalization;

namespace AutoVia.Api.Extensions
{
    public static class AddValidationStartup
    {
        /// <summary>
        /// Registra todos os validadores FluentValidation de AutoVia.Communication
        /// e o filtro que os executa antes de cada action. Mensagens padrao do
        /// FluentValidation em portugues.
        /// </summary>
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo(ConstantsValues.CulturaPadrao);

            services.AddValidatorsFromAssemblyContaining<FiltroPaginacaoVOValidator>();
            services.AddScoped<FluentValidationActionFilter>();

            return services;
        }
    }
}
