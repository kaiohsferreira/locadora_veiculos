using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Caucao na retirada (P4-BE-04, RF-20; contrato caucao-retirada v1.1): valor da caucao lido da configuracao do servidor
    /// (Pagamentos:ValorCaucao) pela regra pura RegrasCaucaoRetirada.TentarLerValorCaucao. Valor invalido impede a subida da API
    /// (molde Rede:CabecalhoIpCliente): o Program chama ValidarValorCaucao logo depois do Build e resolve o CaucaoRetiradaOptions
    /// em seguida, para o valor em uso ser o mesmo que foi validado. A mensagem nunca repete o valor informado.
    /// </summary>
    public static class AddCaucaoRetiradaStartup
    {
        /// <summary>
        /// Registra CaucaoRetiradaOptions (Singleton). A fabrica le a configuracao final com a mesma regra da subida e, com valor
        /// invalido, lanca InvalidOperationException com ErroConfiguracaoValorCaucao: nunca cai em 0, porque 0 desligaria a caucao
        /// sem aviso. Registra tambem o TetoCaucaoPorFuncionario (Singleton; o limite e lido sem lancar).
        /// </summary>
        public static IServiceCollection AddCaucaoRetirada(this IServiceCollection services)
        {
            services.AddSingleton<CaucaoRetiradaOptions>(provedor =>
            {
                IConfiguration configuracao = provedor.GetRequiredService<IConfiguration>();
                decimal valorCaucao = ValidarValorCaucao(configuracao);

                return new CaucaoRetiradaOptions(valorCaucao);
            });

            services.AddSingleton<TetoCaucaoPorFuncionario>(provedor =>
            {
                IConfiguration configuracao = provedor.GetRequiredService<IConfiguration>();

                return new TetoCaucaoPorFuncionario(configuracao);
            });

            return services;
        }

        /// <summary>
        /// Le Pagamentos:ValorCaucao da configuracao e devolve o valor (0 = sem caucao; ausente ou vazio tambem vale 0). Fora do
        /// formato (so ponto decimal, no maximo 2 casas, de 0 a 99999999.99) lanca InvalidOperationException com a mensagem de
        /// configuracao, sem repetir o valor informado. O Program chama no proprio fluxo de subida, logo apos o Build e antes de
        /// atender requisicoes: a API nao sobe. Nao e feito dentro de lambda de options.
        /// </summary>
        public static decimal ValidarValorCaucao(IConfiguration configuration)
        {
            string? texto = configuration[ConstantsValues.ChaveValorCaucao];

            if (!RegrasCaucaoRetirada.TentarLerValorCaucao(texto, out decimal valorCaucao))
            {
                throw new InvalidOperationException(ConstantsMessage.ErroConfiguracaoValorCaucao);
            }

            return valorCaucao;
        }
    }
}
