using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Integrations.Services.Email
{
    /// <summary>
    /// Servico hospedado que consome a IEmailFila. Para cada mensagem cria um escopo e chama o IEmailGateway.
    /// Falha de envio so vai para o log (sem conteudo da mensagem) e nao derruba o servico.
    /// Encerra limpo quando a aplicacao desliga. Mensagens ainda na fila se perdem no restart.
    /// </summary>
    public class EmailEnvioHostedService : BackgroundService
    {
        private readonly IEmailFila _fila;
        private readonly IServiceScopeFactory _escopos;
        private readonly ILogService _logService;

        public EmailEnvioHostedService(IEmailFila fila, IServiceScopeFactory escopos, ILogService logService)
        {
            _fila = fila;
            _escopos = escopos;
            _logService = logService;
        }

        /// <summary>Le a fila ate o desligamento da aplicacao.</summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (EmailMensagem mensagem in _fila.Leitor.ReadAllAsync(stoppingToken))
                {
                    await EnviarAsync(mensagem, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Desligamento da aplicacao: encerra o leitor sem erro.
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailProcessarFila, GetType().ToString());
            }
        }

        /// <summary>Envia uma mensagem num escopo proprio. Falha vai so para o log.</summary>
        private async Task EnviarAsync(EmailMensagem mensagem, CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope escopo = _escopos.CreateScope();
                IEmailGateway gateway = escopo.ServiceProvider.GetRequiredService<IEmailGateway>();

                await gateway.EnviarAsync(mensagem, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroEmailEnviar, GetType().ToString());
            }
        }
    }
}
