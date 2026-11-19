using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoGestorFrota lendo o IContextoEmpresa (empresa e cargo vindos do banco na validacao do token,
    /// nunca da claim). Concede para GESTOR_FROTA ou ADMINISTRADOR de uma locadora do contexto; sem contexto, nega.
    /// </summary>
    public class AutorizacaoGestorFrotaHandler : AuthorizationHandler<RequisitoGestorFrota>
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoGestorFrotaHandler(IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito quando o contexto tem locadora e o cargo e GESTOR_FROTA ou ADMINISTRADOR.</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoGestorFrota requirement)
        {
            try
            {
                CargoFuncionario? cargo = _contextoEmpresa.Cargo;
                bool gestor = _contextoEmpresa.IdEmpresa > 0
                    && (cargo == CargoFuncionario.GESTOR_FROTA || cargo == CargoFuncionario.ADMINISTRADOR);

                if (gestor)
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
