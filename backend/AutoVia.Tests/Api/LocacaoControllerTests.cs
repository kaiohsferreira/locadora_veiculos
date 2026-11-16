using AutoMapper;
using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    public class LocacaoControllerTests
    {
        [Fact]
        public async Task StartFromReserva_Valido_Retorna201ComLocationParaGetById()
        {
            LocacaoController controller = CriarController(CriarRepositorioValido(), "20");

            IActionResult resultado = await controller.StartFromReserva(new LocacaoVO { IdReserva = 10 });

            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(LocacaoController.GetById), criado.ActionName);
            ReturnLocacaoVO retorno = Assert.IsType<ReturnLocacaoVO>(criado.Value);
            Assert.True(retorno.IdLocacao > 0);
            Assert.Equal(retorno.IdLocacao, Convert.ToInt32(criado.RouteValues!["id"]));
        }

        [Fact]
        public async Task GetById_LocacaoExistente_Retorna200ComVO()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            repositorio.Adicionar(new Locacao
            {
                IdLocacao = 1,
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                IdFuncionario = 20,
                DataRetirada = new DateTime(2026, 11, 19, 10, 0, 0),
                DataDevolucaoPrevista = new DateTime(2026, 11, 20, 10, 0, 0),
                KmSaida = 1200,
                ValorDiaria = 175m,
                Status = StatusLocacao.EM_ANDAMENTO
            });
            LocacaoController controller = CriarController(repositorio, "20");

            IActionResult resultado = await controller.GetById(1);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnLocacaoVO retorno = Assert.IsType<ReturnLocacaoVO>(ok.Value);
            Assert.Equal(1, retorno.IdLocacao);
            Assert.Equal(10, retorno.IdReserva);
            Assert.Equal(StatusLocacao.EM_ANDAMENTO, retorno.Status);
            Assert.Equal("19/11/2026 10:00", retorno.DataRetiradaStr);
            Assert.Equal("20/11/2026", retorno.DataDevolucaoPrevistaStr);
        }

        [Fact]
        public async Task StartFromReserva_SemClaimDeFuncionario_PropagaNaoAutenticado()
        {
            LocacaoController controller = CriarController(CriarRepositorioValido(), null);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(
                () => controller.StartFromReserva(new LocacaoVO { IdReserva = 10 }));

            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, excecao.Message);
        }

        [Fact]
        public void Controller_EProtegidaPelaPolicyFuncionario()
        {
            AuthorizeAttribute autorizacao = typeof(LocacaoController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .Single();

            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao.Policy);
        }

        private static LocacaoController CriarController(FakeLocacaoRepository repositorio, string? idFuncionario)
        {
            MapperConfiguration configuracao = new MapperConfiguration(
                cfg => cfg.AddProfile<LocacaoProfile>(),
                NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();
            FakeLogService logService = new FakeLogService();
            FakeRelogio relogio = new FakeRelogio(new DateTime(2026, 11, 19, 9, 30, 0));
            LocacaoService servico = new LocacaoService(repositorio, relogio, mapper, logService);
            LocacaoController controller = new LocacaoController(servico, logService);
            DefaultHttpContext contexto = new DefaultHttpContext();

            if (idFuncionario != null)
            {
                Claim claim = new Claim(ClaimTypes.NameIdentifier, idFuncionario);
                ClaimsIdentity identidade = new ClaimsIdentity(new[] { claim }, "test");
                contexto.User = new ClaimsPrincipal(identidade);
            }

            controller.ControllerContext = new ControllerContext { HttpContext = contexto };

            return controller;
        }

        private static FakeLocacaoRepository CriarRepositorioValido()
        {
            FakeLocacaoRepository repositorio = new FakeLocacaoRepository();
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40,
                KmAtual = 1200,
                ValorDiaria = 175m,
                Status = StatusVeiculo.RESERVADO
            };
            Reserva reserva = new Reserva
            {
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                DataDevolucaoPrevista = new DateTime(2026, 11, 20, 10, 0, 0),
                Status = StatusReserva.ATIVA,
                Veiculo = veiculo
            };
            Funcionario funcionario = new Funcionario
            {
                IdFuncionario = 20,
                Ativo = true
            };

            repositorio.Adicionar(reserva);
            repositorio.Adicionar(funcionario);

            return repositorio;
        }
    }
}
