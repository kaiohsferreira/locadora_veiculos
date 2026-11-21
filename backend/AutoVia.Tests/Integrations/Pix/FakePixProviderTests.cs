using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Tests.Integrations.Pix
{
    public class FakePixProviderTests
    {
        private const string Referencia = "PED-2026-0001";

        private static ConfiguracaoIntegracao CriarConfiguracao()
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://sandbox.exemplo.com.br",
                15000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        private static PixCobrancaSolicitacao CriarSolicitacao(string referencia = Referencia)
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = referencia,
                Valor = 10.5m,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador { Nome = "Maria da Silva", Email = "maria@exemplo.com" }
            };
        }

        /// <summary>Provedor com a chave Pagamentos:PermitirProvedorFake ligada (padrao dos testes que exercitam o FAKE respondendo).</summary>
        private static FakePixProvider CriarProvider(PixFakeStore store, string? ambiente = null)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?> { { ConstantsPix.ChavePermitirProvedorFake, "true" } };
            IConfiguration configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

            return new FakePixProvider(store, new GoogleProvaDeTeste.AmbienteFalso(ambiente ?? Environments.Development), configuracao, new FakeLogService());
        }

        private static int LerInteiroBigEndian(byte[] bytes, int inicio)
        {
            return (bytes[inicio] << 24) | (bytes[inicio + 1] << 16) | (bytes[inicio + 2] << 8) | bytes[inicio + 3];
        }

        private static byte[] LerQrCodeBytes(PixCobrancaResultado resultado)
        {
            Assert.NotNull(resultado.QrCodeBase64);

            return Convert.FromBase64String(resultado.QrCodeBase64!);
        }

        [Fact]
        public async Task CriarAsync_IdentificadorEhFakeComDozeHexDoSha256DaReferencia()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());

            PixCobrancaResultado resultado = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Referencia));
            string esperado = "FAKE-" + Convert.ToHexString(hash).ToLowerInvariant().Substring(0, 12);
            Assert.Equal(esperado, resultado.IdentificadorProvedor);
            Assert.Matches("^FAKE-[0-9a-f]{12}$", resultado.IdentificadorProvedor);
        }

        [Fact]
        public async Task CriarAsync_MesmaReferencia_DevolveAMesmaCobranca()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());

            PixCobrancaResultado primeira = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);
            PixCobrancaResultado segunda = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            // Copias diferentes com o mesmo conteudo: o store nunca devolve o objeto guardado.
            Assert.NotSame(primeira, segunda);
            Assert.Equal(primeira.IdentificadorProvedor, segunda.IdentificadorProvedor);
            Assert.Equal(primeira.Status, segunda.Status);
            Assert.Equal(primeira.ExpiraEm, segunda.ExpiraEm);
        }

        [Fact]
        public async Task ConsultarAsync_AlterarACopiaDevolvida_NaoAlteraACobrancaGuardada()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            PixCobrancaResultado copia = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);
            copia.Status = PixStatusCobranca.PAGA;
            copia.PagoEm = DateTime.Now;

            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(PixStatusCobranca.AGUARDANDO_PAGAMENTO, consultada.Status);
            Assert.Null(consultada.PagoEm);
        }

        [Fact]
        public async Task ConsultarAsync_PrazoPassado_DevolveExpirada()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());
            // Expiracao negativa: a cobranca nasce com o prazo ja vencido.
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), -60);

            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(PixStatusCobranca.EXPIRADA, consultada.Status);
        }

        [Fact]
        public async Task MarcarComoPaga_CobrancaCancelada_LancaBusinessExceptionEFicaCancelada()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store);
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);
            await provider.CancelarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            BusinessException excecao = Assert.Throws<BusinessException>(() => store.MarcarComoPaga(criada.IdentificadorProvedor));
            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(ConstantsMessage.ErroPixCobrancaNaoAguardaPagamento, excecao.Message);
            Assert.Equal(PixStatusCobranca.CANCELADA, consultada.Status);
        }

        /// <summary>
        /// Pagamento e cancelamento disputam a mesma cobranca ao mesmo tempo. Em cada rodada, exatamente
        /// um deles pode vencer: se o pagamento venceu, o cancelamento falha e a cobranca fica PAGA;
        /// se o cancelamento venceu, o pagamento falha e a cobranca fica CANCELADA. Nunca as duas coisas.
        /// </summary>
        [Fact]
        public async Task PagarECancelarEmParalelo_NuncaTerminamComResultadoInconsistente()
        {
            const int rodadas = 200;

            for (int rodada = 0; rodada < rodadas; rodada++)
            {
                PixFakeStore store = new PixFakeStore();
                FakePixProvider provider = CriarProvider(store);
                PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao("PED-CONC-" + rodada), 3600);
                string identificador = criada.IdentificadorProvedor;

                using ManualResetEventSlim largada = new ManualResetEventSlim(false);

                Task<bool> pagamento = Task.Run(() =>
                {
                    largada.Wait();
                    return TentarPagar(store, identificador);
                });

                Task<bool> cancelamento = Task.Run(() =>
                {
                    largada.Wait();
                    return TentarCancelar(provider, identificador);
                });

                largada.Set();

                bool pagou = await pagamento;
                bool cancelou = await cancelamento;
                PixCobrancaResultado final = await provider.ConsultarAsync(CriarConfiguracao(), identificador);

                Assert.True(pagou != cancelou, $"Rodada {rodada}: pagou={pagou}, cancelou={cancelou}.");
                Assert.Equal(pagou ? PixStatusCobranca.PAGA : PixStatusCobranca.CANCELADA, final.Status);
            }
        }

        private static bool TentarPagar(PixFakeStore store, string identificador)
        {
            try
            {
                store.MarcarComoPaga(identificador);
                return true;
            }
            catch (BusinessException)
            {
                return false;
            }
        }

        private static bool TentarCancelar(FakePixProvider provider, string identificador)
        {
            try
            {
                provider.CancelarAsync(CriarConfiguracao(), identificador).GetAwaiter().GetResult();
                return true;
            }
            catch (BusinessException)
            {
                return false;
            }
        }

        [Fact]
        public async Task CriarAsync_DevolveValoresComPrefixoFakeEExpiracaoEmHoraLocal()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());
            DateTime antes = DateTime.Now;

            PixCobrancaResultado resultado = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 900);

            Assert.Equal(PixStatusCobranca.AGUARDANDO_PAGAMENTO, resultado.Status);
            Assert.Equal(Referencia, resultado.ReferenciaExterna);
            Assert.Equal(10.5m, resultado.Valor);
            Assert.Equal(ConstantsPix.ProvedorFake, resultado.Provedor);
            Assert.NotNull(resultado.CopiaECola);
            Assert.StartsWith(ConstantsPix.PrefixoCopiaEColaFake, resultado.CopiaECola);
            Assert.NotNull(resultado.ExpiraEm);
            Assert.True(resultado.ExpiraEm >= antes.AddSeconds(900));
            Assert.True(resultado.ExpiraEm <= DateTime.Now.AddSeconds(900));
        }

        [Fact]
        public async Task CriarAsync_QrCodeEhUmPngValidoDe200PorDuzentosPixels()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());

            PixCobrancaResultado resultado = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 900);
            byte[] png = LerQrCodeBytes(resultado);

            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
            Assert.Equal("IHDR", Encoding.ASCII.GetString(png, 12, 4));
            Assert.Equal(200, LerInteiroBigEndian(png, 16));
            Assert.Equal(200, LerInteiroBigEndian(png, 20));
            Assert.Equal("IEND", Encoding.ASCII.GetString(png, png.Length - 8, 4));
        }

        [Fact]
        public async Task CriarAsync_MesmoIdentificador_DevolveOsMesmosBytesDoQrCode()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());

            PixCobrancaResultado primeiro = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 900);
            PixCobrancaResultado mesmaReferencia = await CriarProvider(new PixFakeStore()).CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 900);
            PixCobrancaResultado outraReferencia = await CriarProvider(new PixFakeStore()).CriarAsync(CriarConfiguracao(), CriarSolicitacao("PED-2026-0002"), 900);

            Assert.Equal(LerQrCodeBytes(primeiro), LerQrCodeBytes(mesmaReferencia));
            Assert.NotEqual(LerQrCodeBytes(primeiro), LerQrCodeBytes(outraReferencia));
        }

        [Fact]
        public async Task CriarAsync_ForaDeDevelopment_LancaFakeForaDoSandbox()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore(), Environments.Production);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 900));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
        }

        [Fact]
        public async Task ConsultarAsync_CobrancaCriada_DevolveAguardandoPagamento()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(PixStatusCobranca.AGUARDANDO_PAGAMENTO, consultada.Status);
        }

        [Fact]
        public async Task ConsultarAsync_IdentificadorDesconhecido_LancaNotFoundException()
        {
            FakePixProvider provider = CriarProvider(new PixFakeStore());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => provider.ConsultarAsync(CriarConfiguracao(), "FAKE-000000000000"));

            Assert.Equal(ConstantsMessage.ErroPixCobrancaNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task ConsultarAsync_AposMarcarComoPaga_DevolvePagaComDataDoPagamento()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store);
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            store.MarcarComoPaga(criada.IdentificadorProvedor);
            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(PixStatusCobranca.PAGA, consultada.Status);
            Assert.NotNull(consultada.PagoEm);
        }

        [Fact]
        public async Task CancelarAsync_AguardandoPagamento_DevolveCancelada()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store);
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            PixCobrancaResultado cancelada = await provider.CancelarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);
            PixCobrancaResultado consultada = await provider.ConsultarAsync(CriarConfiguracao(), criada.IdentificadorProvedor);

            Assert.Equal(PixStatusCobranca.CANCELADA, cancelada.Status);
            Assert.Equal(PixStatusCobranca.CANCELADA, consultada.Status);
        }

        [Fact]
        public async Task CancelarAsync_CobrancaPaga_LancaCancelarPaga()
        {
            PixFakeStore store = new PixFakeStore();
            FakePixProvider provider = CriarProvider(store);
            PixCobrancaResultado criada = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);
            store.MarcarComoPaga(criada.IdentificadorProvedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CancelarAsync(CriarConfiguracao(), criada.IdentificadorProvedor));

            Assert.Equal(ConstantsMessage.ErroPixCancelarCobrancaPaga, excecao.Message);
        }

        [Fact]
        public void MarcarComoPaga_IdentificadorDesconhecido_LancaNotFoundException()
        {
            PixFakeStore store = new PixFakeStore();

            NotFoundException excecao = Assert.Throws<NotFoundException>(() => store.MarcarComoPaga("FAKE-000000000000"));

            Assert.Equal(ConstantsMessage.ErroPixCobrancaNaoEncontrada, excecao.Message);
        }
    }
}
