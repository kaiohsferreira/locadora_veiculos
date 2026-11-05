using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Clientes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do ClienteController chamado direto (sem servidor HTTP): 200 na listagem e na
    /// consulta, 201 com CreatedAtAction apontando para GetById, propagacao de 404 e 409 para
    /// o ErrorHandlingMiddleware, e a checagem por reflexao das politicas de cada rota
    /// (Insert anonimo; GetAll e GetById exigem a politica de funcionario).
    /// </summary>
    public class ClienteControllerTests
    {
        private const string CpfA = "12345678909";
        private const string CnhA = "12345678901";
        private const string SenhaTeste = "senha1234";

        [Fact]
        public async Task GetAll_DevolveOkComPaginaSoDeAtivos()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarCliente("Ana", "11111111111", "CNH001", StatusCadastroCliente.PENDENTE));
            repositorio.Adicionar(CriarCliente("Bruno", "22222222222", "CNH002", StatusCadastroCliente.APROVADO));
            Cliente desativado = CriarCliente("Carla", "33333333333", "CNH003", StatusCadastroCliente.PENDENTE);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteController controller = CriarController(repositorio);

            IActionResult resultado = await controller.GetAll(new ClienteFiltroVO { Pagina = 1, TamanhoPagina = 20 });

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            PaginaVO<ReturnClienteVO> pagina = Assert.IsType<PaginaVO<ReturnClienteVO>>(ok.Value);
            Assert.Equal(2, pagina.Total);
            Assert.Equal(new List<string> { "Ana", "Bruno" }, pagina.Itens.Select(x => x.Nome).ToList());
        }

        [Fact]
        public async Task GetById_ClienteAtivo_DevolveOkComOCliente()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente ativo = CriarCliente("Maria", CpfA, CnhA, StatusCadastroCliente.PENDENTE);
            repositorio.Adicionar(ativo);
            ClienteController controller = CriarController(repositorio);

            IActionResult resultado = await controller.GetById(ativo.IdCliente);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnClienteVO cliente = Assert.IsType<ReturnClienteVO>(ok.Value);
            Assert.Equal(ativo.IdCliente, cliente.IdCliente);
            Assert.Equal("123.456.789-09", cliente.CpfStr);
        }

        [Fact]
        public async Task GetById_IdInexistente_PropagaNaoEncontrada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteController controller = CriarController(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(99));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task GetById_ClienteDesativado_PropagaNaoEncontrada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            Cliente desativado = CriarCliente("Diego", CpfA, CnhA, StatusCadastroCliente.PENDENTE);
            desativado.Disable();
            repositorio.Adicionar(desativado);
            ClienteController controller = CriarController(repositorio);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => controller.GetById(desativado.IdCliente));

            Assert.Equal(ConstantsMessage.ErroClienteNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Insert_Valido_DevolveCreatedAtApontandoParaGetById()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            ClienteController controller = CriarController(repositorio);

            IActionResult resultado = await controller.Insert(CriarVOValido());

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(ClienteController.GetById), criado.ActionName);
            ReturnClienteVO valor = Assert.IsType<ReturnClienteVO>(criado.Value);
            Assert.Equal(StatusCadastroCliente.PENDENTE, valor.StatusCadastro);
            object? idRota = criado.RouteValues?["id"];
            Assert.Equal(valor.IdCliente, Convert.ToInt32(idRota));
        }

        [Fact]
        public async Task Insert_CpfDeClienteAtivo_PropagaCpfDuplicado()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarCliente("Existente", CpfA, "CNH999", StatusCadastroCliente.APROVADO));
            ClienteController controller = CriarController(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Insert(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteCpfDuplicado, excecao.Message);
        }

        [Fact]
        public async Task Insert_CnhDeClienteAtivo_PropagaCnhDuplicada()
        {
            FakeClienteRepository repositorio = new FakeClienteRepository();
            repositorio.Adicionar(CriarCliente("Existente", "98765432100", CnhA, StatusCadastroCliente.PENDENTE));
            ClienteController controller = CriarController(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.Insert(CriarVOValido()));

            Assert.Equal(ConstantsMessage.ErroClienteCnhDuplicada, excecao.Message);
        }

        [Fact]
        public void Insert_EhRotaAnonima_SemPoliticaDeAutorizacao()
        {
            MethodInfo metodo = ObterMetodo(nameof(ClienteController.Insert));

            Assert.NotNull(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.Null(metodo.GetCustomAttribute<AuthorizeAttribute>());
        }

        [Fact]
        public void GetAllEGetById_ExigemAPoliticaDeFuncionario()
        {
            MethodInfo listagem = ObterMetodo(nameof(ClienteController.GetAll));
            MethodInfo consulta = ObterMetodo(nameof(ClienteController.GetById));

            AuthorizeAttribute? autorizacaoListagem = listagem.GetCustomAttribute<AuthorizeAttribute>();
            AuthorizeAttribute? autorizacaoConsulta = consulta.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacaoListagem);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacaoListagem.Policy);
            Assert.NotNull(autorizacaoConsulta);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacaoConsulta.Policy);
            Assert.Null(listagem.GetCustomAttribute<AllowAnonymousAttribute>());
            Assert.Null(consulta.GetCustomAttribute<AllowAnonymousAttribute>());
        }

        [Fact]
        public void Controller_TemRotaApiClientes()
        {
            RouteAttribute? rota = typeof(ClienteController).GetCustomAttribute<RouteAttribute>();

            Assert.NotNull(rota);
            Assert.Equal("api/clientes", rota.Template);
        }

        private static MethodInfo ObterMetodo(string nome)
        {
            MethodInfo? metodo = typeof(ClienteController).GetMethod(nome, BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(metodo);

            return metodo;
        }

        private static ClienteVO CriarVOValido()
        {
            ClienteVO vo = new ClienteVO
            {
                Nome = "Maria da Silva",
                Cpf = CpfA,
                DataNascimento = new DateOnly(1990, 5, 20),
                Email = "maria.silva@exemplo.com",
                Telefone = "11987654321",
                CnhNumero = CnhA,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = DateOnly.FromDateTime(DateTime.Now).AddYears(3),
                Senha = SenhaTeste
            };

            return vo;
        }

        private static Cliente CriarCliente(string nome, string cpf, string cnh, StatusCadastroCliente status)
        {
            Cliente cliente = new Cliente
            {
                Nome = nome,
                Cpf = cpf,
                DataNascimento = new DateOnly(1980, 1, 1),
                Email = "existente@exemplo.com",
                Telefone = "11911112222",
                CnhNumero = cnh,
                CnhCategoria = CategoriaCnh.B,
                CnhValidade = new DateOnly(2031, 1, 1),
                StatusCadastro = status
            };

            return cliente;
        }

        private static ClienteController CriarController(FakeClienteRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<ClienteProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService logService = new FakeLogService();
            ClienteService servico = new ClienteService(repositorio, new FakeSenhaHasher(), mapper, logService);
            ClienteController controller = new ClienteController(servico, logService);

            return controller;
        }
    }
}
