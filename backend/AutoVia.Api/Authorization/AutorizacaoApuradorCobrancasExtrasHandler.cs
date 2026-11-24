using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoApuradorCobrancasExtras lendo o IContextoEmpresa (empresa e cargo vindos do banco na validacao do token,
    /// nunca da claim). Concede para ATENDENTE ou ADMINISTRADOR de uma locadora do contexto; sem contexto, nega.
    /// </summary>
    public class AutorizacaoApuradorCobrancasExtrasHandler : AuthorizationHandler<RequisitoApuradorCobrancasExtras>
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoApuradorCobrancasExtrasHandler(IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito quando o contexto tem locadora e o cargo e ATENDENTE ou ADMINISTRADOR.</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoApuradorCobrancasExtras requirement)
        {
            try
            {
                CargoFuncionario? cargo = _contextoEmpresa.Cargo;
                bool apurador = _contextoEmpresa.IdEmpresa > 0
                    && (cargo == CargoFuncionario.ATENDENTE || cargo == CargoFuncionario.ADMINISTRADOR);

                if (apurador)
                {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logService.Write(ex, ConstantsMessage.ErroAutorizarPolitica, GetType().ToString());
                throw;
            }
        }
    }
}
