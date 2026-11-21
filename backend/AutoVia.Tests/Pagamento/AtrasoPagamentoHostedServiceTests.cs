using AutoVia.Api.Hosting;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Servico hospedado da rotina de atraso (contrato pagamento-atraso v1) com DI real e fakes. Prova: roda uma vez ao subir,
    /// antes do primeiro intervalo; a falha de um ciclo vai para o log e o ciclo seguinte roda; o desligamento sai sem erro; e
    /// a configuracao desligada nao chama a rotina. As esperas usam condicao com tempo limite, nunca tempo fixo.
    /// </summary>
    public class AtrasoPagamentoHostedServiceTests
    {
        private const int TempoLimiteEsperaMs = 5000;
        private const int IntervaloEsperaMs = 10;
        private const int IntervaloCurtoMs = 10;
        private const int IntervaloLongoHoras = 1;
        private const int PrimeiraChamada = 1;
        private const int SegundaChamada = 2;
        private const string MensagemTempoEsgotado = "A condição esperada não aconteceu no tempo limite do teste.";

        /// <summary>Rotina falsa: conta as chamadas e pode falhar so na primeira.</summary>
        private sealed class RotinaFalsa : IPagamentoAtrasoRotinaService
        {
            private int _chamadas;

            public bool FalharNaPrimeira { get; init; }

            public int Chamadas => Volatile.Read(ref _chamadas);

            public Task<int> MarcarAtrasosTodasLocadorasAsync()
            {
                int numero = Interlocked.Increment(ref _chamadas);

                if (FalharNaPrimeira && numero == PrimeiraChamada)
                {
                    throw new InvalidOperationException("Falha simulada da rodada.");
                }

                return Task.FromResult(0);
            }
        }

        /// <summary>Servico hospedado com intervalo trocado, para os testes nao esperarem uma hora.</summary>
        private sealed class HospedadoComIntervalo : AtrasoPagamentoHostedService
        {
            private readonly TimeSpan _intervalo;

            public HospedadoComIntervalo(TimeSpan intervalo, IServiceScopeFactory escopos, ILogService log, IConfiguration configuracao)
                : base(escopos, log, configuracao)
            {
                _intervalo = intervalo;
            }

            protected override TimeSpan IntervaloRodada => _intervalo;
        }

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

        private static IServiceScopeFactory CriarEscopos(IPagamentoAtrasoRotinaService rotina)
        {
            ServiceCollection servicos = new ServiceCollection();
            servicos.AddScoped<IPagamentoAtrasoRotinaService>(provedor => rotina);

            ServiceProvider provedorRaiz = servicos.BuildServiceProvider();

            return provedorRaiz.GetRequiredService<IServiceScopeFactory>();
        }

        private static IConfiguration CriarConfiguracao(bool? ativa)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (ativa.HasValue)
            {
                valores[ConstantsValues.ChaveRotinaAtrasoPagamentoAtiva] = ativa.Value.ToString();
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        [Fact]
        public async Task RodaAoSubir_UmaChamadaAntesDoPrimeiroIntervalo()
        {
            RotinaFalsa rotina = new RotinaFalsa();
            FakeLogService log = new FakeLogService();
            HospedadoComIntervalo servico = new HospedadoComIntervalo(TimeSpan.FromHours(IntervaloLongoHoras), CriarEscopos(rotina), log, CriarConfiguracao(null));

            await servico.StartAsync(CancellationToken.None);
            await AguardarAsync(() => rotina.Chamadas == PrimeiraChamada);
            await servico.StopAsync(CancellationToken.None);

            Assert.Equal(PrimeiraChamada, rotina.Chamadas);
        }

        [Fact]
        public async Task FalhaDeUmCiclo_VaiParaOLogEOProximoCicloRoda()
        {
            RotinaFalsa rotina = new RotinaFalsa { FalharNaPrimeira = true };
            FakeLogService log = new FakeLogService();
            HospedadoComIntervalo servico = new HospedadoComIntervalo(TimeSpan.FromMilliseconds(IntervaloCurtoMs), CriarEscopos(rotina), log, CriarConfiguracao(null));

            await servico.StartAsync(CancellationToken.None);
            await AguardarAsync(() => rotina.Chamadas >= SegundaChamada);
            await servico.StopAsync(CancellationToken.None);

            Assert.True(rotina.Chamadas >= SegundaChamada);
            Assert.Contains(log.Snapshot(), entrada => entrada.StartsWith(ConstantsMessage.ErroLogAtualizarAtrasosTodasLocadoras, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Cancelamento_SaiSemErro()
        {
            RotinaFalsa rotina = new RotinaFalsa();
            HospedadoComIntervalo servico = new HospedadoComIntervalo(TimeSpan.FromHours(IntervaloLongoHoras), CriarEscopos(rotina), new FakeLogService(), CriarConfiguracao(null));

            await servico.StartAsync(CancellationToken.None);
            await AguardarAsync(() => rotina.Chamadas == PrimeiraChamada);
            Task parada = servico.StopAsync(CancellationToken.None);

            await parada;

            Assert.True(parada.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task ConfiguracaoDesligada_NaoChamaARotina()
        {
            RotinaFalsa rotina = new RotinaFalsa();
            HospedadoComIntervalo servico = new HospedadoComIntervalo(TimeSpan.FromMilliseconds(IntervaloCurtoMs), CriarEscopos(rotina), new FakeLogService(), CriarConfiguracao(false));

            await servico.StartAsync(CancellationToken.None);
            await servico.StopAsync(CancellationToken.None);

            Assert.Equal(0, rotina.Chamadas);
        }
    }
}
