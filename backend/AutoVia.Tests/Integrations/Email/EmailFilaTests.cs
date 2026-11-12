using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Integrations.Helpers.Email;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Email;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Tests.Integrations.Email
{
    public class EmailFilaTests
    {
        private const string Destinatario = "maria.sensivel@exemplo.com";
        private const string Assunto = "Redefinicao de senha SEGREDO";
        private const string CorpoTexto = "Link secreto ABC-TOKEN-987";
        private const int EsperaMaximaMs = 5000;

        /// <summary>Gateway de teste: conta as chamadas e falha em todas, com excecao de mensagem neutra.</summary>
        private sealed class GatewayQueFalha : IEmailGateway
        {
            private readonly int _esperadas;
            private int _chamadas;

            public GatewayQueFalha(int esperadas)
            {
                _esperadas = esperadas;
            }

            public TaskCompletionSource<bool> Concluido { get; } = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public List<EmailMensagem> Recebidas { get; } = new List<EmailMensagem>();

            public Task EnviarAsync(EmailMensagem mensagem, CancellationToken cancellationToken = default)
            {
                lock (Recebidas)
                {
                    Recebidas.Add(mensagem);
                }

                if (Interlocked.Increment(ref _chamadas) == _esperadas)
                {
                    Concluido.TrySetResult(true);
                }

                return Task.FromException(new InvalidOperationException("falha simulada do provedor"));
            }
        }

        private static EmailMensagem CriarMensagemSensivel()
        {
            return new EmailMensagem
            {
                DestinatarioEmail = Destinatario,
                DestinatarioNome = "Maria Sensivel",
                Assunto = Assunto,
                CorpoTexto = CorpoTexto,
                Etiqueta = "recuperacao-senha"
            };
        }

        [Fact]
        public void Enfileirar_FilaCheia_DevolveFalseSemLancarERegistraLog()
        {
            FakeLogService log = new FakeLogService();
            EmailFila fila = new EmailFila(log);

            for (int i = 0; i < ConstantsEmail.CapacidadeFila; i++)
            {
                Assert.True(fila.Enfileirar(CriarMensagemSensivel()));
            }

            bool enfileirouComFilaCheia = fila.Enfileirar(CriarMensagemSensivel());

            Assert.False(enfileirouComFilaCheia);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroEmailFilaCheia));
        }

        [Fact]
        public void Enfileirar_MensagemNula_DevolveFalseSemLancar()
        {
            EmailFila fila = new EmailFila(new FakeLogService());

            bool enfileirou = fila.Enfileirar(null!);

            Assert.False(enfileirou);
        }

        [Fact]
        public async Task Leitor_RecebeAMensagemEnfileirada()
        {
            EmailFila fila = new EmailFila(new FakeLogService());
            EmailMensagem mensagem = CriarMensagemSensivel();

            Assert.True(fila.Enfileirar(mensagem));
            EmailMensagem recebida = await fila.Leitor.ReadAsync();

            Assert.Same(mensagem, recebida);
        }

        [Fact]
        public async Task ServicoHospedado_FalhaDoGateway_NaoDerrubaOServicoENaoVazaDadoPessoal()
        {
            FakeLogService log = new FakeLogService();
            EmailFila fila = new EmailFila(log);
            GatewayQueFalha gateway = new GatewayQueFalha(2);
            using ServiceProvider provedor = new ServiceCollection().AddScoped<IEmailGateway>(_ => gateway).BuildServiceProvider();
            EmailEnvioHostedService servico = new EmailEnvioHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);

            await servico.StartAsync(CancellationToken.None);
            Assert.True(fila.Enfileirar(CriarMensagemSensivel()));
            Assert.True(fila.Enfileirar(CriarMensagemSensivel()));

            Task concluidoOuEsgotado = await Task.WhenAny(gateway.Concluido.Task, Task.Delay(EsperaMaximaMs));
            await servico.StopAsync(CancellationToken.None);

            // A segunda mensagem so chega ao gateway se o servico sobreviveu a primeira falha.
            Assert.Same(gateway.Concluido.Task, concluidoOuEsgotado);
            Assert.Equal(2, gateway.Recebidas.Count);
            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroEmailEnviar));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(Destinatario));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(Assunto));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("ABC-TOKEN-987"));
        }

        [Fact]
        public async Task ServicoHospedado_EntregaAoGatewayAMesmaMensagemEnfileirada()
        {
            FakeLogService log = new FakeLogService();
            EmailFila fila = new EmailFila(log);
            GatewayQueFalha gateway = new GatewayQueFalha(1);
            using ServiceProvider provedor = new ServiceCollection().AddScoped<IEmailGateway>(_ => gateway).BuildServiceProvider();
            EmailEnvioHostedService servico = new EmailEnvioHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);
            EmailMensagem mensagem = CriarMensagemSensivel();

            await servico.StartAsync(CancellationToken.None);
            Assert.True(fila.Enfileirar(mensagem));

            Task concluidoOuEsgotado = await Task.WhenAny(gateway.Concluido.Task, Task.Delay(EsperaMaximaMs));
            await servico.StopAsync(CancellationToken.None);

            Assert.Same(gateway.Concluido.Task, concluidoOuEsgotado);
            Assert.Single(gateway.Recebidas);
            Assert.Same(mensagem, gateway.Recebidas[0]);
        }
    }
}
