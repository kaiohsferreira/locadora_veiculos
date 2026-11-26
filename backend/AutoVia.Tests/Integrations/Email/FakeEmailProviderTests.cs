using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Services.Email;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AutoVia.Tests.Integrations.Email
{
    public class FakeEmailProviderTests
    {
        private const string DestinatarioEmail = "maria@exemplo.com";
        private const string RemetenteEmail = "nao-responda@autovia.com.br";
        private const string RemetenteNome = "AutoVia";

        /// <summary>IHostEnvironment de teste: ambiente e pasta de conteudo escolhidos pelo teste.</summary>
        private sealed class AmbienteFalso : IHostEnvironment
        {
            public AmbienteFalso(string nomeAmbiente, string raizConteudo)
            {
                EnvironmentName = nomeAmbiente;
                ContentRootPath = raizConteudo;
                ContentRootFileProvider = new NullFileProvider();
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "AutoVia.Tests";

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
        }

        private static ConfiguracaoIntegracao CriarConfiguracao(AmbienteIntegracao ambiente)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Email,
                "E-mail",
                ambiente,
                "https://sandbox.email.exemplo.com.br",
                30000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        private static EmailMensagem CriarMensagem()
        {
            return new EmailMensagem
            {
                DestinatarioEmail = DestinatarioEmail,
                DestinatarioNome = "Maria",
                Assunto = "Redefinicao da senha",
                CorpoTexto = "Texto do corpo",
                CorpoHtml = "<p>Texto do corpo</p>",
                Etiqueta = "recuperacao-senha"
            };
        }

        private static string CriarPastaTemporaria()
        {
            string raiz = Path.Combine(Path.GetTempPath(), "autovia-email-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(raiz);
            return raiz;
        }

        private static string PastaCaixaFake(string raiz)
        {
            return Path.Combine(raiz, ConstantsEmail.PastaAppData, ConstantsEmail.PastaEmailFake);
        }

        [Fact]
        public void Nome_EhFake()
        {
            FakeEmailProvider provider = new FakeEmailProvider(new EmailFakeStore(), new AmbienteFalso(Environments.Development, Path.GetTempPath()), new FakeLogService());

            Assert.Equal(ConstantsEmail.ProvedorFake, provider.Nome);
        }

        [Fact]
        public async Task EnviarAsync_EmDevelopmentComSandbox_GravaArquivoEEnvelopeNoStore()
        {
            string raiz = CriarPastaTemporaria();
            try
            {
                EmailFakeStore store = new EmailFakeStore();
                FakeEmailProvider provider = new FakeEmailProvider(store, new AmbienteFalso(Environments.Development, raiz), new FakeLogService());

                await provider.EnviarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarMensagem(), RemetenteEmail, RemetenteNome);

                string[] arquivos = Directory.GetFiles(PastaCaixaFake(raiz), "*" + ConstantsEmail.ExtensaoArquivoFake);
                Assert.Single(arquivos);
                string conteudo = File.ReadAllText(arquivos[0]);
                Assert.Contains(DestinatarioEmail, conteudo);
                Assert.Contains("Redefinicao da senha", conteudo);

                Assert.Equal(1, store.Quantidade);
                EmailFakeEnviado enviado = store.Enviados()[0];
                Assert.Equal(DestinatarioEmail, enviado.DestinatarioEmail);
                Assert.Equal(RemetenteEmail, enviado.RemetenteEmail);
                Assert.Equal(Path.GetFileNameWithoutExtension(arquivos[0]), enviado.Identificador);
            }
            finally
            {
                Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public async Task EnviarAsync_NomeDoArquivoSemDadoPessoal()
        {
            string raiz = CriarPastaTemporaria();
            try
            {
                FakeEmailProvider provider = new FakeEmailProvider(new EmailFakeStore(), new AmbienteFalso(Environments.Development, raiz), new FakeLogService());

                await provider.EnviarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarMensagem(), RemetenteEmail, RemetenteNome);

                string nome = Path.GetFileName(Directory.GetFiles(PastaCaixaFake(raiz))[0]);
                Assert.DoesNotContain("maria", nome, StringComparison.OrdinalIgnoreCase);
                Assert.Matches(@"^\d{17}-[0-9a-f]{32}\.txt$", nome);
            }
            finally
            {
                Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public async Task EnviarAsync_FalhaAoGravar_RegistraTipoSemEnderecoOuConteudo()
        {
            string raiz = CriarPastaTemporaria();
            try
            {
                string raizBloqueada = Path.Combine(raiz, DestinatarioEmail);
                File.WriteAllText(raizBloqueada, string.Empty);

                FakeLogService log = new FakeLogService();
                EmailFakeStore store = new EmailFakeStore();
                FakeEmailProvider provider = new FakeEmailProvider(store, new AmbienteFalso(Environments.Development, raizBloqueada), log);
                EmailMensagem mensagem = CriarMensagem();

                IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() =>
                    provider.EnviarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), mensagem, RemetenteEmail, RemetenteNome));

                Exception? causa = excecao.InnerException;
                Assert.NotNull(causa);
                Assert.Contains(DestinatarioEmail, causa.Message, StringComparison.OrdinalIgnoreCase);
                string entrada = Assert.Single(log.Entries);
                Assert.Contains(ConstantsMessage.ErroEmailGravarCaixaFake, entrada, StringComparison.Ordinal);
                Assert.Contains(string.Format(ConstantsMessage.LogEmailTipoFalha, causa.GetType().Name), entrada, StringComparison.Ordinal);
                Assert.DoesNotContain(DestinatarioEmail, entrada, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(mensagem.Assunto, entrada, StringComparison.Ordinal);
                Assert.DoesNotContain(mensagem.CorpoTexto, entrada, StringComparison.Ordinal);
                Assert.Equal(0, store.Quantidade);
            }
            finally
            {
                Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public async Task EnviarAsync_ForaDeDevelopmentMesmoComSandbox_RecusaSemGravar()
        {
            string raiz = CriarPastaTemporaria();
            try
            {
                EmailFakeStore store = new EmailFakeStore();
                FakeEmailProvider provider = new FakeEmailProvider(store, new AmbienteFalso(Environments.Production, raiz), new FakeLogService());

                IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.EnviarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarMensagem(), RemetenteEmail, RemetenteNome));

                Assert.Equal(ConstantsMessage.ErroEmailFakeForaDoDesenvolvimento, excecao.Message);
                Assert.False(Directory.Exists(PastaCaixaFake(raiz)));
                Assert.Equal(0, store.Quantidade);
            }
            finally
            {
                Directory.Delete(raiz, true);
            }
        }

        [Fact]
        public async Task EnviarAsync_EmDevelopmentComAmbienteDiferenteDeSandbox_RecusaSemGravar()
        {
            string raiz = CriarPastaTemporaria();
            try
            {
                EmailFakeStore store = new EmailFakeStore();
                FakeEmailProvider provider = new FakeEmailProvider(store, new AmbienteFalso(Environments.Development, raiz), new FakeLogService());

                IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.EnviarAsync(CriarConfiguracao(AmbienteIntegracao.PRODUCAO), CriarMensagem(), RemetenteEmail, RemetenteNome));

                Assert.Equal(ConstantsMessage.ErroEmailFakeForaDoDesenvolvimento, excecao.Message);
                Assert.False(Directory.Exists(PastaCaixaFake(raiz)));
                Assert.Equal(0, store.Quantidade);
            }
            finally
            {
                Directory.Delete(raiz, true);
            }
        }
    }
}
