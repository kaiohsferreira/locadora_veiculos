using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.RateLimiting;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Teto de geracoes de PIX por cliente (ou funcionario, na rota da locadora): janela fixa de um minuto, uma particao por
    /// identidade do token (claim NameIdentifier, com prefixo que separa cliente de funcionario). A ACAO chama TentarEntrar
    /// depois da autenticacao, porque o limite por IP roda antes, no middleware, e os dois valem juntos: a requisicao
    /// recusada pelo IP nao chega aqui. Numero em Pagamentos:LimiteGeracaoPixPorClienteMinuto (padrao 5; zero ou negativo cai no padrao).
    /// </summary>
    public sealed class TetoGeracaoPixPorCliente : IDisposable
    {
        private readonly PartitionedRateLimiter<string> _limitador;

        /// <summary>Teto com o limite lido da configuracao (Pagamentos:LimiteGeracaoPixPorClienteMinuto).</summary>
        public TetoGeracaoPixPorCliente(IConfiguration configuration)
            : this(
                AddRateLimitingStartup.LerLimite(configuration, ConstantsValues.ChaveLimiteGeracaoPixPorCliente, ConstantsValues.LimiteGeracaoPixPorClienteMinutoPadrao),
                TimeSpan.FromMinutes(ConstantsValues.JanelaEmMinutosLimiteGeracaoPixPorCliente))
        {
        }

        /// <summary>
        /// Teto com limite e janela informados. Publico porque o projeto de testes nao enxerga o interno desta API (sem
        /// InternalsVisibleTo).
        /// </summary>
        public TetoGeracaoPixPorCliente(int limite, TimeSpan janela)
        {
            _limitador = PartitionedRateLimiter.Create<string, string>(chave => RateLimitPartition.GetFixedWindowLimiter<string>(chave, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = janela,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
        }

        /// <summary>Tenta uma geracao para a chave (identidade com prefixo). Falso quando a janela atual dessa identidade esgotou o limite.</summary>
        public bool TentarEntrar(string chave)
        {
            using RateLimitLease permissao = _limitador.AttemptAcquire(chave);
            return permissao.IsAcquired;
        }

        /// <summary>Libera o limitador (singleton, descartado pelo container ao encerrar).</summary>
        public void Dispose()
        {
            _limitador.Dispose();
        }
    }
}
