using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Guarda das rotas publicas do login com Google (RN3.9): cada acao declara [AllowAnonymous] de forma explicita,
    /// login e vinculo estao sob o limite publico do IP (mesma politica do login por CPF), a configuracao nao tem
    /// limite, e as rotas tem o caminho esperado.
    /// </summary>
    public class AutorizacaoRotasGoogleTests
    {
        private const string NomeAcaoConfiguracao = nameof(GoogleAuthController.ObterConfiguracao);
        private const string NomeAcaoLogin = nameof(GoogleAuthController.Login);
        private const string NomeAcaoVinculo = nameof(GoogleAuthController.Vinculo);

        [Fact]
        public void Configuracao_EAnonimaESemLimite()
        {
            MethodInfo acao = typeof(GoogleAuthController).GetMethod(NomeAcaoConfiguracao)!;

            Assert.True(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)));
            Assert.False(Attribute.IsDefined(acao, typeof(EnableRateLimitingAttribute)));
        }

        [Fact]
        public void Login_EAnonimoELimitadoPorIp()
        {
            MethodInfo acao = typeof(GoogleAuthController).GetMethod(NomeAcaoLogin)!;

            Assert.True(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)));

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimitePublico, limite!.PolicyName);
        }

        [Fact]
        public void Vinculo_EAnonimoELimitadoPorIp()
        {
            MethodInfo acao = typeof(GoogleAuthController).GetMethod(NomeAcaoVinculo)!;

            Assert.True(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)));

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimitePublico, limite!.PolicyName);
        }

        [Fact]
        public void Controller_EhApiSemAutorizacaoDeClasse()
        {
            Assert.True(Attribute.IsDefined(typeof(GoogleAuthController), typeof(ApiControllerAttribute)));
            Assert.False(Attribute.IsDefined(typeof(GoogleAuthController), typeof(AuthorizeAttribute)));
        }

        [Fact]
        public void Rotas_SaoSobOPrefixoDoGoogle()
        {
            RouteAttribute? rotaControlador = typeof(GoogleAuthController).GetCustomAttribute<RouteAttribute>();
            HttpGetAttribute? getConfiguracao = typeof(GoogleAuthController).GetMethod(NomeAcaoConfiguracao)!.GetCustomAttribute<HttpGetAttribute>();
            HttpPostAttribute? postLogin = typeof(GoogleAuthController).GetMethod(NomeAcaoLogin)!.GetCustomAttribute<HttpPostAttribute>();
            HttpPostAttribute? postVinculo = typeof(GoogleAuthController).GetMethod(NomeAcaoVinculo)!.GetCustomAttribute<HttpPostAttribute>();

            Assert.NotNull(rotaControlador);
            Assert.Equal("api/auth/google", rotaControlador!.Template);
            Assert.NotNull(getConfiguracao);
            Assert.Equal("configuracao", getConfiguracao!.Template);
            Assert.NotNull(postLogin);
            Assert.Equal("login", postLogin!.Template);
            Assert.NotNull(postVinculo);
            Assert.Equal("vinculo", postVinculo!.Template);
        }
    }
}
