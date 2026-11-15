using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Api
{
    public class ReservaControllerTests
    {
        [Fact]
        public async Task CriacaoRetorna201LocationEIdDoTitularAutenticado()
        {
            FakeReservaService servico = new FakeReservaService();
            ReservaController controller = new ReservaController(servico, new FakeLogService());
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "57")
            }, "test"));
            controller.ControllerContext = new ControllerContext { HttpContext = contexto };
            CriarReservaVO entrada = new CriarReservaVO
            {
                IdVeiculo = 9,
                DataRetirada = new DateOnly(2026, 12, 10),
                DataDevolucao = new DateOnly(2026, 12, 15)
            };

            IActionResult resultado = await controller.CriarAsync(entrada);

            ObjectResult resposta = Assert.IsType<ObjectResult>(resultado);
            Assert.Equal(StatusCodes.Status201Created, resposta.StatusCode);
            Assert.Equal("/api/reservas/91", contexto.Response.Headers.Location);
            Assert.Equal(57, servico.IdClienteRecebido);
        }

        [Fact]
        public async Task GetByIdEncaminhaIdDaReservaETitularERetornaVO()
        {
            FakeReservaService servico = new FakeReservaService
            {
                RetornoPorId = new ReturnReservaVO { IdReserva = 91, IdVeiculo = 9 }
            };
            ReservaController controller = CriarController(servico, 57);

            IActionResult resultado = await controller.GetByIdAsync(91);

            OkObjectResult resposta = Assert.IsType<OkObjectResult>(resultado);
            Assert.Same(servico.RetornoPorId, resposta.Value);
            Assert.Equal(91, servico.IdReservaRecebido);
            Assert.Equal(57, servico.IdClienteRecebido);
        }

        [Fact]
        public async Task GetAllEncaminhaTitularStatusEPaginacaoERetornaPagina()
        {
            PaginaVO<ReturnReservaVO> pagina = new PaginaVO<ReturnReservaVO>(new List<ReturnReservaVO>(), 0, 2, 3);
            FakeReservaService servico = new FakeReservaService { RetornoDaLista = pagina };
            ReservaController controller = CriarController(servico, 57);
            FiltroReservaVO filtro = new FiltroReservaVO
            {
                Status = StatusReserva.ATIVA.ToString(),
                Pagina = 2,
                TamanhoPagina = 3
            };

            IActionResult resultado = await controller.GetAllAsync(filtro);

            OkObjectResult resposta = Assert.IsType<OkObjectResult>(resultado);
            Assert.Same(pagina, resposta.Value);
            Assert.Equal(57, servico.IdClienteRecebido);
            Assert.Same(filtro, servico.FiltroRecebido);
        }

        [Fact]
        public async Task CancelarRetorna204EEncaminhaTitular()
        {
            FakeReservaService servico = new FakeReservaService();
            ReservaController controller = CriarController(servico, 57);

            IActionResult resultado = await controller.CancelarAsync(91);

            Assert.IsType<NoContentResult>(resultado);
            Assert.Equal(91, servico.IdReservaRecebido);
            Assert.Equal(57, servico.IdClienteRecebido);
        }

        [Fact]
        public async Task GetByIdRelancaNotFoundSemRegistrarLogInesperado()
        {
            FakeReservaService servico = new FakeReservaService { ExcecaoAoBuscar = new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada) };
            FakeLogService log = new FakeLogService();
            ReservaController controller = new ReservaController(servico, log);
            ConfigurarContexto(controller, 57);

            await Assert.ThrowsAsync<NotFoundException>(() => controller.GetByIdAsync(91));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task GetAllRelancaBusinessSemRegistrarLogInesperado()
        {
            FakeReservaService servico = new FakeReservaService { ExcecaoAoListar = new BusinessException(ConstantsMessage.ErroReservaConcluidaNaoCancelavel) };
            FakeLogService log = new FakeLogService();
            ReservaController controller = new ReservaController(servico, log);
            ConfigurarContexto(controller, 57);

            await Assert.ThrowsAsync<BusinessException>(() => controller.GetAllAsync(new FiltroReservaVO()));

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task CancelarRelancaNotFoundSemRegistrarLogInesperado()
        {
            FakeReservaService servico = new FakeReservaService { ExcecaoAoCancelar = new NotFoundException(ConstantsMessage.ErroReservaNaoEncontrada) };
            FakeLogService log = new FakeLogService();
            ReservaController controller = new ReservaController(servico, log);
            ConfigurarContexto(controller, 57);

            await Assert.ThrowsAsync<NotFoundException>(() => controller.CancelarAsync(91));

            Assert.Empty(log.Entries);
        }

        private static ReservaController CriarController(FakeReservaService servico, int idCliente)
        {
            ReservaController controller = new ReservaController(servico, new FakeLogService());
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, idCliente.ToString())
            }, "test"));
            controller.ControllerContext = new ControllerContext { HttpContext = contexto };
            return controller;
        }

        private static void ConfigurarContexto(ReservaController controller, int idCliente)
        {
            DefaultHttpContext contexto = new DefaultHttpContext();
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, idCliente.ToString())
            }, "test"));
            controller.ControllerContext = new ControllerContext { HttpContext = contexto };
        }

        private sealed class FakeReservaService : IReservaService
        {
            public int IdClienteRecebido { get; private set; }
            public int IdReservaRecebido { get; private set; }
            public FiltroReservaVO? FiltroRecebido { get; private set; }
            public ReturnReservaVO? RetornoPorId { get; set; }
            public PaginaVO<ReturnReservaVO>? RetornoDaLista { get; set; }
            public Exception? ExcecaoAoCancelar { get; set; }
            public Exception? ExcecaoAoBuscar { get; set; }
            public Exception? ExcecaoAoListar { get; set; }

            public Task<ReturnReservaCriadaVO> CriarAsync(int idCliente, CriarReservaVO entrada)
            {
                IdClienteRecebido = idCliente;
                ReturnReservaCriadaVO retorno = new ReturnReservaCriadaVO
                {
                    IdReserva = 91,
                    IdVeiculo = entrada.IdVeiculo,
                    DataRetirada = entrada.DataRetirada,
                    DataDevolucao = entrada.DataDevolucao
                };
                return Task.FromResult(retorno);
            }

            public Task<ReturnReservaVO> GetByIdAsync(int idReserva, int idCliente)
            {
                IdReservaRecebido = idReserva;
                IdClienteRecebido = idCliente;
                if (ExcecaoAoBuscar != null)
                {
                    return Task.FromException<ReturnReservaVO>(ExcecaoAoBuscar);
                }
                ReturnReservaVO retorno = RetornoPorId ?? new ReturnReservaVO { IdReserva = idReserva };
                return Task.FromResult(retorno);
            }

            public Task<PaginaVO<ReturnReservaVO>> GetAllAsync(int idCliente, FiltroReservaVO filtro)
            {
                IdClienteRecebido = idCliente;
                FiltroRecebido = filtro;
                if (ExcecaoAoListar != null)
                {
                    return Task.FromException<PaginaVO<ReturnReservaVO>>(ExcecaoAoListar);
                }
                PaginaVO<ReturnReservaVO> retorno = RetornoDaLista ?? new PaginaVO<ReturnReservaVO>(new List<ReturnReservaVO>(), 0, filtro.Pagina, filtro.TamanhoPagina);
                return Task.FromResult(retorno);
            }

            public Task CancelarAsync(int idReserva, int idCliente)
            {
                IdReservaRecebido = idReserva;
                IdClienteRecebido = idCliente;
                if (ExcecaoAoCancelar != null)
                {
                    return Task.FromException(ExcecaoAoCancelar);
                }
                return Task.CompletedTask;
            }
        }
    }
}
