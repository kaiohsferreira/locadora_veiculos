using AutoVia.Api.Controllers;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Autenticacao;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.AspNetCore.Mvc;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Controller da recuperacao de senha sem HTTP: devolve 202 com a mensagem neutra e 204 na redefinicao,
    /// repassa BusinessException para o middleware (409), e registra no log qualquer falha inesperada.
    /// </summary>
    public class RecuperacaoSenhaControllerTests
    {
        private const string MensagemFalhaInesperada = "Falha inesperada simulada.";

        /// <summary>Servico de recuperacao falso: cada metodo devolve ou lanca o que o teste configurar.</summary>
        private sealed class ServicoFalso : IRecuperacaoSenhaService
        {
            public Exception? ExcecaoAoSolicitar { get; set; }

            public Exception? ExcecaoAoRedefinir { get; set; }

            public int Solicitacoes { get; private set; }

            public int Redefinicoes { get; private set; }

            public Task<ReturnRecuperacaoSolicitadaVO> SolicitarAsync(SolicitarRecuperacaoSenhaVO vo)
            {
                Solicitacoes++;

                if (ExcecaoAoSolicitar != null)
                {
                    throw ExcecaoAoSolicitar;
                }

                ReturnRecuperacaoSolicitadaVO retorno = new ReturnRecuperacaoSolicitadaVO { Mensagem = ConstantsMessage.InfoRecuperacaoSolicitada };

                return Task.FromResult(retorno);
            }

            public Task RedefinirAsync(RedefinirSenhaVO vo)
            {
                Redefinicoes++;

                if (ExcecaoAoRedefinir != null)
                {
                    throw ExcecaoAoRedefinir;
                }

                return Task.CompletedTask;
            }
        }

        private static SolicitarRecuperacaoSenhaVO Pedido()
        {
            return new SolicitarRecuperacaoSenhaVO { Perfil = PerfilUsuario.CLIENTE, Cpf = "52998224725" };
        }

        private static RedefinirSenhaVO Redefinicao()
        {
            return new RedefinirSenhaVO { Token = "token-de-teste", NovaSenha = "Senha1234", ConfirmacaoSenha = "Senha1234" };
        }

        [Fact]
        public async Task Solicitar_DevolveAccepted_ComAMensagemNeutra()
        {
            ServicoFalso servico = new ServicoFalso();
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaController controller = new RecuperacaoSenhaController(servico, log);

            IActionResult resultado = await controller.SolicitarRecuperacaoSenha(Pedido());

            AcceptedResult aceito = Assert.IsType<AcceptedResult>(resultado);
            ReturnRecuperacaoSolicitadaVO corpo = Assert.IsType<ReturnRecuperacaoSolicitadaVO>(aceito.Value);
            Assert.Equal(ConstantsMessage.InfoRecuperacaoSolicitada, corpo.Mensagem);
            Assert.Equal(1, servico.Solicitacoes);
        }

        [Fact]
        public async Task Redefinir_DevolveNoContent()
        {
            ServicoFalso servico = new ServicoFalso();
            RecuperacaoSenhaController controller = new RecuperacaoSenhaController(servico, new FakeLogService());

            IActionResult resultado = await controller.RedefinirSenha(Redefinicao());

            Assert.IsType<NoContentResult>(resultado);
            Assert.Equal(1, servico.Redefinicoes);
        }

        [Fact]
        public async Task Redefinir_BusinessException_EhRepassadaParaOMiddleware()
        {
            ServicoFalso servico = new ServicoFalso { ExcecaoAoRedefinir = new BusinessException(ConstantsMessage.ErroRedefinicaoLinkInvalido) };
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaController controller = new RecuperacaoSenhaController(servico, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => controller.RedefinirSenha(Redefinicao()));

            Assert.Equal(ConstantsMessage.ErroRedefinicaoLinkInvalido, excecao.Message);
            Assert.Empty(log.Entries);
        }

        [Fact]
        public async Task Solicitar_FalhaInesperada_RegistraNoLogERepassa()
        {
            ServicoFalso servico = new ServicoFalso { ExcecaoAoSolicitar = new InvalidOperationException(MensagemFalhaInesperada) };
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaController controller = new RecuperacaoSenhaController(servico, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.SolicitarRecuperacaoSenha(Pedido()));

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroSolicitarRecuperacaoSenha));
        }

        [Fact]
        public async Task Redefinir_FalhaInesperada_RegistraNoLogERepassa()
        {
            ServicoFalso servico = new ServicoFalso { ExcecaoAoRedefinir = new InvalidOperationException(MensagemFalhaInesperada) };
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaController controller = new RecuperacaoSenhaController(servico, log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RedefinirSenha(Redefinicao()));

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroRedefinirSenha));
        }
    }
}
