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
    /// Garante o administrador inicial na subida da API. Roda uma vez por inicializacao, em
    /// escopo proprio (o servico e Scoped). Falha e so registrada no log: nunca derruba a API.
    /// </summary>
    public class AdminInicialHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogService _logService;

        public AdminInicialHostedService(IServiceScopeFactory scopeFactory, ILogService logService)
        {
            _scopeFactory = scopeFactory;
            _logService = logService;
        }

        /// <summary>Executa a garantia do administrador inicial; qualquer excecao vai para o log e a subida segue.</summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope escopo = _scopeFactory.CreateScope();
                IAdminInicialService adminInicialService = escopo.ServiceProvider.GetRequiredService<IAdminInicialService>();

                await adminInicialService.GarantirAdminInicialAsync();
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroCriarAdminInicial, GetType().ToString());
            }
        }

        /// <summary>Nada a finalizar: a garantia roda uma unica vez na subida.</summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
