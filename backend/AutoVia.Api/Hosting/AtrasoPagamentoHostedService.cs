using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Api.Hosting
{
    /// <summary>
    /// Servico hospedado da rotina de atraso (contrato pagamento-atraso v1, P10, RF-34): roda uma vez ao subir a API, logo no
    /// inicio, e depois a cada hora. Cada rodada cria um escopo proprio e chama IPagamentoAtrasoRotinaService. Falha de uma
    /// rodada vai para o log e a proxima rodada segue; nunca derruba a API. Ligada por padrao; a configuracao
    /// Pagamentos:RotinaAtrasoAtiva com valor falso desliga a rotina (os testes da API a desligam).
    /// </summary>
    public class AtrasoPagamentoHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _escopos;
        private readonly ILogService _logService;
        private readonly bool _ativa;

        public AtrasoPagamentoHostedService(IServiceScopeFactory escopos, ILogService logService, IConfiguration configuracao)
        {
            _escopos = escopos;
            _logService = logService;
            _ativa = LerRotinaAtiva(configuracao);
        }

        /// <summary>Intervalo entre as rodadas: uma hora. Os testes trocam por um intervalo curto.</summary>
        protected virtual TimeSpan IntervaloRodada => TimeSpan.FromMinutes(ConstantsValues.IntervaloRotinaAtrasoMinutos);

        /// <summary>Roda ao subir e depois a cada intervalo, ate o desligamento da aplicacao. Desligada pela configuracao, nao faz nada.</summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_ativa)
            {
                return;
            }

            try
            {
                await ExecutarRodadaAsync(stoppingToken);

                using PeriodicTimer temporizador = new PeriodicTimer(IntervaloRodada);

                while (await temporizador.WaitForNextTickAsync(stoppingToken))
                {
                    await ExecutarRodadaAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Desligamento da aplicacao: a rotina sai sem erro.
            }
        }

        /// <summary>Uma rodada em escopo proprio. Falha vai so para o log; o cancelamento e relancado para sair limpo.</summary>
        private async Task ExecutarRodadaAsync(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope escopo = _escopos.CreateScope();
                IPagamentoAtrasoRotinaService rotina = escopo.ServiceProvider.GetRequiredService<IPagamentoAtrasoRotinaService>();

                await rotina.MarcarAtrasosTodasLocadorasAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroLogAtualizarAtrasosTodasLocadoras, GetType().ToString());
            }
        }

        /// <summary>Le a chave de configuracao; ausente ou invalida conta como ligada.</summary>
        private static bool LerRotinaAtiva(IConfiguration configuracao)
        {
            string? valor = configuracao[ConstantsValues.ChaveRotinaAtrasoPagamentoAtiva];

            if (bool.TryParse(valor, out bool ativa))
            {
                return ativa;
            }

            return true;
        }
    }
}
