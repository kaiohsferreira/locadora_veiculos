using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoAdministrador lendo o IContextoEmpresa (empresa e cargo vindos do banco na
    /// validacao do token, nunca da claim). Sem contexto de empresa, nega.
    /// </summary>
    public class AutorizacaoAdministradorHandler : AuthorizationHandler<RequisitoAdministrador>
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoAdministradorHandler(IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito quando o contexto tem empresa e o cargo e ADMINISTRADOR.</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoAdministrador requirement)
        {
            try
            {
                bool administrador = _contextoEmpresa.IdEmpresa > 0 && _contextoEmpresa.Cargo == CargoFuncionario.ADMINISTRADOR;

                if (administrador)
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
