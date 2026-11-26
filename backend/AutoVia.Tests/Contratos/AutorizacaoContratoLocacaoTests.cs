using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.Contratos
{
    /// <summary>
    /// Politica ContratoLocacao (contrato-locacao v5): so ATENDENTE e ADMINISTRADOR da locadora do contexto. O cargo vem do
    /// IContextoEmpresa (banco), nunca da claim. VENDEDOR, GESTOR_FROTA, FINANCEIRO e ADMINISTRATIVO recebem 403; sem contexto, nega.
    /// </summary>
    public sealed class AutorizacaoContratoLocacaoTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(IContextoEmpresa contexto)
        {
            AutorizacaoContratoLocacaoHandler handler = new AutorizacaoContratoLocacaoHandler(contexto, new FakeLogService());
            AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoContratoLocacao() }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(avaliacao);

            return avaliacao.HasSucceeded;
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task AtendenteOuAdministrador_DaLocadoraDoContexto_Permite(CargoFuncionario cargo)
        {
            Assert.True(await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo)));
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task OutroCargo_Nega(CargoFuncionario cargo)
        {
            Assert.False(await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo)));
        }

        [Fact]
        public async Task SemContextoDeEmpresa_Nega()
        {
            Assert.False(await AvaliarAsync(ContextoEmpresaDeTeste.Vazio()));
        }
    }
}
