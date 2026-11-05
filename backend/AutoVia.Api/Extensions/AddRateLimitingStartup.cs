using AutoVia.Api.Middleware;
using AutoVia.Helpers;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Limite de requisicoes das rotas publicas (login e cadastro de cliente): janela fixa
    /// de um minuto por IP, com o numero vindo de Autenticacao:LimitePublicoPorMinuto.
    /// Tambem configura os cabecalhos encaminhados pelo proxy (Railway), para que o IP
    /// usado no limite seja o do cliente.
    /// </summary>
    public static class AddRateLimitingStartup
    {
        /// <summary>Registra a politica AutoViaPublico (429 em problem+json) e o tratamento de cabecalhos encaminhados.</summary>
        public static IServiceCollection AddRateLimitingPublico(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // O proxy do Railway nao tem IP fixo, por isso a lista de proxies confiaveis fica vazia.
                // RISCO: sem lista de proxies, um cliente que chega direto na API pode enviar
                // X-Forwarded-For forjado e trocar o IP usado no limite. Mitigacao: a API deve ficar
                // acessivel so pelo proxy (rede privada ou firewall), e o proxy precisa acrescentar o IP real.
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddRateLimiter(options =>
            {
                // Lido quando o pipeline e montado (host ja com toda a configuracao aplicada).
                int limite = configuration.GetValue<int>(ConstantsValues.ChaveLimitePublico, ConstantsValues.LimitePublicoPorMinutoPadrao);

                if (limite <= 0)
                {
                    limite = ConstantsValues.LimitePublicoPorMinutoPadrao;
                }

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy<string>(ConstantsValues.PoliticaLimitePublico, httpContext =>
                {
                    string chaveIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? ConstantsValues.ChaveIpDesconhecido;

                    return RateLimitPartition.GetFixedWindowLimiter<string>(chaveIp, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limite,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    await ProblemDetailsWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status429TooManyRequests,
                        ConstantsMessage.TituloLimiteRequisicoes,
                        ConstantsMessage.ErroLimiteRequisicoes);
                };
            });

            return services;
        }
    }
}
