using AutoVia.Api.Hosting;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using static AutoVia.Tests.Autenticacao.RecuperacaoAmbiente;

namespace AutoVia.Tests.Autenticacao
{
    /// <summary>
    /// Servico hospedado da recuperacao (RecuperacaoSenhaHostedService) com DI real e fakes. Prova que o pedido da
    /// fila chega ao processador e ao e-mail, que a falha de um pedido nao derruba o leitor (o seguinte e processado),
    /// que cada pedido ganha um escopo proprio, que nenhum log leva o CPF e que o encerramento e limpo.
    /// As esperas usam condicao com tempo limite curto, nunca tempo fixo.
    /// </summary>
    public class RecuperacaoSenhaHostedServiceTests
    {
        private const int TempoLimiteEsperaMs = 5000;
        private const int IntervaloEsperaMs = 10;
        private const int TempoLimiteParadaMs = 5000;
        private const string MensagemTempoEsgotado = "A condição esperada não aconteceu no tempo limite do teste.";

        /// <summary>Espera uma condicao verdadeira, consultando de tempos em tempos, ate o tempo limite do teste.</summary>
        private static async Task AguardarAsync(Func<bool> condicao)
        {
            Stopwatch cronometro = Stopwatch.StartNew();

            while (!condicao())
            {
                if (cronometro.ElapsedMilliseconds > TempoLimiteEsperaMs)
                {
                    throw new TimeoutException(MensagemTempoEsgotado);
                }

                await Task.Delay(IntervaloEsperaMs);
            }
        }

        private static Cliente ClienteElegivel(string cpf, string email)
        {
            return new Cliente
            {
                Nome = NomeCliente,
                Cpf = cpf,
                Email = email,
                SenhaHash = new FakeSenhaHasher().Hash(SenhaAntiga),
                StatusCadastro = StatusCadastroCliente.APROVADO
            };
        }

        private static RecuperacaoSenhaOptions OpcoesDeTeste()
        {
            return new RecuperacaoSenhaOptions
            {
                UrlPublicaFront = UrlFront,
                ValidadeMinutos = ValidadeMinutos,
                LimitePorHora = LimitePorHora,
                TempoMinimoRespostaMs = TempoMinimoZero
            };
        }

        /// <summary>Registro compartilhado entre o servico hospedado e o processador espiao: o que foi processado, e em qual instancia.</summary>
        private sealed class RegistroProcessamento
        {
            private readonly object _trava = new object();
            private readonly List<string> _cpfs = new List<string>();
            private readonly List<Guid> _instancias = new List<Guid>();

            /// <summary>CPF cujo processamento lanca excecao (para provar que o leitor segue).</summary>
            public string? CpfComFalha { get; set; }

            public int Total
            {
                get
                {
                    lock (_trava)
                    {
                        return _cpfs.Count;
                    }
                }
            }

            public List<string> Cpfs
            {
                get
                {
                    lock (_trava)
                    {
                        return new List<string>(_cpfs);
                    }
                }
            }

            public List<Guid> Instancias
            {
                get
                {
                    lock (_trava)
                    {
                        return new List<Guid>(_instancias);
                    }
                }
            }

            public void Registrar(Guid instancia, string cpf)
            {
                lock (_trava)
                {
                    _instancias.Add(instancia);
                    _cpfs.Add(cpf);
                }
            }
        }

        /// <summary>Processador de teste (Scoped): registra cada pedido e lanca quando o CPF e o de falha. A mensagem nao leva o CPF.</summary>
        private sealed class ProcessadorEspiao : IRecuperacaoSenhaProcessador
        {
            private readonly RegistroProcessamento _registro;
            private readonly Guid _instancia = Guid.NewGuid();

            public ProcessadorEspiao(RegistroProcessamento registro)
            {
                _registro = registro;
            }

