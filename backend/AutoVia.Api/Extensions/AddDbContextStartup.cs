using AutoVia.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVia.Api.Extensions
{
    public static class AddDbContextStartup
    {
        /// <summary>
        /// Registra o AutoViaDbContext com o provider Npgsql (PostgreSQL).
        ///
        /// Sem EnableRetryOnFailure de proposito: a estrategia de nova tentativa
        /// do Npgsql nao aceita transacao aberta pelo codigo (BeginTransaction), e
        /// o Prompt 3 pede transacao em toda operacao que altera mais de uma tabela.
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.ResolveDatabaseConnection();

            services.AddDbContext<AutoViaDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AutoViaDbContext).Assembly.GetName().Name);
                });
            });

            return services;
        }
    }
}
