using AutoVia.Api.Authorization;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutoVia.Tests.Frota
{
    /// <summary>
    /// Politica GestorFrota (P1-BE-04): so GESTOR_FROTA e ADMINISTRADOR da locadora do contexto passam no requisito. O handler
    /// le o IContextoEmpresa; sem locadora no contexto, nega. (O 403 para ATENDENTE e para token de cliente fica no teste HTTP,
    /// VeiculoStatusHttpTests.)
    /// </summary>
    public class AutorizacaoGestorFrotaTests
    {
        private static ClaimsPrincipal FuncionarioAutenticado()
        {
            ClaimsIdentity identidade = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, nameof(PerfilUsuario.FUNCIONARIO)) }, "teste");

            return new ClaimsPrincipal(identidade);
        }

        private static async Task<bool> AvaliarAsync(IContextoEmpresa contexto)
        {
            AutorizacaoGestorFrotaHandler handler = new AutorizacaoGestorFrotaHandler(contexto, new FakeLogService());
            AuthorizationHandlerContext avaliacao = new AuthorizationHandlerContext(new[] { new RequisitoGestorFrota() }, FuncionarioAutenticado(), null);

            await handler.HandleAsync(avaliacao);

            return avaliacao.HasSucceeded;
        }

        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministrador_DaLocadoraDoContexto_Permite(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo));

            Assert.True(permitido);
        }

        /// <summary>
        /// GESTOR_FROTA e ADMINISTRADOR da empresa operadora (EmpresaOperadora verdadeiro, IdEmpresa maior que zero) passam
        /// como qualquer locadora com contexto: o handler nao olha EmpresaOperadora. Registrado no contrato veiculo-status v1
        /// (19/11/2026). A trava por id_empresa limita a troca aos veiculos da propria empresa.
        /// </summary>
        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GestorFrotaOuAdministradorDaEmpresaOperadora_Permite(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(ContextoEmpresaDeTeste.Criar(1, cargo, true));

            Assert.True(permitido);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
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

        /// <summary>
        /// Cargo permitido, mas sem empresa no contexto (IdEmpresa 0): o ContextoEmpresa real nao deixa definir id zero, entao
        /// o caso so e alcancavel com este stub. O handler tem de negar mesmo com o cargo certo.
        /// </summary>
        [Theory]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task CargoPermitidoSemEmpresaNoContexto_Nega(CargoFuncionario cargo)
        {
            bool permitido = await AvaliarAsync(new ContextoEmpresaFalso(0, cargo));

            Assert.False(permitido);
        }

        /// <summary>
        /// Empresa presente no contexto, mas sem cargo (Cargo nulo): o handler nega, porque nenhum cargo e GESTOR_FROTA ou ADMINISTRADOR.
        /// </summary>
        [Fact]
        public async Task EmpresaSemCargoNoContexto_Nega()
        {
            bool permitido = await AvaliarAsync(new ContextoEmpresaFalso(1, null));

            Assert.False(permitido);
        }

        /// <summary>Stub de IContextoEmpresa com empresa e cargo informados, para o caso que o contexto real nao alcanca.</summary>
        private sealed class ContextoEmpresaFalso : IContextoEmpresa
        {
            public ContextoEmpresaFalso(int idEmpresa, CargoFuncionario? cargo)
            {
                IdEmpresa = idEmpresa;
                Cargo = cargo;
            }

            public int IdEmpresa { get; }

            public int IdFuncionario { get; } = 0;

            public int IdCliente { get; } = 0;

            public CargoFuncionario? Cargo { get; }

            public bool EmpresaOperadora { get; } = false;

            public bool PodeVerValores { get; } = false;

            public void Definir(int idEmpresa, int idFuncionario, CargoFuncionario cargo, bool empresaOperadora)
            {
            }

            public void DefinirCliente(int idCliente)
            {
            }
        }
    }
}
