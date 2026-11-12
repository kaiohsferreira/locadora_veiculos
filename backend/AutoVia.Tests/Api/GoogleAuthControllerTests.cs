using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Controller do login com Google sem HTTP: devolve o VO de cada operacao (200), repassa as excecoes de
    /// negocio, de integracao e de autorizacao para o middleware (sem log), e registra no log qualquer falha inesperada.
    /// </summary>
    public class GoogleAuthControllerTests
    {
        private const string MensagemFalhaInesperada = "Falha inesperada simulada.";
        private const string MensagemIntegracao = "Integracao simulada indisponivel.";

        /// <summary>Controller com HttpContext de teste: as acoes leem HttpContext.RequestAborted (cancelamento do cliente).</summary>
        private static GoogleAuthController CriarController(IAutenticacaoGoogleService servico, ILogService log)
        {
            GoogleAuthController controller = new GoogleAuthController(servico, log);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            return controller;
        }

        /// <summary>Servico do login com Google falso: cada metodo devolve ou lanca o que o teste configurar.</summary>
        private sealed class ServicoGoogleFalso : IAutenticacaoGoogleService
        {
            public Exception? ExcecaoAoConfigurar { get; set; }

            public Exception? ExcecaoAoLogar { get; set; }

            public Exception? ExcecaoAoVincular { get; set; }

            public Task<ReturnGoogleConfiguracaoVO> ObterConfiguracaoAsync(CancellationToken cancellationToken = default)
            {
                if (ExcecaoAoConfigurar != null)
                {
                    throw ExcecaoAoConfigurar;
                }

                ReturnGoogleConfiguracaoVO retorno = new ReturnGoogleConfiguracaoVO { Habilitado = true, ClientId = "cliente-teste", ModoTeste = false };

                return Task.FromResult(retorno);
            }

            public Task<ReturnGoogleLoginVO> LoginAsync(GoogleLoginVO vo, CancellationToken cancellationToken = default)
            {
                if (ExcecaoAoLogar != null)
                {
                    throw ExcecaoAoLogar;
                }

                ReturnGoogleLoginVO retorno = new ReturnGoogleLoginVO { Situacao = SituacaoLoginGoogle.CADASTRO_NECESSARIO, Nome = "Nome Google", Email = "nome@exemplo.com" };

                return Task.FromResult(retorno);
            }

            public Task<ReturnLoginVO> VincularAsync(GoogleVinculoVO vo, CancellationToken cancellationToken = default)
            {
                if (ExcecaoAoVincular != null)
                {
                    throw ExcecaoAoVincular;
                }

                ReturnLoginVO retorno = new ReturnLoginVO { Token = "token-teste", TipoToken = ConstantsValues.TipoTokenBearer, Perfil = PerfilUsuario.CLIENTE, IdUsuario = 7 };

                return Task.FromResult(retorno);
            }
        }

        [Fact]
        public async Task Configuracao_DevolveOkComAVo()
        {
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso(), new FakeLogService());

            IActionResult resultado = await controller.ObterConfiguracao();

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnGoogleConfiguracaoVO corpo = Assert.IsType<ReturnGoogleConfiguracaoVO>(ok.Value);
            Assert.True(corpo.Habilitado);
            Assert.Equal("cliente-teste", corpo.ClientId);
        }

        [Fact]
        public async Task Login_DevolveOkComAVo()
        {
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso(), new FakeLogService());

            IActionResult resultado = await controller.Login(new GoogleLoginVO { Credencial = "credencial-teste" });

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnGoogleLoginVO corpo = Assert.IsType<ReturnGoogleLoginVO>(ok.Value);
            Assert.Equal(SituacaoLoginGoogle.CADASTRO_NECESSARIO, corpo.Situacao);
        }

        [Fact]
        public async Task Vinculo_DevolveOkComOLoginVO()
        {
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso(), new FakeLogService());

            IActionResult resultado = await controller.Vinculo(new GoogleVinculoVO { Credencial = "credencial-teste", Cpf = "52998224725", Senha = "SenhaTeste#2026" });

            OkObjectResult ok = Assert.IsType<OkObjectResult>(resultado);
            ReturnLoginVO corpo = Assert.IsType<ReturnLoginVO>(ok.Value);
            Assert.Equal(PerfilUsuario.CLIENTE, corpo.Perfil);
            Assert.Equal(7, corpo.IdUsuario);
        }

        [Fact]
        public async Task Login_ExcecoesDeNegocioEDeIntegracao_SaoRepassadasSemLog()
        {
            FakeLogService log = new FakeLogService();
            Exception[] excecoes =
            {
                new UnauthorizedException(ConstantsMessage.ErroGoogleCredencialInvalida),
                new BusinessException(ConstantsMessage.ErroGoogleContaAmbigua),
                new IntegrationException("GOOGLE", MensagemIntegracao)
            };

            foreach (Exception excecao in excecoes)
            {
                GoogleAuthController controller = CriarController(new ServicoGoogleFalso { ExcecaoAoLogar = excecao }, log);

                Exception relancada = await Assert.ThrowsAnyAsync<Exception>(() => controller.Login(new GoogleLoginVO { Credencial = "credencial-teste" }));

                Assert.Same(excecao, relancada);
            }

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Vinculo_ExcecoesDeNegocioEDeIntegracao_SaoRepassadasSemLog()
        {
            FakeLogService log = new FakeLogService();
            Exception[] excecoes =
            {
                new UnauthorizedException(ConstantsMessage.ErroCredenciaisInvalidas),
                new BusinessException(ConstantsMessage.ErroGoogleClienteJaVinculado),
                new IntegrationException("GOOGLE", MensagemIntegracao)
            };

            foreach (Exception excecao in excecoes)
            {
                GoogleAuthController controller = CriarController(new ServicoGoogleFalso { ExcecaoAoVincular = excecao }, log);

                Exception relancada = await Assert.ThrowsAnyAsync<Exception>(() => controller.Vinculo(new GoogleVinculoVO { Credencial = "credencial-teste" }));

                Assert.Same(excecao, relancada);
            }

            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Login_FalhaInesperada_RegistraNoLogERepassa()
        {
            FakeLogService log = new FakeLogService();
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso { ExcecaoAoLogar = new InvalidOperationException(MensagemFalhaInesperada) }, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Login(new GoogleLoginVO { Credencial = "credencial-teste" }));

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLoginGoogle));
        }

        [Fact]
        public async Task Vinculo_FalhaInesperada_RegistraNoLogERepassa()
        {
            FakeLogService log = new FakeLogService();
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso { ExcecaoAoVincular = new InvalidOperationException(MensagemFalhaInesperada) }, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Vinculo(new GoogleVinculoVO { Credencial = "credencial-teste" }));

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroVincularGoogle));
        }

        [Fact]
        public async Task Configuracao_FalhaInesperada_RegistraNoLogERepassa()
        {
            FakeLogService log = new FakeLogService();
            GoogleAuthController controller = CriarController(new ServicoGoogleFalso { ExcecaoAoConfigurar = new InvalidOperationException(MensagemFalhaInesperada) }, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ObterConfiguracao());

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroConfiguracaoGoogle));
        }
    }
}
