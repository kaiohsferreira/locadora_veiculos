using AutoVia.Api.Extensions;
using AutoVia.Helpers;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Globalization;

namespace AutoVia.Tests.Api
{
    /// <summary>
    /// Teto da rota de leitura de arquivo: contador unico, esgota no limite, vira a janela, e o numero da configuracao cai no
    /// padrao quando ausente, zero ou negativo. Janela curta nos testes (o limitador usa o relogio do sistema, nao o TimeProvider).
    /// </summary>
    public sealed class TetoLeituraArquivoTests
    {
        private const int LimiteDoTesteBaixo = 3;
        private const int LimiteDoTesteDeConfiguracao = 7;
        private const int JanelaCurtaMilissegundos = 1000;
        private const int EsperaMaximaVirada = 5000;
        private const int IntervaloEntreTentativas = 50;
        private const string ValorDeConfiguracaoZero = "0";
        private const string ValorDeConfiguracaoNegativo = "-5";

        [Fact]
        public void EsgotaNoLimiteEDepoisRecusa()
        {
            using TetoLeituraArquivo teto = new TetoLeituraArquivo(LimiteDoTesteBaixo, TimeSpan.FromMinutes(1));

            Assert.True(teto.TentarEntrar());
            Assert.True(teto.TentarEntrar());
            Assert.True(teto.TentarEntrar());
            Assert.False(teto.TentarEntrar());
            Assert.False(teto.TentarEntrar());
        }

        [Fact]
        public async Task DepoisDaJanelaVoltaAEntrar()
        {
            using TetoLeituraArquivo teto = new TetoLeituraArquivo(LimiteDoTesteBaixo - 1, TimeSpan.FromMilliseconds(JanelaCurtaMilissegundos));

            Assert.True(teto.TentarEntrar());
            Assert.True(teto.TentarEntrar());
            Assert.False(teto.TentarEntrar());

            bool voltou = false;
            for (int espera = 0; espera < EsperaMaximaVirada && !voltou; espera += IntervaloEntreTentativas)
            {
                await Task.Delay(IntervaloEntreTentativas);
                voltou = teto.TentarEntrar();
            }

            Assert.True(voltou);
        }

        [Theory]
        [InlineData(ValorDeConfiguracaoZero)]
        [InlineData(ValorDeConfiguracaoNegativo)]
        [InlineData(null)]
        public void ValorZeroNegativoOuAusente_CaiNoPadrao(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();
            if (valor != null)
            {
                valores[ConstantsValues.ChaveLimiteLeituraArquivoGlobal] = valor;
            }

            IConfiguration configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
            using TetoLeituraArquivo teto = new TetoLeituraArquivo(configuracao);

            for (int entrada = 0; entrada < ConstantsValues.LimiteLeituraArquivoGlobalPorMinutoPadrao; entrada++)
            {
                Assert.True(teto.TentarEntrar(), string.Format(CultureInfo.InvariantCulture, "Entrada {0}", entrada));
            }

            Assert.False(teto.TentarEntrar());
        }

        [Fact]
        public void ValorDaConfiguracao_ViraOLimite()
        {
            IConfiguration configuracao = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { { ConstantsValues.ChaveLimiteLeituraArquivoGlobal, LimiteDoTesteDeConfiguracao.ToString(CultureInfo.InvariantCulture) } })
                .Build();
            using TetoLeituraArquivo teto = new TetoLeituraArquivo(configuracao);

            for (int entrada = 0; entrada < LimiteDoTesteDeConfiguracao; entrada++)
            {
                Assert.True(teto.TentarEntrar());
            }

            Assert.False(teto.TentarEntrar());
        }

        [Fact]
        public void Descartar_DuasVezesNaoLanca()
        {
            TetoLeituraArquivo teto = new TetoLeituraArquivo(LimiteDoTesteBaixo, TimeSpan.FromMinutes(1));

            teto.Dispose();

            Assert.Null(Record.Exception(() => teto.Dispose()));
        }
    }
}
