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
    /// Limite de requisicoes das rotas publicas: janela fixa de um minuto por IP. Duas politicas
    /// independentes, cada uma com o seu contador: AutoViaPublico (login, cadastro, recuperacao e
    /// Google; numero em Autenticacao:LimitePublicoPorMinuto) e AutoViaVerificacaoCadastro
    /// (verificacao de cadastro; numero em Clientes:LimiteVerificacaoCadastroPorMinuto). Um valor
    /// menor ou igual a zero cai no padrao. Tambem configura os cabecalhos encaminhados pelo proxy
    /// (Railway), para que o IP usado no limite seja o do cliente.
    /// </summary>
    public static class AddRateLimitingStartup
    {
        /// <summary>Registra as politicas de limite das rotas publicas (429 em problem+json) e o tratamento de cabecalhos encaminhados.</summary>
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
                // Lidos quando o pipeline e montado (host ja com toda a configuracao aplicada).
                int limitePublico = LerLimitePorMinuto(configuration, ConstantsValues.ChaveLimitePublico, ConstantsValues.LimitePublicoPorMinutoPadrao);
                int limiteVerificacao = LerLimitePorMinuto(configuration, ConstantsValues.ChaveLimiteVerificacaoCadastro, ConstantsValues.LimiteVerificacaoCadastroPorMinutoPadrao);

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimitePublico, limitePublico);
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteVerificacaoCadastro, limiteVerificacao);

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

        /// <summary>Le o limite por minuto da chave informada; zero ou negativo cai no padrao informado.</summary>
        private static int LerLimitePorMinuto(IConfiguration configuration, string chave, int padrao)
        {
            int limite = configuration.GetValue<int>(chave, padrao);

            if (limite <= 0)
            {
                limite = padrao;
            }

            return limite;
        }

        /// <summary>Registra uma politica de janela fixa de um minuto, com contador separado por IP (desconhecido quando nao ha IP).</summary>
        private static void RegistrarPoliticaPorIp(RateLimiterOptions options, string nomePolitica, int limite)
        {
            options.AddPolicy<string>(nomePolitica, httpContext =>
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
        }
    }
}
