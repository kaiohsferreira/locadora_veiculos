using AutoVia.Api.Hosting;
using AutoVia.Core.Options;
using AutoVia.Core.RepositoriesInterface;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Repositories;
using AutoVia.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace AutoVia.Api.Extensions
{
    public static class AddRecuperacaoSenhaStartup
    {
        /// <summary>
        /// Registro da recuperacao de senha por link (N1, RF-49): RecuperacaoSenhaOptions (secao Autenticacao:Recuperacao,
        /// validada na subida: URL publica absoluta, validade, limite e piso dentro das faixas, e piso nao negativo),
        /// IRedefinicaoSenhaRepository, IRecuperacaoSenhaService e IRecuperacaoSenhaProcessador (Scoped), a fila de
        /// pedidos IRecuperacaoSenhaFila (Singleton) e o RecuperacaoSenhaHostedService, que processa a fila em segundo
        /// plano. A API nao sobe com configuracao invalida. Fora de Development a URL publica precisa ser https e nao
        /// pode apontar para loopback (localhost, localhost., *.localhost, 127.0.0.0/8, ::1).
        /// </summary>
        public static IServiceCollection AddRecuperacaoSenha(this IServiceCollection services, IConfiguration configuration, IHostEnvironment ambiente)
        {
            bool ehDesenvolvimento = ambiente.IsDevelopment();

            services.AddOptions<RecuperacaoSenhaOptions>()
                .Bind(configuration.GetSection(ConstantsValues.SecaoRecuperacao))
                .Validate(opcoes => RecuperacaoValida(opcoes, ehDesenvolvimento), ConstantsMessage.ErroConfiguracaoRecuperacao)
                .ValidateOnStart();

            services.AddSingleton<RecuperacaoSenhaOptions>(provedor => provedor.GetRequiredService<IOptions<RecuperacaoSenhaOptions>>().Value);

            services.AddScoped<IRedefinicaoSenhaRepository, RedefinicaoSenhaRepository>();
            services.AddScoped<IRecuperacaoSenhaService, RecuperacaoSenhaService>();
            services.AddScoped<IRecuperacaoSenhaProcessador, RecuperacaoSenhaProcessador>();
            services.AddSingleton<IRecuperacaoSenhaFila, RecuperacaoSenhaFila>();
            services.AddHostedService<RecuperacaoSenhaHostedService>();

            return services;
        }

        /// <summary>URL publica, validade, limite e piso dentro das faixas aceitas. Fora de Development, a URL tem de ser https fora de loopback.</summary>
        private static bool RecuperacaoValida(RecuperacaoSenhaOptions opcoes, bool ehDesenvolvimento)
        {
            bool urlValida = UrlPublicaAceita(opcoes.UrlPublicaFront, ehDesenvolvimento);

            bool validadeValida = opcoes.ValidadeMinutos >= ConstantsValues.ValidadeMinutosMinimo
                && opcoes.ValidadeMinutos <= ConstantsValues.ValidadeMinutosMaximo;

            bool limiteValido = opcoes.LimitePorHora >= ConstantsValues.LimitePorHoraMinimo
                && opcoes.LimitePorHora <= ConstantsValues.LimitePorHoraMaximo;

            bool tempoValido = opcoes.TempoMinimoRespostaMs >= ConstantsValues.TempoMinimoRespostaMsMinimo
                && opcoes.TempoMinimoRespostaMs <= ConstantsValues.TempoMinimoRespostaMsMaximo;

            return urlValida && validadeValida && limiteValido && tempoValido;
        }

        /// <summary>Em Development aceita http ou https. Fora dele, so https com host que nao seja loopback.</summary>
        private static bool UrlPublicaAceita(string urlTexto, bool ehDesenvolvimento)
        {
            bool absoluta = Uri.TryCreate(urlTexto, UriKind.Absolute, out Uri? url);

            if (!absoluta || url == null)
            {
                return false;
            }

            if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (ehDesenvolvimento)
            {
                return true;
            }

            return url.Scheme == Uri.UriSchemeHttps && !EhLoopback(url.Host);
        }

        /// <summary>
        /// Verdadeiro para localhost, localhost. (com ponto final), *.localhost e enderecos de loopback (127.0.0.0/8 e ::1),
        /// com ou sem colchetes do IPv6. O sufixo exige o ponto antes de localhost: x.localhostx.com nao e loopback.
        /// </summary>
        private static bool EhLoopback(string host)
        {
            string nomeSemPontoFinal = host.TrimEnd(ConstantsValues.CaracterePontoFinal);

            if (string.Equals(nomeSemPontoFinal, ConstantsValues.HostLocalhost, StringComparison.OrdinalIgnoreCase)
                || nomeSemPontoFinal.EndsWith(ConstantsValues.SufixoSubdominioLocalhost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string semColchetes = host.Trim(ConstantsValues.CaractereColcheteAbre, ConstantsValues.CaractereColcheteFecha);
            bool enderecoValido = IPAddress.TryParse(semColchetes, out IPAddress? endereco);

            return enderecoValido && endereco != null && IPAddress.IsLoopback(endereco);
        }
    }
}