            public Task ProcessarAsync(PedidoRecuperacaoSenha pedido, CancellationToken cancellationToken)
            {
                _registro.Registrar(_instancia, pedido.Cpf);

                if (string.Equals(pedido.Cpf, _registro.CpfComFalha, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(MensagemFalhaSimulada);
                }

                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task PedidoEnfileirado_EhProcessadoNoEscopoEOEmailSai()
        {
            FakeAutenticacaoRepository autenticacao = new FakeAutenticacaoRepository();
            autenticacao.AdicionarCliente(ClienteElegivel(CpfCliente, EmailCliente));
            FakeRedefinicaoSenhaRepository redefinicoes = new FakeRedefinicaoSenhaRepository(autenticacao);
            FakeEmailFila emailFila = new FakeEmailFila();
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaOptions opcoes = OpcoesDeTeste();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            ServiceCollection services = new ServiceCollection();
            services.AddScoped<IRecuperacaoSenhaProcessador>(provedor => new RecuperacaoSenhaProcessador(autenticacao, redefinicoes, emailFila, opcoes, log));
            using ServiceProvider provedor = services.BuildServiceProvider();

            RecuperacaoSenhaHostedService servico = new RecuperacaoSenhaHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);
            await servico.StartAsync(CancellationToken.None);

            fila.Enfileirar(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfCliente));

            await AguardarAsync(() => emailFila.Mensagens.Count == 1);

            Assert.Equal(EmailCliente, emailFila.Mensagens[0].DestinatarioEmail);
            Assert.Single(redefinicoes.Redefinicoes);

            await servico.StopAsync(CancellationToken.None);
        }

        /// <summary>Falha de um pedido: o erro vai para o log sem o CPF, e o pedido seguinte da fila ainda e processado.</summary>
        [Fact]
        public async Task ExcecaoDoProcessador_NaoDerrubaOLeitor_OPedidoSeguinteEProcessado()
        {
            RegistroProcessamento registro = new RegistroProcessamento { CpfComFalha = CpfCliente };
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(registro);
            services.AddScoped<IRecuperacaoSenhaProcessador, ProcessadorEspiao>();
            using ServiceProvider provedor = services.BuildServiceProvider();

            RecuperacaoSenhaHostedService servico = new RecuperacaoSenhaHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);
            await servico.StartAsync(CancellationToken.None);

            fila.Enfileirar(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfCliente));
            fila.Enfileirar(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfOutro));

            await AguardarAsync(() => registro.Total == 2);

            Assert.Equal(new List<string> { CpfCliente, CpfOutro }, registro.Cpfs);
            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroProcessarPedidoRecuperacaoSenha));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains(CpfCliente, StringComparison.Ordinal));

            await servico.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task CadaPedidoRecebeUmEscopoDeDIProprio()
        {
            RegistroProcessamento registro = new RegistroProcessamento();
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(registro);
            services.AddScoped<IRecuperacaoSenhaProcessador, ProcessadorEspiao>();
            using ServiceProvider provedor = services.BuildServiceProvider();

            RecuperacaoSenhaHostedService servico = new RecuperacaoSenhaHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);
            await servico.StartAsync(CancellationToken.None);

            fila.Enfileirar(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfCliente));
            fila.Enfileirar(new PedidoRecuperacaoSenha(PerfilUsuario.CLIENTE, CpfOutro));

            await AguardarAsync(() => registro.Total == 2);

            List<Guid> instancias = registro.Instancias;

            Assert.Equal(2, instancias.Count);
            Assert.NotEqual(instancias[0], instancias[1]);

            await servico.StopAsync(CancellationToken.None);
        }

        /// <summary>Encerramento limpo: a parada termina dentro do tempo limite, sem excecao e sem erro no log.</summary>
        [Fact]
        public async Task Encerramento_TerminaLimpoAoSerCancelado()
        {
            RegistroProcessamento registro = new RegistroProcessamento();
            FakeLogService log = new FakeLogService();
            RecuperacaoSenhaFila fila = new RecuperacaoSenhaFila(log);

            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(registro);
            services.AddScoped<IRecuperacaoSenhaProcessador, ProcessadorEspiao>();
            using ServiceProvider provedor = services.BuildServiceProvider();

            RecuperacaoSenhaHostedService servico = new RecuperacaoSenhaHostedService(fila, provedor.GetRequiredService<IServiceScopeFactory>(), log);
            await servico.StartAsync(CancellationToken.None);

            using CancellationTokenSource limite = new CancellationTokenSource(TempoLimiteParadaMs);
            await servico.StopAsync(limite.Token);

            Assert.Empty(log.Entries);
        }
    }
}
