using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.Devolucao;

/// <summary>
/// Politica EncerramentoLocacao (D6, devolucao-encerramento v4): so ATENDENTE e ADMINISTRADOR da locadora do contexto encerram.
/// O cargo vem do IContextoEmpresa (banco), nunca da claim. VENDEDOR, GESTOR_FROTA, FINANCEIRO e ADMINISTRATIVO recebem 403.
/// </summary>
public sealed class AutorizacaoEncerramentoLocacaoTests
{
    private static ClaimsPrincipal FuncionarioAutenticado()
    {
        ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

        return new ClaimsPrincipal(identidade);
    }

    private static async Task<bool> AvaliarAsync(IContextoEmpresa contexto)
    {
        AutorizacaoEncerramentoLocacaoHandler handler = new AutorizacaoEncerramentoLocacaoHandler(contexto, new FakeLogService());
        AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoEncerramentoLocacao() }, FuncionarioAutenticado(), null);

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
    [InlineData(CargoFuncionario.VENDEDOR)]
    [InlineData(CargoFuncionario.GESTOR_FROTA)]
    [InlineData(CargoFuncionario.FINANCEIRO)]
    [InlineData(CargoFuncionario.ADMINISTRATIVO)]
    public async Task OutroCargo_Nega403(CargoFuncionario cargo)
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
