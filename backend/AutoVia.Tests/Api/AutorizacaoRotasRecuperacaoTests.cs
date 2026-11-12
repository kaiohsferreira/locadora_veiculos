using AutoVia.Api.Controllers;
using AutoVia.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Reflection;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Guarda das rotas publicas de recuperacao de senha (RN1.14): cada acao declara [AllowAnonymous] de forma
    /// explicita, esta sob o limite publico do IP (mesma politica do login), e tem a rota esperada.
    /// </summary>
    public class AutorizacaoRotasRecuperacaoTests
    {
        private const string NomeAcaoSolicitar = nameof(RecuperacaoSenhaController.SolicitarRecuperacaoSenha);
        private const string NomeAcaoRedefinir = nameof(RecuperacaoSenhaController.RedefinirSenha);

        [Fact]
        public void Solicitacao_EAnonimaELimitadaPorIp()
        {
            MethodInfo acao = typeof(RecuperacaoSenhaController).GetMethod(NomeAcaoSolicitar)!;

            Assert.True(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)));

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimitePublico, limite!.PolicyName);
        }

        [Fact]
        public void Redefinicao_EAnonimaELimitadaPorIp()
        {
            MethodInfo acao = typeof(RecuperacaoSenhaController).GetMethod(NomeAcaoRedefinir)!;

            Assert.True(Attribute.IsDefined(acao, typeof(AllowAnonymousAttribute)));

            EnableRateLimitingAttribute? limite = acao.GetCustomAttribute<EnableRateLimitingAttribute>();

            Assert.NotNull(limite);
            Assert.Equal(ConstantsValues.PoliticaLimitePublico, limite!.PolicyName);
        }

        [Fact]
        public void Rotas_SaoPostSobOPrefixoDeAuth()
        {
            RouteAttribute? rotaControlador = typeof(RecuperacaoSenhaController).GetCustomAttribute<RouteAttribute>();
            HttpPostAttribute? postSolicitar = typeof(RecuperacaoSenhaController).GetMethod(NomeAcaoSolicitar)!.GetCustomAttribute<HttpPostAttribute>();
            HttpPostAttribute? postRedefinir = typeof(RecuperacaoSenhaController).GetMethod(NomeAcaoRedefinir)!.GetCustomAttribute<HttpPostAttribute>();

            Assert.NotNull(rotaControlador);
            Assert.Equal("api/auth", rotaControlador!.Template);
            Assert.NotNull(postSolicitar);
            Assert.Equal("recuperacao-senha", postSolicitar!.Template);
            Assert.NotNull(postRedefinir);
            Assert.Equal("redefinicao-senha", postRedefinir!.Template);
        }
    }
}
