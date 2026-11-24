using AutoVia.Communication.Enums;
using AutoVia.Core.Options;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace AutoVia.Api.Authorization
{
    /// <summary>
    /// Avalia RequisitoAberturaLocacao lendo CaucaoRetiradaOptions (valor da caucao, lido na subida) e o IContextoEmpresa (empresa
    /// e cargo vindos do banco na validacao do token, nunca da claim). Regra pura em RegrasCaucaoRetirada.CargoPodeAbrirComCaucao:
    /// sem caucao exigida concede sempre (RF-17 como hoje); com caucao exigida, so ATENDENTE ou ADMINISTRADOR de uma locadora do
    /// contexto. A recusa vira 403 em problem+json antes de o corpo ser lido e antes de qualquer chamada ao provedor.
    /// </summary>
    public class AutorizacaoAberturaLocacaoHandler : AuthorizationHandler<RequisitoAberturaLocacao>
    {
        private readonly CaucaoRetiradaOptions _caucaoRetiradaOptions;
        private readonly IContextoEmpresa _contextoEmpresa;
        private readonly ILogService _logService;

        public AutorizacaoAberturaLocacaoHandler(CaucaoRetiradaOptions caucaoRetiradaOptions, IContextoEmpresa contextoEmpresa, ILogService logService)
        {
            _caucaoRetiradaOptions = caucaoRetiradaOptions;
            _contextoEmpresa = contextoEmpresa;
            _logService = logService;
        }

        /// <summary>Concede o requisito sem caucao exigida, ou com caucao exigida para ATENDENTE ou ADMINISTRADOR da locadora do contexto.</summary>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoAberturaLocacao requirement)
        {
            try
            {
                CargoFuncionario? cargo = _contextoEmpresa.Cargo;
                bool permitido = RegrasCaucaoRetirada.CargoPodeAbrirComCaucao(_caucaoRetiradaOptions.CaucaoExigida, _contextoEmpresa.IdEmpresa, cargo);

                if (permitido)
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
