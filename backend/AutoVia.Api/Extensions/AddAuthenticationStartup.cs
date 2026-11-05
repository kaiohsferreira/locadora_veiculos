using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Autenticacao JWT (Bearer, HS256), politicas Funcionario e Cliente, FallbackPolicy
    /// (usuario autenticado) e resposta 401/403 em ProblemDetails. A configuracao JWT e
    /// lida como opcao e validada na subida do host (ValidateOnStart): sem chave com 32
    /// caracteres ou sem emissor/audiencia, a API nao sobe com ErroConfiguracaoJwt.
    /// </summary>
    public static class AddAuthenticationStartup
    {
        /// <summary>Registra o JWT Bearer, as politicas de autorizacao e o handler de 401/403 em problem+json.</summary>
        public static IServiceCollection AddAuthenticationJwt(this IServiceCollection services, IConfiguration configuration)
        {
            // Opcoes lidas por IOptions: a leitura acontece quando o host sobe, com toda a configuracao ja aplicada.
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(ConstantsValues.SecaoJwt))
                .Validate(ConfiguracaoJwtValida, string.Format(ConstantsMessage.ErroConfiguracaoJwt, ConstantsValues.TamanhoMinimoJwtChave))
                .ValidateOnStart();

            services.AddOptions<AdminInicialOptions>()
                .Bind(configuration.GetSection(ConstantsValues.SecaoAdminInicial));

            services.AddSingleton<JwtOptions>(provedor => provedor.GetRequiredService<IOptions<JwtOptions>>().Value);
            services.AddSingleton<AdminInicialOptions>(provedor => provedor.GetRequiredService<IOptions<AdminInicialOptions>>().Value);

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((options, opcoes) => ConfigurarJwtBearer(options, opcoes.Value));

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ConstantsValues.PoliticaFuncionario, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(nameof(PerfilUsuario.FUNCIONARIO));
                });

                options.AddPolicy(ConstantsValues.PoliticaCliente, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(nameof(PerfilUsuario.CLIENTE));
                });

                // Rota sem nenhuma declaracao exige usuario autenticado (o teste de guarda AutorizacaoRotasTests reprova rota sem declaracao).
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();

            return services;
        }

        /// <summary>Regras de validacao da configuracao JWT: chave com no minimo 32 caracteres, emissor, audiencia e validade positiva.</summary>
        private static bool ConfiguracaoJwtValida(JwtOptions jwt)
        {
            bool chaveValida = !string.IsNullOrWhiteSpace(jwt.Chave) && jwt.Chave.Length >= ConstantsValues.TamanhoMinimoJwtChave;
            bool emissorValido = !string.IsNullOrWhiteSpace(jwt.Emissor);
            bool audienciaValida = !string.IsNullOrWhiteSpace(jwt.Audiencia);
            bool expiracaoValida = jwt.ExpiracaoMinutos > 0;

            return chaveValida && emissorValido && audienciaValida && expiracaoValida;
        }

        /// <summary>Parametros de validacao do token: assinatura HS256, emissor, audiencia, validade e tolerancia de relogio de 30 s.</summary>
        private static void ConfigurarJwtBearer(JwtBearerOptions options, JwtOptions jwt)
        {
            // Mantem as claims do token com os nomes de ClaimTypes (sub vira NameIdentifier, role vira Role).
            options.MapInboundClaims = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Emissor,
                ValidateAudience = true,
                ValidAudience = jwt.Audiencia,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
                ValidAlgorithms = new string[] { SecurityAlgorithms.HmacSha256 },
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromSeconds(ConstantsValues.ToleranciaRelogioSegundos)
            };
        }
    }
}
