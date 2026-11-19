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
        public void Categoria_PostEPutExigemOperadora()
        {
            MethodInfo insercao = typeof(CategoriaController).GetMethod(nameof(CategoriaController.Insert))!;
            MethodInfo alteracao = typeof(CategoriaController).GetMethod(nameof(CategoriaController.Update))!;

            AuthorizeAttribute? autorizacaoInsercao = insercao.GetCustomAttribute<AuthorizeAttribute>();
            AuthorizeAttribute? autorizacaoAlteracao = alteracao.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacaoInsercao);
            Assert.Equal(ConstantsValues.PoliticaOperadora, autorizacaoInsercao!.Policy);
            Assert.NotNull(autorizacaoAlteracao);
            Assert.Equal(ConstantsValues.PoliticaOperadora, autorizacaoAlteracao!.Policy);
        }

        [Fact]
        public void Veiculo_ExigeFuncionarioNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(VeiculoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);
        }

        [Fact]
        public void Veiculo_TrocaDeStatusExigePoliticaGestorFrotaNaAcao()
        {
            MethodInfo acao = typeof(VeiculoController).GetMethod(nameof(VeiculoController.AlterarStatus))!;
            AuthorizeAttribute? autorizacao = acao.GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaGestorFrota, autorizacao!.Policy);
        }

        [Fact]
        public void Avaria_ExigeFuncionarioNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(AvariaController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);
        }

        [Fact]
        public void Veiculo_CatalogoAVendaEAnonimo()
        {
            MethodInfo catalogo = typeof(VeiculoController).GetMethod(nameof(VeiculoController.GetCatalogo))!;

            Assert.True(Attribute.IsDefined(catalogo, typeof(AllowAnonymousAttribute)));
        }

        [Fact]
        public void Manutencao_ExigePoliticaGestorFrotaNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(ManutencaoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaGestorFrota, autorizacao!.Policy);
        }

        [Fact]
        public void Manutencao_AberturaEncerramentoEAlertasNaoSaoAnonimas()
        {
            Assert.False(Attribute.IsDefined(typeof(ManutencaoController), typeof(AllowAnonymousAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(ManutencaoController).GetMethod(nameof(ManutencaoController.Abrir))!, typeof(AllowAnonymousAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(ManutencaoController).GetMethod(nameof(ManutencaoController.Encerrar))!, typeof(AllowAnonymousAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(ManutencaoController).GetMethod(nameof(ManutencaoController.Alertas))!, typeof(AllowAnonymousAttribute), true));
        }

        [Fact]
        public void Pagamento_ExigePoliticaFinanceiroNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(PagamentoController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFinanceiro, autorizacao!.Policy);
        }

        [Fact]
        public void PagamentoCliente_ExigePoliticaClienteNaClasseERotasDoClienteNaoSaoAnonimas()
        {
            AuthorizeAttribute? autorizacao = typeof(PagamentoClienteController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaCliente, autorizacao!.Policy);
            Assert.False(Attribute.IsDefined(typeof(PagamentoClienteController), typeof(AllowAnonymousAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(PagamentoClienteController).GetMethod(nameof(PagamentoClienteController.ListarMeus))!, typeof(AllowAnonymousAttribute), true));
            Assert.False(Attribute.IsDefined(typeof(PagamentoClienteController).GetMethod(nameof(PagamentoClienteController.ResumoMeus))!, typeof(AllowAnonymousAttribute), true));
        }

        [Fact]
        public void Seguro_ExigeFuncionarioNaClasse()
        {
            AuthorizeAttribute? autorizacao = typeof(SeguroController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);
        }

        [Fact]
        public void Health_EAnonimo()
        {
            Assert.True(Attribute.IsDefined(typeof(HealthController), typeof(AllowAnonymousAttribute)));
        }

        [Fact]
        public void Empresa_CadastroEVerificacaoSaoAnonimasELimitadas()
        {
            MethodInfo cadastro = typeof(EmpresaController).GetMethod(nameof(EmpresaController.Insert))!;
            MethodInfo verificacao = typeof(EmpresaController).GetMethod(nameof(EmpresaController.VerificarCadastro))!;

            Assert.True(Attribute.IsDefined(cadastro, typeof(AllowAnonymousAttribute)));
            Assert.True(Attribute.IsDefined(verificacao, typeof(AllowAnonymousAttribute)));
            Assert.True(Attribute.IsDefined(cadastro, typeof(EnableRateLimitingAttribute)));
            Assert.True(Attribute.IsDefined(verificacao, typeof(EnableRateLimitingAttribute)));
        }

        [Fact]
        public void Equipe_TodasAsRotasExigemPoliticaAdministrador()
        {
            AuthorizeAttribute? autorizacao = typeof(EquipeController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaAdministrador, autorizacao!.Policy);

            foreach (MethodInfo acao in typeof(EquipeController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.False(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)), acao.Name);
            }
        }

        [Fact]
        public void Painel_TodasAsRotasExigemPoliticaFuncionario()
        {
            AuthorizeAttribute? autorizacao = typeof(PainelController).GetCustomAttribute<AuthorizeAttribute>();

            Assert.NotNull(autorizacao);
            Assert.Equal(ConstantsValues.PoliticaFuncionario, autorizacao!.Policy);

            foreach (MethodInfo acao in typeof(PainelController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Assert.False(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)), acao.Name);
            }
        }
    }
}
