using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Tests.Integrations.Caucao
{
    public class FakeCaucaoProviderTests
    {
        private const string Chave = "caucao-0123456789abcdef0123456789abcdef01234567";
        private const string Referencia = "CAUCAO-LOC-123";
        private const string TokenNormal = "tok_cartao_9876543210";
        private static readonly DateTime Agora = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Local);

        private static ConfiguracaoIntegracao CriarConfiguracao()
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://sandbox.exemplo.com.br",
                30000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        private static CaucaoSolicitacao CriarSolicitacao(string token = TokenNormal, decimal valor = 500m)
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = valor,
                Descricao = "Caucao da locacao",
                TokenCartao = token,
                MetodoPagamentoId = "visa",
                Pagador = new CaucaoPagador { Email = "maria@exemplo.com" }
            };
        }

        private static FakeCaucaoProvider CriarProvider(CaucaoFakeStore store)
        {
            return new FakeCaucaoProvider(store, new FakeLogService());
        }

        private static Task<CaucaoResultado> Reservar(FakeCaucaoProvider provider, string token = TokenNormal, decimal valor = 500m, int validadeDias = 5)
        {
            return provider.PreAutorizarAsync(CriarConfiguracao(), CriarSolicitacao(token, valor), Chave, validadeDias);
        }

        [Fact]
        public async Task PreAutorizar_IdentificadorEhFakeCauComDozeHexDoSha256DaChave()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore());

            CaucaoResultado resultado = await Reservar(provider);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Chave));
            string esperado = "FAKE-CAU-" + Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 12);
            Assert.Equal(esperado, resultado.IdentificadorProvedor);
            Assert.Matches("^FAKE-CAU-[0-9a-f]{12}$", resultado.IdentificadorProvedor);
        }

        [Fact]
        public async Task PreAutorizar_ReservaComCartaoFinalFakeECriadoEmDoRelogio()
        {
            CaucaoFakeStore store = new CaucaoFakeStore(() => Agora);
            FakeCaucaoProvider provider = CriarProvider(store);

            CaucaoResultado resultado = await Reservar(provider);

            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(500m, resultado.Valor);
            Assert.Equal(ConstantsCaucao.CartaoFinalFake, resultado.CartaoFinal);
            Assert.Equal(Agora, resultado.CriadoEm);
            Assert.Equal(Agora.AddDays(5), resultado.ReservaValidaAte);
            Assert.Equal(ConstantsCaucao.ProvedorFake, resultado.Provedor);
        }

        [Fact]
        public async Task PreAutorizar_MesmaChave_DevolveAMesmaCaucaoSemReservarDeNovo()
        {
            CaucaoFakeStore store = new CaucaoFakeStore(() => Agora);
            FakeCaucaoProvider provider = CriarProvider(store);

            CaucaoResultado primeira = await Reservar(provider);
            await provider.CapturarAsync(CriarConfiguracao(), primeira.IdentificadorProvedor, 200m);
            CaucaoResultado segunda = await Reservar(provider);

            Assert.Equal(primeira.IdentificadorProvedor, segunda.IdentificadorProvedor);
            Assert.Equal(CaucaoStatus.CAPTURADA, segunda.Status);
            Assert.Equal(200m, segunda.ValorCapturado);
        }

        [Fact]
        public async Task Ciclo_ReservarConsultarCapturarParcial_EstadoFinalCapturada()
        {
            CaucaoFakeStore store = new CaucaoFakeStore(() => Agora);
            FakeCaucaoProvider provider = CriarProvider(store);
            CaucaoResultado reservada = await Reservar(provider);

            CaucaoResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);
            CaucaoResultado capturada = await provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 200m);
            CaucaoResultado depois = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.RESERVADA, consultada.Status);
            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Equal(200m, capturada.ValorCapturado);
            Assert.Equal(Agora, capturada.CapturadoEm);
            Assert.Equal(CaucaoStatus.CAPTURADA, depois.Status);
            Assert.Equal(500m, depois.Valor);
        }

        [Fact]
        public async Task Ciclo_CapturaIntegralUsaOValorReservado()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);

            CaucaoResultado capturada = await provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, null);

            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Equal(500m, capturada.ValorCapturado);
        }

        [Fact]
        public async Task Ciclo_ReservarCancelar_EstadoFinalCancelada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);

            CaucaoResultado cancelada = await provider.CancelarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);
            CaucaoResultado denovo = await provider.CancelarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, cancelada.Status);
            Assert.Equal(CaucaoStatus.CANCELADA, denovo.Status);
        }

        [Fact]
        public async Task Capturar_AcimaDoReservado_LancaBusinessException()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 600m));

            Assert.Equal(ConstantsMessage.ErroCaucaoCapturaAcimaReservado, excecao.Message);
        }

        [Fact]
        public async Task Capturar_DepoisDeCancelada_LancaNaoReservada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);
            await provider.CancelarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 100m));

            Assert.Equal(ConstantsMessage.ErroCaucaoNaoReservada, excecao.Message);
        }

        [Fact]
        public async Task Cancelar_DepoisDeCapturada_LancaCancelarCapturada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);
            await provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 100m);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CancelarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroCaucaoCancelarCapturada, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_TokenRecusado_LancaBusinessExceptionEConsultaDevolveRecusada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => Reservar(provider, ConstantsCaucao.TokenFakeRecusado));

            Assert.Equal(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro), excecao.Message);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Chave));
            string identificador = "FAKE-CAU-" + Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 12);
            CaucaoResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), identificador);

            Assert.Equal(CaucaoStatus.RECUSADA, consultada.Status);
        }

        [Fact]
        public async Task PreAutorizar_TokenEmAnalise_DevolveEmAnaliseSemReservaValidaAte()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));

            CaucaoResultado resultado = await Reservar(provider, ConstantsCaucao.TokenFakeAnalise);

            Assert.Equal(CaucaoStatus.EM_ANALISE, resultado.Status);
            Assert.Null(resultado.ReservaValidaAte);
        }

        [Fact]
        public async Task Capturar_EmAnalise_LancaNaoReservada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado emAnalise = await Reservar(provider, ConstantsCaucao.TokenFakeAnalise);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CapturarAsync(CriarConfiguracao(), emAnalise.IdentificadorProvedor, null));

            Assert.Equal(ConstantsMessage.ErroCaucaoNaoReservada, excecao.Message);
        }

        [Fact]
        public async Task Expiracao_PeloRelogio_ReservaVencidaVirarExpiradaELiberaNaoCaptura()
        {
            DateTime relogio = Agora;
            CaucaoFakeStore store = new CaucaoFakeStore(() => relogio);
            FakeCaucaoProvider provider = CriarProvider(store);
            CaucaoResultado reservada = await Reservar(provider, validadeDias: 1);

            relogio = Agora.AddDays(2);
            CaucaoResultado expirada = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);
            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 100m));
            CaucaoResultado cancelamento = await provider.CancelarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.RESERVADA, reservada.Status);
            Assert.Equal(CaucaoStatus.EXPIRADA, expirada.Status);
            Assert.Equal(ConstantsMessage.ErroCaucaoNaoReservada, excecao.Message);
            Assert.Equal(CaucaoStatus.EXPIRADA, cancelamento.Status);
        }

        [Fact]
        public async Task Consultar_IdInexistente_LancaNotFoundException()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => provider.ConsultarAsync(CriarConfiguracao(), "FAKE-CAU-000000000000"));

            Assert.Equal(ConstantsMessage.ErroCaucaoNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task Capturar_IdInexistente_LancaNotFoundException()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));

            await Assert.ThrowsAsync<NotFoundException>(() => provider.CapturarAsync(CriarConfiguracao(), "FAKE-CAU-000000000000", 10m));
        }

        [Fact]
        public async Task Cancelar_IdInexistente_LancaNotFoundException()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));

            await Assert.ThrowsAsync<NotFoundException>(() => provider.CancelarAsync(CriarConfiguracao(), "FAKE-CAU-000000000000"));
        }

        [Fact]
        public async Task FakeAnalise_Cancelar_VaiParaCancelada()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado emAnalise = await Reservar(provider, ConstantsCaucao.TokenFakeAnalise);

            CaucaoResultado cancelada = await provider.CancelarAsync(CriarConfiguracao(), emAnalise.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.CANCELADA, cancelada.Status);
            Assert.Null(cancelada.ReservaValidaAte);
        }

        [Fact]
        public async Task ReservaValidaAte_NulaAposCaptura()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);

            CaucaoResultado capturada = await provider.CapturarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor, 200m);

            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Null(capturada.ReservaValidaAte);
        }

        [Fact]
        public async Task ReservaValidaAte_NulaQuandoExpirada()
        {
            DateTime relogio = Agora;
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => relogio));
            CaucaoResultado reservada = await Reservar(provider, validadeDias: 1);

            relogio = Agora.AddDays(2);
            CaucaoResultado expirada = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.EXPIRADA, expirada.Status);
            Assert.Null(expirada.ReservaValidaAte);
        }

        /// <summary>
        /// Concorrencia real, repetida: 50 rodadas independentes, cada uma com uma caucao nova. Em cada rodada,
        /// 20 capturas e 20 cancelamentos rodam em threads dedicadas (LongRunning, uma por operacao). Todas esperam
        /// uma Barrier e so entao sao liberadas juntas, pela thread de teste. Sem o lock do store, as invariantes
        /// abaixo quebram em alguma rodada; a repeticao torna a deteccao independente do agendamento do pool.
        /// </summary>
        [Fact]
        public async Task Concorrencia_CapturasECancelamentosLiberadosJuntos_EstadoFinalConsistente()
        {
            const int rodadas = 50;
            const int operacoesPorTipo = 20;

            for (int rodada = 0; rodada < rodadas; rodada++)
            {
                await ExecutarRodadaConcorrenteAsync(operacoesPorTipo);
            }
        }

        private static async Task ExecutarRodadaConcorrenteAsync(int operacoesPorTipo)
        {
            CaucaoFakeStore store = new CaucaoFakeStore(() => Agora);
            FakeCaucaoProvider provider = CriarProvider(store);
            CaucaoResultado reservada = await Reservar(provider);
            string identificador = reservada.IdentificadorProvedor;

            int total = operacoesPorTipo * 2;
            string[] resultados = new string[total];

            // Participantes: as 'total' threads de operacao + a thread de teste, que libera todas ao mesmo tempo.
            using Barrier barreira = new Barrier(total + 1);
            List<Task> tarefas = new List<Task>();

            for (int i = 0; i < operacoesPorTipo; i++)
            {
                int indice = i;

                tarefas.Add(Task.Factory.StartNew(() =>
                {
                    barreira.SignalAndWait();
                    resultados[indice] = ExecutarCaptura(store, identificador);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }

            for (int i = 0; i < operacoesPorTipo; i++)
            {
                int indice = operacoesPorTipo + i;

                tarefas.Add(Task.Factory.StartNew(() =>
                {
                    barreira.SignalAndWait();
                    resultados[indice] = ExecutarCancelamento(store, identificador);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
            }

            // Libera todas as threads juntas. Qualquer excecao que nao seja BusinessException faz o WhenAll falhar.
            barreira.SignalAndWait();
            await Task.WhenAll(tarefas);

            int capturasOk = resultados.Count(r => r == TipoCaptura.Ok);
            int cancelamentosEfetivos = resultados.Count(r => r == TipoCancelamento.Efetivo);
            int cancelamentosJaEncerrados = resultados.Count(r => r == TipoCancelamento.JaEncerrado);

            CaucaoResultado final = await provider.ConsultarAsync(CriarConfiguracao(), identificador);

            Assert.True(final.Status == CaucaoStatus.CAPTURADA || final.Status == CaucaoStatus.CANCELADA);

            if (final.Status == CaucaoStatus.CAPTURADA)
            {
                // Capturou primeiro: exatamente 1 captura ok, nenhum cancelamento ok (todos viram erro de caucao capturada).
                Assert.Equal(1, capturasOk);
                Assert.Equal(0, cancelamentosEfetivos);
                Assert.Equal(0, cancelamentosJaEncerrados);
                Assert.Equal(100m, final.ValorCapturado);
            }
            else
            {
                // Cancelou primeiro: 0 capturas ok e exatamente 1 cancelamento que efetivou a transicao.
                // Os demais cancelamentos devolvem o estado ja cancelado (sucesso sem transicao), por isso
                // "ja encerrado" pode ser qualquer numero; a asserção so exige que nenhum outro tenha efetivado.
                Assert.Equal(0, capturasOk);
                Assert.Equal(1, cancelamentosEfetivos);
                Assert.Null(final.ValorCapturado);
            }

            // Toda chamada terminou em sucesso ou BusinessException (nenhum outro tipo chegou a este ponto).
            Assert.Equal(total, resultados.Length);
            Assert.All(resultados, r => Assert.Contains(r, new[] { TipoCaptura.Ok, TipoCaptura.Negocio, TipoCancelamento.Efetivo, TipoCancelamento.JaEncerrado, TipoCancelamento.Negocio }));
        }

        private static string ExecutarCaptura(CaucaoFakeStore store, string identificador)
        {
            try
            {
                store.Capturar(identificador, 100m);
                return TipoCaptura.Ok;
            }
            catch (BusinessException)
            {
                return TipoCaptura.Negocio;
            }
        }

        private static string ExecutarCancelamento(CaucaoFakeStore store, string identificador)
        {
            try
            {
                store.CancelarComIndicador(identificador, out bool efetivou);
                return efetivou ? TipoCancelamento.Efetivo : TipoCancelamento.JaEncerrado;
            }
            catch (BusinessException)
            {
                return TipoCancelamento.Negocio;
            }
        }

        private static class TipoCaptura
        {
            public const string Ok = "captura-ok";
            public const string Negocio = "captura-negocio";
        }

        private static class TipoCancelamento
        {
            public const string Efetivo = "cancelamento-efetivo";
            public const string JaEncerrado = "cancelamento-ja-encerrado";
            public const string Negocio = "cancelamento-negocio";
        }

        [Fact]
        public async Task EstadoImutavel_AlterarACopiaNaoAlteraOQueEstaGuardado()
        {
            FakeCaucaoProvider provider = CriarProvider(new CaucaoFakeStore(() => Agora));
            CaucaoResultado reservada = await Reservar(provider);

            CaucaoResultado copia = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);
            copia.Status = CaucaoStatus.CANCELADA;
            copia.Valor = 1m;

            CaucaoResultado depois = await provider.ConsultarAsync(CriarConfiguracao(), reservada.IdentificadorProvedor);

            Assert.Equal(CaucaoStatus.RESERVADA, depois.Status);
            Assert.Equal(500m, depois.Valor);
        }
    }
}
