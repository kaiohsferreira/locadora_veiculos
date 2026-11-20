using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.ViewObjects.Vistorias;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AutoVia.Tests.Vistorias
{
    public class VistoriaControllerTests
    {
        private static VistoriaController CriarController(string? claim)
        {
            VistoriaService servico = VistoriaServiceTests.CriarServico(new FakeVistoriaRepository());
            DefaultHttpContext contexto = new DefaultHttpContext();
            if (claim != null)
            {
                contexto.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, claim) }, "test"));
            }
            return new VistoriaController(servico, new FakeLogService()) { ControllerContext = new ControllerContext { HttpContext = contexto } };
        }

        [Fact]
        public async Task PostRetorna201ComVOEReferenciaAoGetNaMesmaRota()
        {
            VistoriaController controller = CriarController("20");
            IActionResult resultado = await controller.RegistrarEntrada(10, VistoriaServiceTests.Entrada());
            CreatedAtActionResult criado = Assert.IsType<CreatedAtActionResult>(resultado);
            ReturnVistoriaVO retorno = Assert.IsType<ReturnVistoriaVO>(criado.Value);
            Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
            Assert.Equal(nameof(VistoriaController.GetEntrada), criado.ActionName);
            Assert.Equal(10, Convert.ToInt32(criado.RouteValues!["idLocacao"]));
            Assert.Equal(20, retorno.IdFuncionario);
            OkObjectResult consulta = Assert.IsType<OkObjectResult>(await controller.GetEntrada(10));
            Assert.Equal(retorno.IdVistoria, Assert.IsType<ReturnVistoriaVO>(consulta.Value).IdVistoria);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("2147483648")]
        public async Task PostExigeClaimInteiraPositiva(string? claim)
        {
            VistoriaController controller = CriarController(claim);
            await Assert.ThrowsAsync<UnauthorizedException>(() => controller.RegistrarEntrada(10, VistoriaServiceTests.Entrada()));
        }

        [Fact]
        public async Task GetAusentePropaga404()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => CriarController("20").GetEntrada(10));
        }

        [Fact]
        public void ControllerProtegidaPorPolicyFuncionarioERotaContratada()
        {
            AuthorizeAttribute autorizacao = typeof(VistoriaController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
            RouteAttribute rota = typeof(VistoriaController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single();
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao.Policy);
            Assert.Equal("api/locacoes/{idLocacao:int}/vistorias/entrada", rota.Template);
        }
    }
}
