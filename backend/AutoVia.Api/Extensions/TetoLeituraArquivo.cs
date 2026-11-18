using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.RateLimiting;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Teto da rota publica de leitura de arquivo: um contador unico, sem IP, com janela fixa de um minuto. A ACAO chama
    /// TentarEntrar como primeiro passo, DEPOIS do limite por IP (que roda antes, no middleware): requisicao recusada por IP
    /// nao gasta o teto. Numero em Armazenamento:LimiteLeituraGlobalPorMinuto (padrao 3000; zero ou negativo cai no padrao).
    /// </summary>
    public sealed class TetoLeituraArquivo : IDisposable
    {
        private readonly FixedWindowRateLimiter _limitador;

        /// <summary>Teto com o limite lido da configuracao da maquina (Armazenamento:LimiteLeituraGlobalPorMinuto).</summary>
        public TetoLeituraArquivo(IConfiguration configuration)
            : this(
                AddRateLimitingStartup.LerLimite(configuration, ConstantsValues.ChaveLimiteLeituraArquivoGlobal, ConstantsValues.LimiteLeituraArquivoGlobalPorMinutoPadrao),
                TimeSpan.FromMinutes(ConstantsValues.JanelaEmMinutosTetoLeituraArquivo))
        {
        }

        /// <summary>
        /// Teto com limite e janela informados. Publico porque o projeto de testes nao enxerga o interno desta API (sem
        /// InternalsVisibleTo); a janela curta deixa testar a virada sem esperar um minuto.
        /// </summary>
        public TetoLeituraArquivo(int limite, TimeSpan janela)
        {
            _limitador = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = janela,
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }

        /// <summary>Tenta uma entrada no teto. Falso quando a janela atual esgotou o limite.</summary>
        public bool TentarEntrar()
        {
            using RateLimitLease permissao = _limitador.AttemptAcquire(ConstantsValues.PermissoesPorEntradaTetoLeitura);
            return permissao.IsAcquired;
        }

        /// <summary>Libera o limitador (singleton, descartado pelo container ao encerrar).</summary>
        public void Dispose()
        {
            _limitador.Dispose();
        }
    }
}
