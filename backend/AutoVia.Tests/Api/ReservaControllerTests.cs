using AutoVia.Api.Controllers;
using AutoVia.Communication.ViewObjects.Reservas;
using AutoVia.Core.ServicesInterface;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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

        private sealed class FakeReservaService : IReservaService
        {
            public int IdClienteRecebido { get; private set; }

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
        }
    }
}
