using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Guarda de autorizacao das rotas: todo controller da API declara [Authorize] ou
    /// [AllowAnonymous] na classe ou em cada acao (sem isso a rota cairia na FallbackPolicy
    /// sem intencao). Confere tambem as rotas de login, categoria, veiculo e health.
    /// </summary>
    public class AutorizacaoRotasTests
    {
        private static IEnumerable<Type> Controllers()
        {
            IEnumerable<Type> tipos = typeof(AuthController).Assembly.GetTypes()
                .Where(tipo => typeof(ControllerBase).IsAssignableFrom(tipo) && !tipo.IsAbstract);

            return tipos;
        }

        private static bool Declara(MemberInfo membro)
        {
            return Attribute.IsDefined(membro, typeof(AuthorizeAttribute), true)
                || Attribute.IsDefined(membro, typeof(AllowAnonymousAttribute), true);
        }

        [Fact]
        public void TodoController_TemAutorizacaoNaClasseOuEmCadaAcao()
        {
            List<string> semDeclaracao = new List<string>();

            foreach (Type controller in Controllers())
            {
                if (Declara(controller))
                {
                    continue;
                }

                foreach (MethodInfo acao in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!Declara(acao))
                    {
                        semDeclaracao.Add($"{controller.Name}.{acao.Name}");
                    }
                }
            }

            Assert.Empty(semDeclaracao);
        }

        [Fact]
        public void Login_EAnonimoELimitadoPorIp()
        {
            Assert.True(Attribute.IsDefined(typeof(AuthController), typeof(AllowAnonymousAttribute)));

            MethodInfo login = typeof(AuthController).GetMethod(nameof(AuthController.Login))!;

            Assert.True(Attribute.IsDefined(login, typeof(EnableRateLimitingAttribute)));
        }

        [Fact]
        public void Categoria_ListagemEConsultaSaoAnonimas()
        {
            MethodInfo listagem = typeof(CategoriaController).GetMethod(nameof(CategoriaController.GetAll))!;
            MethodInfo consulta = typeof(CategoriaController).GetMethod(nameof(CategoriaController.GetById))!;

            Assert.True(Attribute.IsDefined(listagem, typeof(AllowAnonymousAttribute)));
            Assert.True(Attribute.IsDefined(consulta, typeof(AllowAnonymousAttribute)));
        }

        [Fact]
        public void Categoria_PostEPutExigemFuncionario()
        {
            MethodInfo insercao = typeof(CategoriaController).GetMethod(nameof(CategoriaController.Insert))!;
            MethodInfo alteracao = typeof(CategoriaController).GetMethod(nameof(CategoriaController.Update))!;

            AuthorizeAttribute? autorizacaoInsercao = insercao.GetCustomAttribute<AuthorizeAttribute>();
            AuthorizeAttribute? autorizacaoAlteracao = alteracao.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacaoInsercao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacaoInsercao!.Policy);
            Assert.NotNull(autorizacaoAlteracao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacaoAlteracao!.Policy);
        }

        [Fact]
        public void Veiculo_ExigeFuncionarioNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(VeiculoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);
        }

        [Fact]
        public void Health_EAnonimo()
        {
            Assert.True(Attribute.IsDefined(typeof(HealthController), typeof(AllowAnonymousAttribute)));
        }
    }
}
