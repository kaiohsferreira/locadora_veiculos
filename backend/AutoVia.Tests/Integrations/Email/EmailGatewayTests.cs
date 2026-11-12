using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Email;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Email
{
    public class EmailGatewayTests
    {
        private const string Destinatario = "maria.sensivel@exemplo.com";
        private const string DestinatarioNome = "Maria Sensivel";
        private const string Assunto = "Redefinicao de senha SEGREDO-123";
        private const string CorpoTexto = "Link secreto https://exemplo.com/redefinir#token=ABC-TOKEN-987";
        private const string CorpoHtml = "<p>Link secreto ABC-TOKEN-987</p>";
        private const string Etiqueta = "recuperacao-senha";
        private const string RemetenteEmail = "nao-responda@autovia.com.br";
        private const string RemetenteNome = "AutoVia";
        private const string NomeProvedorTeste = "PROVEDOR-TESTE";

        // ------------------------------------------------------------------
        // Escopo de teste: provedor roteirizado (sem rede) para observar a chamada.
        // ------------------------------------------------------------------

        private sealed class ProvedorRoteirizado : IEmailProvider
        {
            public ProvedorRoteirizado(string nome)
            {
                Nome = nome;
            }

            public string Nome { get; }

            public int Chamadas { get; private set; }

            public string? RemetenteEmailRecebido { get; private set; }

            public string? RemetenteNomeRecebido { get; private set; }

            public Exception? Excecao { get; set; }

            public Task EnviarAsync(ConfiguracaoIntegracao configuracao, EmailMensagem mensagem, string remetenteEmail, string remetenteNome, CancellationToken cancellationToken = default)
            {
                Chamadas++;
                RemetenteEmailRecebido = remetenteEmail;
                RemetenteNomeRecebido = remetenteNome;

                if (Excecao != null)
                {
                    return Task.FromException(Excecao);
                }

                return Task.CompletedTask;
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Integration CriarIntegracao(string? provedor, AmbienteIntegracao ambiente, bool ativo = true, bool comRemetente = true)
        {
            Integration integration = new Integration
            {
                IdIntegration = 2,
                Codigo = ConstantsIntegration.Email,
                Nome = "E-mail",
                Ambiente = ambiente,
                BaseUrl = "https://sandbox.email.exemplo.com.br",
                TimeoutMs = 30000,
                Ativo = ativo
            };

            if (provedor != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsEmail.ChaveProvedor, Valor = provedor, Protegido = false });
            }

            if (comRemetente)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsEmail.ChaveRemetenteEmail, Valor = RemetenteEmail, Protegido = false });
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsEmail.ChaveRemetenteNome, Valor = RemetenteNome, Protegido = false });
            }

            return integration;
        }

        private static FakeIntegrationRepository CriarRepositorio(Integration integration)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integration);
            return repositorio;
        }

        private static EmailGateway CriarGateway(FakeIntegrationRepository repositorio, ILogService logService, params IEmailProvider[] provedores)
        {
            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), logService);
            return new EmailGateway(resolver, provedores, logService);
        }

        private static EmailMensagem CriarMensagem()
        {
            return new EmailMensagem
            {
                DestinatarioEmail = Destinatario,
                DestinatarioNome = DestinatarioNome,
                Assunto = Assunto,
                CorpoTexto = CorpoTexto,
                CorpoHtml = CorpoHtml,
                Etiqueta = Etiqueta
            };
        }

        /// <summary>Confere que nenhuma entrada do log traz destinatario, assunto ou corpo.</summary>
        private static void AssertLogSemDadoPessoal(FakeLogService log)
        {
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(Destinatario));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(DestinatarioNome));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(Assunto));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("ABC-TOKEN-987"));
        }

        // ------------------------------------------------------------------
        // Provedor e integracao
        // ------------------------------------------------------------------

        [Fact]
        public async Task EnviarAsync_ProvedorDesconhecido_LancaProvedorNaoSuportado()
        {
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao("OUTRO-CORREIO", AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(NomeProvedorTeste));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(ConstantsMessage.ErroEmailProvedorNaoSuportado, excecao.Message);
            Assert.Equal(ConstantsEmail.ChaveProvedor, excecao.Chave);
        }

        [Fact]
        public async Task EnviarAsync_IntegracaoInativa_RepassaAExcecaoDoResolver()
        {
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX, ativo: false)), new FakeLogService(), new ProvedorRoteirizado(NomeProvedorTeste));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(string.Format(ConstantsMessageIntegration.ErroIntegracaoInativa, ConstantsIntegration.Email), excecao.Message);
        }

        [Fact]
        public async Task EnviarAsync_IntegracaoInexistente_RepassaAExcecaoDoResolver()
        {
            EmailGateway gateway = CriarGateway(new FakeIntegrationRepository(), new FakeLogService(), new ProvedorRoteirizado(NomeProvedorTeste));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(string.Format(ConstantsMessageIntegration.ErroIntegracaoNaoEncontrada, ConstantsIntegration.Email), excecao.Message);
        }

        [Fact]
        public async Task EnviarAsync_FakeEmAmbienteDiferenteDeSandbox_LancaFakeForaDoDesenvolvimento()
        {
            ProvedorRoteirizado fake = new ProvedorRoteirizado(ConstantsEmail.ProvedorFake);
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsEmail.ProvedorFake, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), fake);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(ConstantsMessage.ErroEmailFakeForaDoDesenvolvimento, excecao.Message);
            Assert.Equal(0, fake.Chamadas);
        }

        [Fact]
        public async Task EnviarAsync_RemetenteAusente_LancaIntegrationExceptionComAChave()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(NomeProvedorTeste);
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX, comRemetente: false)), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(ConstantsEmail.ChaveRemetenteEmail, excecao.Chave);
            Assert.Equal(0, provedor.Chamadas);
        }

        // ------------------------------------------------------------------
        // Validacao da mensagem (antes do provedor)
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("", "Assunto", "Corpo")]
        [InlineData("sem-arroba", "Assunto", "Corpo")]
        [InlineData("maria@exemplo.com", "", "Corpo")]
        [InlineData("maria@exemplo.com", "Linha\r\nBcc: alguem@exemplo.com", "Corpo")]
        [InlineData("maria@exemplo.com", "Assunto", "   ")]
        public async Task EnviarAsync_MensagemInvalida_LancaBusinessExceptionSemChamarOProvedor(string destinatario, string assunto, string corpo)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(NomeProvedorTeste);
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX)), new FakeLogService(), provedor);
            EmailMensagem mensagem = CriarMensagem();
            mensagem.DestinatarioEmail = destinatario;
            mensagem.Assunto = assunto;
            mensagem.CorpoTexto = corpo;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.EnviarAsync(mensagem));

            Assert.Equal(ConstantsMessage.ErroEmailMensagemInvalida, excecao.Message);
            Assert.Equal(0, provedor.Chamadas);
        }

        [Fact]
        public async Task EnviarAsync_MensagemNula_LancaArgumentNullException()
        {
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX)), new FakeLogService(), new ProvedorRoteirizado(NomeProvedorTeste));

            await Assert.ThrowsAsync<ArgumentNullException>(() => gateway.EnviarAsync(null!));
        }

        // ------------------------------------------------------------------
        // Envio e log
        // ------------------------------------------------------------------

        [Fact]
        public async Task EnviarAsync_Sucesso_RepassaRemetenteDaIntegracao()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(NomeProvedorTeste);
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX)), new FakeLogService(), provedor);

            await gateway.EnviarAsync(CriarMensagem());

            Assert.Equal(1, provedor.Chamadas);
            Assert.Equal(RemetenteEmail, provedor.RemetenteEmailRecebido);
            Assert.Equal(RemetenteNome, provedor.RemetenteNomeRecebido);
        }

        [Fact]
        public async Task EnviarAsync_Sucesso_LogaSoProvedorEtiquetaEIdentificador()
        {
            FakeLogService log = new FakeLogService();
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX)), log, new ProvedorRoteirizado(NomeProvedorTeste));

            await gateway.EnviarAsync(CriarMensagem());

            Assert.Contains(log.Entries, entrada => entrada.Contains(NomeProvedorTeste) && entrada.Contains(Etiqueta));
            AssertLogSemDadoPessoal(log);
        }

        [Fact]
        public async Task EnviarAsync_FalhaInesperadaDoProvedor_LancaIntegrationExceptionComInner_SemDadoPessoalNoLog()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(NomeProvedorTeste);
            provedor.Excecao = new InvalidOperationException("falha interna do provedor");
            FakeLogService log = new FakeLogService();
            EmailGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(NomeProvedorTeste, AmbienteIntegracao.SANDBOX)), log, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.EnviarAsync(CriarMensagem()));

            Assert.Equal(ConstantsMessage.ErroEmailEnviar, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroEmailEnviar));
            AssertLogSemDadoPessoal(log);
        }
    }
}
