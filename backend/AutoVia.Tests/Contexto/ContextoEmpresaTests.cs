using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Contexto
{
    /// <summary>
    /// Testes do ContextoEmpresa (multi-locadora, T-BE-03b): estado vazio por padrao (fail-closed),
    /// valores gravados por Definir, corte de valores (ADMINISTRADOR e FINANCEIRO) e recusa de id nao positivo com log.
    /// </summary>
    public class ContextoEmpresaTests
    {
        [Fact]
        public void Padrao_EhVazioESemPermissaoDeValores()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            Assert.Equal(0, contexto.IdEmpresa);
            Assert.Equal(0, contexto.IdFuncionario);
            Assert.Null(contexto.Cargo);
            Assert.False(contexto.EmpresaOperadora);
            Assert.False(contexto.PodeVerValores);
        }

        [Fact]
        public void Definir_GravaOsValoresDoBanco()
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());

            contexto.Definir(4, 9, CargoFuncionario.GESTOR_FROTA, true);

            Assert.Equal(4, contexto.IdEmpresa);
            Assert.Equal(9, contexto.IdFuncionario);
            Assert.Equal(CargoFuncionario.GESTOR_FROTA, contexto.Cargo);
            Assert.True(contexto.EmpresaOperadora);
        }

        [Theory]
        [InlineData(CargoFuncionario.ADMINISTRADOR, true)]
        [InlineData(CargoFuncionario.FINANCEIRO, true)]
        [InlineData(CargoFuncionario.ATENDENTE, false)]
        [InlineData(CargoFuncionario.VENDEDOR, false)]
        [InlineData(CargoFuncionario.GESTOR_FROTA, false)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO, false)]
        public void PodeVerValores_SoParaAdministradorEFinanceiro(CargoFuncionario cargo, bool esperado)
        {
            ContextoEmpresa contexto = new ContextoEmpresa(new FakeLogService());
            contexto.Definir(1, 1, cargo, false);

            Assert.Equal(esperado, contexto.PodeVerValores);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        [InlineData(-1, 1)]
        public void Definir_IdNaoPositivo_LancaERegistraSemGravar(int idEmpresa, int idFuncionario)
        {
            FakeLogService log = new FakeLogService();
            ContextoEmpresa contexto = new ContextoEmpresa(log);

            InvalidOperationException excecao = Assert.Throws<InvalidOperationException>(() => contexto.Definir(idEmpresa, idFuncionario, CargoFuncionario.ADMINISTRADOR, true));

            Assert.Equal(ConstantsMessage.ErroDefinirContextoEmpresa, excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroDefinirContextoEmpresa));
            Assert.Equal(0, contexto.IdEmpresa);
            Assert.Null(contexto.Cargo);
        }
    }
}
