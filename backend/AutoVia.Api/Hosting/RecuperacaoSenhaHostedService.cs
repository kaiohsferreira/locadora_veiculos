using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Api.Hosting
{
    /// <summary>
    /// Servico hospedado da recuperacao de senha (N1, RF-49): le a fila de pedidos e, para cada pedido, cria um escopo
    /// de DI e chama o IRecuperacaoSenhaProcessador. Falha de um pedido vai para o log (sem dado pessoal) e nao derruba
    /// o leitor. Encerra limpo no desligamento. Pedidos ainda na fila se perdem no restart.
    /// </summary>
    public class RecuperacaoSenhaHostedService : BackgroundService
    {
        private readonly IRecuperacaoSenhaFila _fila;
        private readonly IServiceScopeFactory _escopos;
        private readonly ILogService _logService;

        public RecuperacaoSenhaHostedService(IRecuperacaoSenhaFila fila, IServiceScopeFactory escopos, ILogService logService)
        {
            _fila = fila;
            _escopos = escopos;
            _logService = logService;
        }

        /// <summary>Le a fila de pedidos ate o desligamento da aplicacao; cada pedido e processado num escopo proprio.</summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (PedidoRecuperacaoSenha pedido in _fila.Leitor.ReadAllAsync(stoppingToken))
                {
                    await ProcessarAsync(pedido, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Desligamento da aplicacao: encerra o leitor sem erro.
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroProcessarFilaRecuperacaoSenha, GetType().ToString());
            }
        }

        /// <summary>Processa um pedido num escopo proprio. Falha vai so para o log e o leitor segue.</summary>
        private async Task ProcessarAsync(PedidoRecuperacaoSenha pedido, CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope escopo = _escopos.CreateScope();
                IRecuperacaoSenhaProcessador processador = escopo.ServiceProvider.GetRequiredService<IRecuperacaoSenhaProcessador>();

                await processador.ProcessarAsync(pedido, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroProcessarPedidoRecuperacaoSenha, GetType().ToString());
            }
        }
    }
}
