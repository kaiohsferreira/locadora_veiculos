using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Politica AberturaLocacao (decisao 4 de 24/11/2026; contrato caucao-retirada v1.1): sem caucao exigida concede para os
    /// seis cargos e ate sem contexto (RF-17 como hoje); com caucao exigida so ATENDENTE e ADMINISTRADOR da locadora do contexto.
    /// O handler le o IContextoEmpresa (cargo do banco) e o CaucaoRetiradaOptions.
    /// </summary>
    public class AutorizacaoAberturaLocacaoTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(decimal valorCaucao, IContextoEmpresa contexto)
        {
            AutorizacaoAberturaLocacaoHandler handler = new AutorizacaoAberturaLocacaoHandler(new CaucaoRetiradaOptions(valorCaucao), contexto, new FakeLogService());
            AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoAberturaLocacao() }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(avaliacao);

            return avaliacao.HasSucceeded;
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task ValorZero_ConcedeParaQualquerCargo(CargoFuncionario cargo)
        {
            Assert.True(await AvaliarAsync(0m, ContextoEmpresaDeTeste.Criar(1, cargo)));
        }

        [Fact]
        public async Task ValorZero_ConcedeMesmoSemContexto()
        {
            Assert.True(await AvaliarAsync(0m, ContextoEmpresaDeTeste.Vazio()));
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task ComCaucao_AtendenteEAdministradorDaLocadora_Concede(CargoFuncionario cargo)
        {
            Assert.True(await AvaliarAsync(1200.00m, ContextoEmpresaDeTeste.Criar(1, cargo)));
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task ComCaucao_OutrosCargos_Nega(CargoFuncionario cargo)
        {
            Assert.False(await AvaliarAsync(1200.00m, ContextoEmpresaDeTeste.Criar(1, cargo)));
        }

        [Fact]
        public async Task ComCaucao_SemContexto_Nega()
        {
            Assert.False(await AvaliarAsync(1200.00m, ContextoEmpresaDeTeste.Vazio()));
        }
    }
}
