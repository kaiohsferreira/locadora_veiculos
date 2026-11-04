using AutoVia.Core.Entities;

namespace AutoVia.Api.Extensions
{
    public static class AddAutoMapperStartup
    {
        /// <summary>
        /// Registra o AutoMapper lendo todos os Profiles de AutoVia.Core/Profiles.
        /// </summary>
        public static IServiceCollection AddMapping(this IServiceCollection services)
        {
            services.AddAutoMapper(configuration => { }, typeof(BaseEntities).Assembly);
            return services;
        }
    }
}
