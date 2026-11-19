using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoFinanceiro lendo o IContextoEmpresa (empresa e cargo vindos do banco na validacao do token, nunca da
    /// claim). Concede para quem tem locadora no contexto e PodeVerValores (FINANCEIRO ou ADMINISTRADOR); sem contexto, nega.
    /// </summary>
    public class AutorizacaoFinanceiroHandler : AuthorizationHandler<RequisitoFinanceiro>
    {
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoFinanceiroHandler(IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito quando o contexto tem locadora e o cargo pode ver valores (FINANCEIRO ou ADMINISTRADOR).</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoFinanceiro requirement)
        {
            try
            {
                bool financeiro = _contextoEmpresa.IdEmpresa > 0 && _contextoEmpresa.PodeVerValores;

                if (financeiro)
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
