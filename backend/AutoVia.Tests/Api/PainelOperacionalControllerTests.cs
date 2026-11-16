using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    public class PainelOperacionalControllerTests
    {
        [Fact]
        public void Controller_ExigePoliticaFuncionario()
        {
            AuthorizeAttribute? autorizacao = typeof(PainelOperacionalController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);
        }

        [Fact]
        public void GetHoje_DeclaraRotaDoContrato()
        {
            MethodInfo? acao = typeof(PainelOperacionalController).GetMethod(nameof(PainelOperacionalController.GetHojeAsync));

            Assert.NotNull(acao);
            HttpGetAttribute? rota = acao!.GetCustomAttribute<HttpGetAttribute>();

            Assert.NotNull(rota);
            Assert.Equal("hoje", rota!.Template);
            RouteAttribute? baseRota = typeof(PainelOperacionalController).GetCustomAttribute<RouteAttribute>();
            Assert.NotNull(baseRota);
            Assert.Equal("api/painel-operacional", baseRota!.Template);
        }
    }
}
