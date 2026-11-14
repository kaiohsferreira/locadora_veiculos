using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.Contexto
{
    /// <summary>
    /// Testes das politicas por empresa (T-BE-03b): PoliticaAdministrador (cargo ADMINISTRADOR na empresa
    /// do contexto) e PoliticaOperadora (empresa operadora da plataforma). Os handlers leem o
    /// IContextoEmpresa; sem contexto de empresa, negam.
    /// </summary>
    public class PoliticasEmpresaTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(IAuthorizationHandler handler, IAuthorizationRequirement requisito)
        {
            AuthorizationHandlerContext contexto = new AuthorizationHandlerContext(new[] { requisito }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(contexto);

            return contexto.HasSucceeded;
        }

        private static IContextoEmpresa ContextoDe(CargoFuncionario cargo, bool operadora)
        {
            return ContextoEmpresaDeTeste.Criar(1, cargo, operadora);
        }

        [Fact]
        public async Task Administrador_CargoAdministradorNaEmpresa_Permite()
        {
            AutorizacaoAdministradorHandler handler = new AutorizacaoAdministradorHandler(ContextoDe(CargoFuncionario.ADMINISTRADOR, false), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoAdministrador());

            Assert.True(permitido);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task Administrador_OutroCargo_Nega(CargoFuncionario cargo)
        {
            AutorizacaoAdministradorHandler handler = new AutorizacaoAdministradorHandler(ContextoDe(cargo, false), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoAdministrador());

            Assert.False(permitido);
        }

        [Fact]
        public async Task Administrador_SemContextoDeEmpresa_Nega()
        {
            AutorizacaoAdministradorHandler handler = new AutorizacaoAdministradorHandler(ContextoEmpresaDeTeste.Vazio(), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoAdministrador());

            Assert.False(permitido);
        }

        [Fact]
        public async Task Operadora_AdministradorDaOperadora_Permite()
        {
            AutorizacaoOperadoraHandler handler = new AutorizacaoOperadoraHandler(ContextoDe(CargoFuncionario.ADMINISTRADOR, true), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoOperadora());

            Assert.True(permitido);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task Operadora_OutroCargoDaOperadora_Nega(CargoFuncionario cargo)
        {
            AutorizacaoOperadoraHandler handler = new AutorizacaoOperadoraHandler(ContextoDe(cargo, true), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoOperadora());

            Assert.False(permitido);
        }

        [Fact]
        public async Task Operadora_AdministradorDeOutraEmpresa_Nega()
        {
            AutorizacaoOperadoraHandler handler = new AutorizacaoOperadoraHandler(ContextoDe(CargoFuncionario.ADMINISTRADOR, false), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoOperadora());

            Assert.False(permitido);
        }

        [Fact]
        public async Task Operadora_SemContextoDeEmpresa_Nega()
        {
            AutorizacaoOperadoraHandler handler = new AutorizacaoOperadoraHandler(ContextoEmpresaDeTeste.Vazio(), new FakeLogService());

            bool permitido = await AvaliarAsync(handler, new RequisitoOperadora());

            Assert.False(permitido);
        }
    }
}
