using AutoMapper;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.ClienteLocadora
{
    /// <summary>
    /// CNH por cargo (T-BE-06): o numero completo sai so para ATENDENTE e ADMINISTRADOR; para os demais cargos sai
    /// mascarado no servidor (so os 3 ultimos digitos). O statusCadastro nao muda. A mascara e a regra de StaticMethods.
    /// </summary>
    public class ClienteCnhPorCargoTests
    {
        private const int EmpresaDoContexto = 1;
        private const string CnhCompleta = "12345678901";
        private const string CnhMascarada = "********901";
        private const string EmailTeste = "cliente@exemplo.com";

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static Cliente CriarClienteComCnh(string cnh)
        {
            Cliente cliente = new Cliente
            {
                Nome = "Maria da Silva",
                Cpf = "12345678909",
                DataNascimento = new DateOnly(1980, 1, 1),
                Email = EmailTeste,
                Telefone = "11911112222",
                CnhNumero = cnh,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = new DateOnly(2031, 1, 1),
                StatusCadastro = StatusCadastroCliente.PENDENTE
            };

            return cliente;
        }

        private static (FakeClienteRepository repositorio, ClienteService servico) CriarServico(CargoFuncionario cargo)
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente cliente = CriarClienteComCnh(CnhCompleta);
            repositorio.Adicionar(cliente);
            repositorio.VincularAEmpresa(cliente.IdCliente, EmpresaDoContexto);

            ClienteService servico = new ClienteService(repositorio, new FakeSenhaHasher(), CriarMapper(), new FakeLogService(), ContextoEmpresaDeTeste.Criar(EmpresaDoContexto, cargo));

            return (repositorio, servico);
        }

        [Theory]
        [InlineData(CargoFuncionario.ATENDENTE)]
        [InlineData(CargoFuncionario.ADMINISTRADOR)]
        public async Task GetById_AtendenteOuAdministrador_VeACnhCompleta(CargoFuncionario cargo)
        {
            (FakeClienteRepository repositorio, ClienteService servico) = CriarServico(cargo);

            ReturnClienteVO retorno = await servico.GetByIdAsync(repositorio.Armazenados[0].IdCliente);

            Assert.Equal(CnhCompleta, retorno.CnhNumero);
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR)]
        [InlineData(CargoFuncionario.GESTOR_FROTA)]
        [InlineData(CargoFuncionario.FINANCEIRO)]
        [InlineData(CargoFuncionario.ADMINISTRATIVO)]
        public async Task GetById_OutrosCargos_VeASoOsTresUltimosDigitos(CargoFuncionario cargo)
        {
            (FakeClienteRepository repositorio, ClienteService servico) = CriarServico(cargo);

            ReturnClienteVO retorno = await servico.GetByIdAsync(repositorio.Armazenados[0].IdCliente);

            Assert.Equal(CnhMascarada, retorno.CnhNumero);
        }

        [Theory]
        [InlineData(CargoFuncionario.VENDEDOR, CnhMascarada)]
        [InlineData(CargoFuncionario.ATENDENTE, CnhCompleta)]
        public async Task GetAll_AplicaAMascaraConformeOCargo(CargoFuncionario cargo, string cnhEsperada)
        {
            (FakeClienteRepository repositorio, ClienteService servico) = CriarServico(cargo);

            PaginaVO<ReturnClienteVO> pagina = await servico.GetAllAsync(new ClienteFiltroVO());

            Assert.Equal(cnhEsperada, Assert.Single(pagina.Itens).CnhNumero);
        }

        /// <summary>A mascara nao altera o status do cadastro, que continua vindo do cliente.</summary>
        [Fact]
        public async Task GetById_MascaraNaoMudaOStatusDoCadastro()
        {
            (FakeClienteRepository repositorio, ClienteService servico) = CriarServico(CargoFuncionario.VENDEDOR);

            ReturnClienteVO retorno = await servico.GetByIdAsync(repositorio.Armazenados[0].IdCliente);

            Assert.Equal(StatusCadastroCliente.PENDENTE, retorno.StatusCadastro);
        }

        [Fact]
        public void MascararCnh_DeixaSoOsTresUltimosDigitos()
        {
            string mascarada = StaticMethods.MascararCnh(CnhCompleta);

            Assert.Equal(CnhMascarada, mascarada);
        }

        [Fact]
        public void MascararCnh_ValorComOutroTamanho_DevolveVazio()
        {
            string mascarada = StaticMethods.MascararCnh("123");

            Assert.Equal(string.Empty, mascarada);
        }
    }
}
