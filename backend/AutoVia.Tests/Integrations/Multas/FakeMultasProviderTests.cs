using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Multas;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Multas;
using AutoVia.Integrations.Services.Multas;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Integrations.Multas
{
    public class FakeMultasProviderTests
    {
        [Theory]
        [InlineData("ABC1230", 0)]
        [InlineData("ABC1231", 1)]
        [InlineData("ABC1232", 2)]
        public async Task Consultar_RetornaQuantidadeDeterministicaNoPeriodo(string placa, int esperada)
        {
            DateTime inicio = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Unspecified);
            DateTime fim = inicio.AddDays(5);
            FakeMultasProvider provider = new FakeMultasProvider(new FakeLogService());

            MultasConsultaResultado resultado = await provider.ConsultarInfracoesAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), new MultasConsultaSolicitacao
            {
                Placa = placa, InicioLocal = inicio, FimLocal = fim
            });

            Assert.Equal(esperada, resultado.Infracoes.Count);
            Assert.All(resultado.Infracoes, infracao =>
            {
                Assert.InRange(infracao.DataHoraLocal, inicio, fim);
                Assert.True(infracao.Valor >= decimal.Zero);
                Assert.Equal(infracao.Valor, decimal.Round(infracao.Valor, ConstantsMultas.CasasDecimaisValor));
                Assert.StartsWith(ConstantsMultas.PrefixoIdentificadorFake, infracao.IdentificadorProvedor);
            });
        }

        [Fact]
        public async Task Consultar_MesmaEntrada_GeraIdentificadoresEstaveis()
        {
            DateTime inicio = new DateTime(2026, 11, 1, 10, 0, 0);
            MultasConsultaSolicitacao solicitacao = new MultasConsultaSolicitacao { Placa = "ABC1232", InicioLocal = inicio, FimLocal = inicio.AddDays(3) };
            FakeMultasProvider provider = new FakeMultasProvider(new FakeLogService());

            MultasConsultaResultado primeiro = await provider.ConsultarInfracoesAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), solicitacao);
            MultasConsultaResultado segundo = await provider.ConsultarInfracoesAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), solicitacao);

            Assert.Equal(
                primeiro.Infracoes.Select(infracao => infracao.IdentificadorProvedor),
                segundo.Infracoes.Select(infracao => infracao.IdentificadorProvedor));
        }

        [Fact]
        public async Task Consultar_IntervaloIgual_AdmiteInfracaoNoLimite()
        {
            DateTime limite = new DateTime(2026, 11, 1, 10, 0, 0);
            FakeMultasProvider provider = new FakeMultasProvider(new FakeLogService());

            MultasConsultaResultado resultado = await provider.ConsultarInfracoesAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), new MultasConsultaSolicitacao
            {
                Placa = "ABC1231", InicioLocal = limite, FimLocal = limite
            });

            Assert.Single(resultado.Infracoes);
            Assert.Equal(limite, resultado.Infracoes[0].DataHoraLocal);
        }

        [Fact]
        public async Task Consultar_ForaDoSandbox_RejeitaFake()
        {
            FakeMultasProvider provider = new FakeMultasProvider(new FakeLogService());

            await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarInfracoesAsync(
                CriarConfiguracao(AmbienteIntegracao.PRODUCAO),
                new MultasConsultaSolicitacao
                {
                    Placa = "ABC1231", InicioLocal = new DateTime(2026, 11, 1), FimLocal = new DateTime(2026, 11, 2)
                }));
        }

        private static ConfiguracaoIntegracao CriarConfiguracao(AmbienteIntegracao ambiente)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Multas,
                "Consulta de multas",
                ambiente,
                "https://sandbox.multas.exemplo.com.br",
                ConstantsIntegration.TimeoutPadraoMs,
                new Dictionary<string, IntegrationSettingValue>());
        }
    }
}