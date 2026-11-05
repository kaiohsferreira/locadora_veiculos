using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.Options;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Testes do AuthController chamado direto: login valido devolve 200 com ReturnLoginVO,
    /// recusa de credenciais vira UnauthorizedException (o middleware transforma em 401) e
    /// falha inesperada e registrada e relancada.
    /// </summary>
    public class AuthControllerTests
    {
        private const string CpfCliente = "52998224725";
        private const string SenhaCliente = "SenhaCliente#2026";

        private static AuthController CriarController(FakeAutenticacaoRepository repositorio, FakeLogService log)
        {
            JwtOptions opcoes = new JwtOptions
            {
                Emissor = "AutoVia.Api",
                Audiencia = "AutoVia.Web",
                ExpiracaoMinutos = 120,
                Chave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            };

            SenhaHasher hasher = new SenhaHasher(log);
            TokenService tokens = new TokenService(opcoes, log);
            AutenticacaoService servico = new AutenticacaoService(repositorio, hasher, tokens, log);

            return new AuthController(servico, log);
        }

        private static LoginVO Login(PerfilUsuario perfil, string cpf, string senha)
        {
            return new LoginVO { Perfil = perfil, Cpf = cpf, Senha = senha };
        }

        [Fact]
        public async Task Login_CredenciaisValidas_DevolveOkComReturnLoginVO()
        {
            FakeLogService log = new FakeLogService();
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.AdicionarCliente(new Cliente { Nome = "Maria", Cpf = CpfCliente, SenhaHash = new SenhaHasher(log).Hash(SenhaCliente), StatusCadastro = StatusCadastroCliente.APROVADO });
            AuthController controller = CriarController(repositorio, log);

            IActionResult resultado = await controller.Login(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnLoginVO retorno = Assert.IsType<ReturnLoginVO>(ok.Value);
            Assert.False(string.IsNullOrWhiteSpace(retorno.Token));
            Assert.Equal(StatusCadastroCliente.APROVADO, retorno.StatusCadastro);
        }

        [Fact]
        public async Task Login_CredenciaisRecusadas_PropagaUnauthorizedException()
        {
            FakeLogService log = new FakeLogService();
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            AuthController controller = CriarController(repositorio, log);

            UnauthorizedException excecao = await Assert.ThrowsAsync<UnauthorizedException>(() => controller.Login(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal(ConstantsMessage.ErroCredenciaisInvalidas, excecao.Message);
        }

        [Fact]
        public async Task Login_FalhaInesperada_RegistraErroAutenticarERelanca()
        {
            FakeLogService log = new FakeLogService();
            FakeAutenticacaoRepository repositorio = new FakeAutenticacaoRepository();
            repositorio.ExcecaoAoBuscar = new InvalidOperationException("falha simulada");
            AuthController controller = CriarController(repositorio, log);

            InvalidOperationException excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Login(Login(PerfilUsuario.CLIENTE, CpfCliente, SenhaCliente)));

            Assert.Equal("falha simulada", excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroAutenticar));
        }
    }
}
