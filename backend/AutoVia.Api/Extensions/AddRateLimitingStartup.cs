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
    /// Limite de requisicoes das rotas publicas: janela fixa por IP. Tres politicas independentes, cada uma
    /// com o seu contador: AutoViaPublico (login, cadastro, recuperacao e Google; um minuto; numero em
    /// Autenticacao:LimitePublicoPorMinuto), AutoViaVerificacaoCadastro (verificacao de cadastro; um minuto;
    /// numero em Clientes:LimiteVerificacaoCadastroPorMinuto) e AutoViaCadastroEmpresa (cadastro publico de
    /// locadora; uma hora; numero em Locadoras:LimiteCadastroPorHora, padrao 5). Um valor menor ou igual a
    /// zero cai no padrao. Tambem configura os cabecalhos encaminhados pelo proxy (Railway), para que o IP
    /// usado no limite seja o do cliente.
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
                int limitePublico = LerLimite(configuration, ConstantsValues.ChaveLimitePublico, ConstantsValues.LimitePublicoPorMinutoPadrao);
                int limiteVerificacao = LerLimite(configuration, ConstantsValues.ChaveLimiteVerificacaoCadastro, ConstantsValues.LimiteVerificacaoCadastroPorMinutoPadrao);
                int limiteCadastroEmpresa = LerLimite(configuration, ConstantsValues.ChaveLimiteCadastroPorHora, ConstantsValues.LimiteCadastroEmpresaPorHoraPadrao);

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimitePublico, limitePublico, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteVerificacaoCadastro, limiteVerificacao, TimeSpan.FromMinutes(1));
                RegistrarPoliticaPorIp(options, ConstantsValues.PoliticaLimiteCadastroEmpresa, limiteCadastroEmpresa, TimeSpan.FromHours(1));

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

        /// <summary>Le o limite da chave informada; zero ou negativo cai no padrao informado.</summary>
        private static int LerLimite(IConfiguration configuration, string chave, int padrao)
        {
            int limite = configuration.GetValue<int>(chave, padrao);

            if (limite <= 0)
            {
                limite = padrao;
            }

            return limite;
        }

        /// <summary>Registra uma politica de janela fixa (a janela informada), com contador separado por IP (desconhecido quando nao ha IP).</summary>
        private static void RegistrarPoliticaPorIp(RateLimiterOptions options, string nomePolitica, int limite, TimeSpan janela)
        {
            options.AddPolicy<string>(nomePolitica, httpContext =>
            {
                string chaveIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? ConstantsValues.ChaveIpDesconhecido;

                return RateLimitPartition.GetFixedWindowLimiter<string>(chaveIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    Window = janela,
                    QueueLimit = 0,
                    AutoReplenishment = true
                });
            });
        }
    }
}
