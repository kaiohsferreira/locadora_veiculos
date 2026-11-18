using AutoVia.Api.Extensions;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Registro agregado do 429 da rota de leitura: a primeira recusa grava na hora, as seguintes so contam ate passar o intervalo
    /// de um minuto, o registro seguinte leva a contagem acumulada, cada motivo tem o seu contador, e chamadas simultaneas nao
    /// perdem contagem. Relogio de teste (FakeTimeProvider); nenhuma espera real.
    /// </summary>
    public sealed class RegistroRecusasLimiteLeituraTests
    {
        private const int IntervaloDentroDoMinutoSegundos = 10;
        private const int IntervaloAposOMinutoSegundos = 61;
        private const int RecusasDepoisDaPrimeira = 3;
        private const int RecusasSimultaneas = 999;
        private const int TotalAcumuladoEsperado = 1000;
        private const int ContagemUm = 1;

        /// <summary>ILogService que guarda as chamadas por sobrecarga; o registro agregado so usa a de texto.</summary>
        private sealed class LogCapturado : ILogService
        {
            private readonly object _trava = new object();

            public List<string> Mensagens { get; } = new List<string>();

            public List<string> Amigaveis { get; } = new List<string>();

            public List<System.Exception> Excecoes { get; } = new List<System.Exception>();

            public List<string> Informativos { get; } = new List<string>();

            public void Write(string message, string friendlyMessage, string source)
            {
                lock (_trava)
                {
                    Mensagens.Add(message);
                    Amigaveis.Add(friendlyMessage);
                }
            }

            public void Write(System.Exception exception, string friendlyMessage, string source)
            {
                lock (_trava)
                {
                    Excecoes.Add(exception);
                }
            }

            public void WriteInfo(string message, string source)
            {
                lock (_trava)
                {
                    Informativos.Add(message);
                }
            }
        }

        private static string MensagemPorIp(int contagem)
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraLimitePorIp, contagem);
        }

        private static string MensagemDoTeto(int contagem)
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogArmazenamentoLeituraTetoDaRota, contagem);
        }

        [Fact]
        public void PrimeiraRecusaGravaNaHoraComContagemUm()
        {
            LogCapturado log = new LogCapturado();
            RegistroRecusasLimiteLeitura registro = new RegistroRecusasLimiteLeitura(log, new FakeTimeProvider(DateTimeOffset.UtcNow));

            registro.RegistrarPorIp();

            Assert.Single(log.Mensagens);
            Assert.Equal(MensagemPorIp(ContagemUm), log.Mensagens[0]);
            Assert.Equal(ConstantsMessage.ErroLimiteRequisicoes, log.Amigaveis[0]);
        }

        [Fact]
        public void RecusasDentroDeUmMinuto_NaoGravam()
        {
            LogCapturado log = new LogCapturado();
            FakeTimeProvider relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
            RegistroRecusasLimiteLeitura registro = new RegistroRecusasLimiteLeitura(log, relogio);

            registro.RegistrarPorIp();
            relogio.Avancar(TimeSpan.FromSeconds(IntervaloDentroDoMinutoSegundos));
            for (int recusa = 0; recusa < RecusasDepoisDaPrimeira; recusa++)
            {
                registro.RegistrarPorIp();
            }

            Assert.Single(log.Mensagens);
        }

        [Fact]
        public void PassadoOMinuto_GravaComAContagemAcumulada()
        {
            LogCapturado log = new LogCapturado();
            FakeTimeProvider relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
            RegistroRecusasLimiteLeitura registro = new RegistroRecusasLimiteLeitura(log, relogio);

            registro.RegistrarPorIp();
            for (int recusa = 0; recusa < RecusasDepoisDaPrimeira; recusa++)
            {
                registro.RegistrarPorIp();
            }
            relogio.Avancar(TimeSpan.FromSeconds(IntervaloAposOMinutoSegundos));
            registro.RegistrarPorIp();

            Assert.Equal(2, log.Mensagens.Count);
            Assert.Equal(MensagemPorIp(RecusasDepoisDaPrimeira + ContagemUm), log.Mensagens[1]);
        }

        [Fact]
        public void MotivosDiferentes_TemContadoresIndependentes()
        {
            LogCapturado log = new LogCapturado();
            RegistroRecusasLimiteLeitura registro = new RegistroRecusasLimiteLeitura(log, new FakeTimeProvider(DateTimeOffset.UtcNow));

            registro.RegistrarPorIp();
            registro.RegistrarTeto();

            Assert.Equal(2, log.Mensagens.Count);
            Assert.Equal(MensagemPorIp(ContagemUm), log.Mensagens[0]);
            Assert.Equal(MensagemDoTeto(ContagemUm), log.Mensagens[1]);
        }

        [Fact]
        public async Task ChamadasSimultaneas_NaoPerdemContagem()
        {
            LogCapturado log = new LogCapturado();
            FakeTimeProvider relogio = new FakeTimeProvider(DateTimeOffset.UtcNow);
            RegistroRecusasLimiteLeitura registro = new RegistroRecusasLimiteLeitura(log, relogio);

            registro.RegistrarPorIp();
            await Task.WhenAll(Enumerable.Range(0, RecusasSimultaneas).Select(indice => Task.Run(() => registro.RegistrarPorIp())));
            relogio.Avancar(TimeSpan.FromSeconds(IntervaloAposOMinutoSegundos));
            registro.RegistrarPorIp();

            Assert.Equal(2, log.Mensagens.Count);
            Assert.Equal(MensagemPorIp(TotalAcumuladoEsperado), log.Mensagens[1]);
            Assert.Empty(log.Excecoes);
            Assert.Empty(log.Informativos);
        }
    }
}
