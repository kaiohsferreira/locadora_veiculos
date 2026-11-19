using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Politica Financeiro (cobranca da locadora, RF-31): so FINANCEIRO e ADMINISTRADOR da locadora do contexto passam no
    /// requisito; o handler le o IContextoEmpresa (PodeVerValores, o ponto unico do corte de valores). Sem locadora no
    /// contexto, nega. O 403 pela rota fica em PagamentoHttpTests.
    /// </summary>
    public class AutorizacaoFinanceiroTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(IContextoEmpresa contexto)
        {
            AutorizacaoFinanceiroHandler handler = new AutorizacaoFinanceiroHandler(contexto, new FakeLogService());
            AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoFinanceiro() }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(avaliacao);

            return avaliacao.HasSucceeded;
        }

        [Theory]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task FinanceiroOuAdministrador_DaLocadoraDoContexto_Permite(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo));

            Assert.True(permitido);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task OutrosCargos_Negam(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo));

            Assert.False(permitido);
        }

        [Fact]
        public async Task SemLocadoraNoContexto_Nega()
        {
            ContextoEmpresa semLocadora = new ContextoEmpresa(new FakeLogService());

            bool permitido = await AvaliarAsync(semLocadora);

            Assert.False(permitido);
        }
    }
}
