using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Controller da lista "Minhas locacoes" sem HTTP: o id do cliente vem so da claim (ausente ou nao numerica vira 401 sem
    /// chamar o servico), o controller registra a falha inesperada uma vez e as excecoes de negocio sobem sem log.
    /// </summary>
    public class MinhasLocacoesControllerTests
    {
        private const string IdClienteTexto = "7";
        private const string ClaimDeIdInvalido = "abc";

        private static MinhasLocacoesController CriarController(IMinhasLocacoesService servico, FakeLogService log, List<Claim> claims)
        {
            ClaimsPrincipal usuario = new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"));
            MinhasLocacoesController controller = new MinhasLocacoesController(servico, log);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = usuario } };

            return controller;
        }

        private static List<Claim> ClaimsDoCliente(string valor)
        {
            return new List<Claim> { new Claim(ClaimTypes.NameIdentifier, valor) };
        }

        [Fact]
        public async Task ClaimAusente_Devolve401SemChamarOServico()
        {
            FakeMinhasLocacoesServico servico = new FakeMinhasLocacoesServico();
            MinhasLocacoesController controller = CriarController(servico, new FakeLogService(), new List<Claim>());

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => controller.ListarMinhas(new FiltroMinhaLocacaoVO()));

            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, excecao.Message);
            Assert.Equal(0, servico.Chamadas);
        }

        [Fact]
        public async Task ClaimNaoNumerica_Devolve401SemChamarOServico()
        {
            FakeMinhasLocacoesServico servico = new FakeMinhasLocacoesServico();
            MinhasLocacoesController controller = CriarController(servico, new FakeLogService(), ClaimsDoCliente(ClaimDeIdInvalido));

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => controller.ListarMinhas(new FiltroMinhaLocacaoVO()));

            Assert.Equal(ConstantsMessage.ErroNaoAutenticado, excecao.Message);
            Assert.Equal(0, servico.Chamadas);
        }

        [Fact]
        public async Task FalhaInesperada_RegistraNoLogUmaVezNoController()
        {
            FakeMinhasLocacoesServico servico = new FakeMinhasLocacoesServico { Falha = new InvalidOperationException("falha de teste") };
            FakeLogService log = new FakeLogService();
            MinhasLocacoesController controller = CriarController(servico, log, ClaimsDoCliente(IdClienteTexto));

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ListarMinhas(new FiltroMinhaLocacaoVO()));

            Assert.Single(log.Entries);
            Assert.Contains(ConstantsMessage.ErroListarMinhasLocacoes, log.Entries[0]);
        }

        [Fact]
        public async Task FalhaDeNegocio_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new BusinessException(ConstantsMessage.ErroLocacaoReservaDuplicada));
        }

        [Fact]
        public async Task FalhaNaoEncontrada_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new NotFoundException(ConstantsMessage.ErroLocacaoNaoEncontrada));
        }

        [Fact]
        public async Task LimiteExcedido_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new LimiteExcedidoException(ConstantsMessage.ErroLimiteRequisicoes));
        }

        [Fact]
        public async Task ValidacaoFluent_RelancaSemRegistrarNoLog()
        {
            await VerificarRelancaSemLogAsync(new ValidationException("falha de validacao de teste"));
        }

        /// <summary>Confere que a excecao sobe com o mesmo tipo e que o controller nao a registra no log.</summary>
        private static async Task VerificarRelancaSemLogAsync(Exception excecao)
        {
            FakeMinhasLocacoesServico servico = new FakeMinhasLocacoesServico { Falha = excecao };
            FakeLogService log = new FakeLogService();
            MinhasLocacoesController controller = CriarController(servico, log, ClaimsDoCliente(IdClienteTexto));

            Exception capturada = await Assert.ThrowsAnyAsync<Exception>(() => controller.ListarMinhas(new FiltroMinhaLocacaoVO()));

            Assert.Equal(excecao.GetType(), capturada.GetType());
            Assert.Empty(log.Entries);
        }

        /// <summary>Servico de mentira: conta as chamadas e lanca a falha programada, sem registrar nada no log.</summary>
        private sealed class FakeMinhasLocacoesServico : IMinhasLocacoesService
        {
            /// <summary>Excecao lancada na chamada; nula devolve uma pagina vazia.</summary>
            public Exception? Falha { get; set; }

            /// <summary>Quantas vezes o controller chamou a listagem.</summary>
            public int Chamadas { get; private set; }

            public Task<PaginaVO<ReturnMinhaLocacaoVO>> ListarAsync(int idCliente, FiltroMinhaLocacaoVO filtro)
            {
                Chamadas++;

                if (Falha != null)
                {
                    throw Falha;
                }

                PaginaVO<ReturnMinhaLocacaoVO> pagina = new PaginaVO<ReturnMinhaLocacaoVO>(new List<ReturnMinhaLocacaoVO>(), 0, 1, 20);

                return Task.FromResult(pagina);
            }
        }
    }
}
