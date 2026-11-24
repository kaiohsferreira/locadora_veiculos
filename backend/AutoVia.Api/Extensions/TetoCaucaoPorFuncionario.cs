using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.Threading.RateLimiting;

namespace AutoVia.Api.Extensions
{
    /// <summary>
    /// Teto de aberturas de locacao com caucao por funcionario (decisao 5 de 24/11/2026; molde TetoGeracaoPixPorCliente): janela
    /// fixa de um minuto, uma particao por funcionario (claim NameIdentifier, com o prefixo funcionario:). A ACAO chama
    /// TentarEntrar so quando a caucao e exigida; com valor 0 o teto nao e consultado (RF-17 sem 429 novo). Numero em
    /// Pagamentos:LimiteCaucaoPorFuncionarioMinuto (padrao 5), lido com int.TryParse: ausente, nao inteiro, zero ou negativo cai
    /// no padrao, sem lancar, porque o teto entra no construtor da LocacaoController e um erro derrubaria todas as rotas de locacao.
    /// </summary>
    public sealed class TetoCaucaoPorFuncionario : IDisposable
    {
        private readonly PartitionedRateLimiter<string> _limitador;

        /// <summary>Teto com o limite lido da configuracao (Pagamentos:LimiteCaucaoPorFuncionarioMinuto) e janela de um minuto.</summary>
        public TetoCaucaoPorFuncionario(IConfiguration configuration)
            : this(LerLimite(configuration), TimeSpan.FromMinutes(ConstantsValues.JanelaEmMinutosLimiteCaucaoPorFuncionario))
        {
        }

        /// <summary>
        /// Teto com limite e janela informados; limite zero ou negativo cai no padrao. Publico porque o projeto de testes nao
        /// enxerga o interno desta API (sem InternalsVisibleTo).
        /// </summary>
        public TetoCaucaoPorFuncionario(int limite, TimeSpan janela)
        {
            int limiteEfetivo = limite > 0 ? limite : ConstantsValues.LimiteCaucaoPorFuncionarioMinutoPadrao;

            _limitador = PartitionedRateLimiter.Create<string, string>(chave => RateLimitPartition.GetFixedWindowLimiter<string>(chave, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limiteEfetivo,
                Window = janela,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
        }

        /// <summary>
        /// Le o limite da configuracao sem lancar: texto inteiro positivo vale; ausente, vazio, nao inteiro ('5.0', 'cinco'), zero
        /// ou negativo devolve o padrao (5).
        /// </summary>
        public static int LerLimite(IConfiguration configuration)
        {
            string? texto = configuration[ConstantsValues.ChaveLimiteCaucaoPorFuncionario];
            bool lido = int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int limite);

            if (!lido || limite <= 0)
            {
                return ConstantsValues.LimiteCaucaoPorFuncionarioMinutoPadrao;
            }

            return limite;
        }

        /// <summary>Tenta uma abertura com caucao para a chave (funcionario com prefixo). Falso quando a janela atual dessa identidade esgotou o limite.</summary>
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
