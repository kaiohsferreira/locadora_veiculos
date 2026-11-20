using AutoVia.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        ///
        /// A conexao e resolvida ao criar o contexto, e nao no registro: assim a
        /// configuracao em memoria das fabricas de teste (ConfigureAppConfiguration)
        /// ja esta valendo quando ela e lida.
        /// </summary>
        public static IServiceCollection AddDatabase(this IServiceCollection services)
        {
            services.AddDbContext<AutoViaDbContext>((IServiceProvider provider, DbContextOptionsBuilder options) =>
            {
                IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
                string connectionString = configuration.ResolveDatabaseConnection();

                options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AutoViaDbContext).Assembly.GetName().Name);
                });
            });

            return services;
        }
    }
}
