using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using System;

namespace AutoVia.Infrastructure.Services
{
    /// <summary>
    /// Relogio de producao: DateTime.Now do servidor (regra R5, colunas timestamp sem fuso). Sem estado, por isso Singleton.
    /// </summary>
    public class RelogioSistema : IRelogio
    {
        private readonly ILogService _logService;

        public RelogioSistema(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>Data e hora local do servidor. Falha, se ocorrer, so e registrada e relancada.</summary>
        public DateTime Agora
        {
            get
            {
                try
                {
                    DateTime agora = DateTime.Now;

                    return agora;
                }
                catch (Exception ex)
                {
                    _logService.Write(ex, ConstantsMessage.ErroConsultarVisaoGeral, GetType().ToString());
                    throw;
                }
            }
        }
    }
}
