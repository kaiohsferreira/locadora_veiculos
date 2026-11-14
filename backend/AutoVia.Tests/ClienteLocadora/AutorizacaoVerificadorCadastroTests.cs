using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// Politica VerificadorCadastro (T-BE-06): so ATENDENTE e ADMINISTRADOR da locadora do contexto passam no requisito.
    /// O handler le o IContextoEmpresa; sem locadora no contexto, nega. (O 403 para token de cliente e a rota sem cargo
    /// valido estao no teste HTTP, VerificacaoClienteHttpTests.)
    /// </summary>
    public class AutorizacaoVerificadorCadastroTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(IContextoEmpresa contexto)
        {
            AutorizacaoVerificadorCadastroHandler handler = new AutorizacaoVerificadorCadastroHandler(contexto, new FakeLogService());
            AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoVerificadorCadastro() }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(avaliacao);

            return avaliacao.HasSucceeded;
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task AtendenteOuAdministrador_DaLocadoraDoContexto_Permite(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo));

            Assert.True(permitido);
        }

        [Theory]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task OutroCargo_Nega(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo));

            Assert.False(permitido);
        }

        [Fact]
        public async Task SemContextoDeEmpresa_Nega()
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Vazio());

            Assert.False(permitido);
        }
    }
}
