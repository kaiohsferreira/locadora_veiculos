using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoOperadora lendo o IContextoEmpresa: concede quando a empresa do funcionario e a
    /// operadora da plataforma E o cargo e ADMINISTRADOR (revisao de seguranca, ALTO-3: a migration poe todos
    /// os funcionarios atuais na operadora, entao so a empresa nao basta). Sem contexto de empresa, nega.
    /// </summary>
    public class AutorizacaoOperadoraHandler : AuthorizationHandler<RequisitoOperadora>
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoOperadoraHandler(IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito quando o contexto tem empresa, ela e a operadora da plataforma e o cargo e ADMINISTRADOR.</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoOperadora requirement)
        {
            try
            {
                bool operadora = _contextoEmpresa.IdEmpresa > 0
                    && _contextoEmpresa.EmpresaOperadora
                    && _contextoEmpresa.Cargo == CargoFuncionario.ADMINISTRADOR;

                if (operadora)
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
