using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Tests.Integrations.Caucao
{
    /// <summary>
    /// Decisao 5 de 24/11/2026 ('Aprovo 1 a 6'; contrato integracao-pagamento-caucao v1.3): o FAKE da caucao ganha a MESMA trava
    /// do PIX. So responde com a chave Pagamentos:PermitirProvedorFake verdadeira, a API em Development e a integracao em SANDBOX,
    /// nas quatro operacoes e antes de tocar no CaucaoFakeStore (nada e criado, consultado, capturado nem cancelado).
    /// SimulacaoLiberada usa o mesmo predicado, sem lancar. A chave e a mesma do PIX e e relida a cada chamada.
    /// </summary>
    public class ChaveProvedorFakeCaucaoTests
    {
        private const string Chave = "caucao-0123456789abcdef0123456789abcdef01234567";
        private const string IdentificadorInexistente = "FAKE-CAU-000000000000";

        /// <summary>Texto do 502 da trava aprovado pelo Kaio (ajuste A3, 24/11/2026), fixado aqui: trocar a constante quebra o teste.</summary>
        private const string TextoNeutroAprovado = "O provedor de teste (FAKE) da caução não está liberado neste ambiente.";

        private static ConfiguracaoIntegracao CriarConfiguracao(AmbienteIntegracao ambiente)
        {
            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                ambiente,
                "https://sandbox.exemplo.com.br",
                30000,
                new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase));
        }

        private static CaucaoSolicitacao CriarSolicitacao()
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = "CAUCAO-RES-42",
                Valor = 1200.00m,
                Descricao = "Caucao da locacao",
                TokenCartao = "tok_cartao_9876543210",
                MetodoPagamentoId = "visa",
                Pagador = new CaucaoPagador { Email = "maria@exemplo.com" }
            };
        }

        /// <summary>Configuracao em memoria com a chave no valor dado; null = chave ausente.</summary>
        private static IConfiguration CriarConfiguracaoDaChave(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valor != null)
            {
                valores.Add(ConstantsPix.ChavePermitirProvedorFake, valor);
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        private static FakeCaucaoProvider CriarProvider(CaucaoFakeStore store, string ambienteDeHospedagem, string? valorDaChave)
        {
            return new FakeCaucaoProvider(store, new GoogleProvaDeTeste.AmbienteFalso(ambienteDeHospedagem), CriarConfiguracaoDaChave(valorDaChave), new FakeLogService());
        }

        /// <summary>Provedor com a chave ligada e em Development: com a configuracao em SANDBOX, a trava fica liberada.</summary>
        private static FakeCaucaoProvider CriarProviderLiberado(CaucaoFakeStore store)
        {
            return CriarProvider(store, Environments.Development, "true");
        }

        /// <summary>Identificador que o FAKE da a esta chave de idempotencia: FAKE-CAU- + 12 hex do SHA-256 dela.</summary>
        private static string IdentificadorDaChave()
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(Chave));

            return ConstantsCaucao.PrefixoIdentificadorFake + Convert.ToHexString(hash).ToLowerInvariant().Substring(0, ConstantsCaucao.TamanhoHashIdentificadorFake);
        }

        /// <summary>Reserva uma caucao pela trava liberada (SANDBOX) no store informado e devolve o identificador.</summary>
        private static async Task<string> ReservarComATravaLiberadaAsync(CaucaoFakeStore store)
        {
            CaucaoResultado reservada = await CriarProviderLiberado(store).PreAutorizarAsync(CriarConfiguracao(AmbienteIntegracao.SANDBOX), CriarSolicitacao(), Chave, 5);

            Assert.Equal(CaucaoStatus.RESERVADA, reservada.Status);

            return reservada.IdentificadorProvedor;
        }

        /// <summary>
        /// Cada linha fecha a trava por uma condicao: (ambiente de hospedagem, valor da chave, ambiente da integracao). Chave
        /// ausente, falsa ou que nao e booleana; API em Production ou em Testing (o ambiente dos testes HTTP); integracao em
        /// PRODUCAO ou HOMOLOGACAO.
        /// </summary>
        public static TheoryData<string, string?, AmbienteIntegracao> TravaFechada => new TheoryData<string, string?, AmbienteIntegracao>
        {
            { "Development", null, AmbienteIntegracao.SANDBOX },
            { "Development", "false", AmbienteIntegracao.SANDBOX },
            { "Development", "abc", AmbienteIntegracao.SANDBOX },
            { "Production", "true", AmbienteIntegracao.SANDBOX },
            { "Testing", "true", AmbienteIntegracao.SANDBOX },
            { "Development", "true", AmbienteIntegracao.PRODUCAO },
            { "Development", "true", AmbienteIntegracao.HOMOLOGACAO }
        };

        /// <summary>
        /// Ajuste A3 do Kaio (24/11/2026, 'Texto neutro no 502'; contrato integracao-pagamento-caucao v1.3.2): a recusa da trava
        /// mostra o texto NEUTRO aprovado, que cita a caucao e NAO mostra o nome da chave Pagamentos:PermitirProvedorFake (ela fica
        /// so no README). Como o texto nao cita mais as tres condicoes, elas sao provadas pelo COMPORTAMENTO: cada linha de
        /// TravaFechada fecha a trava por uma condicao so (chave, Development ou SANDBOX), o FAKE recusa com o mesmo texto neutro e
        /// nada e criado no store; no mesmo store, com as tres condicoes juntas, o mesmo pedido reserva. O texto do PIX nao muda
        /// (ChaveProvedorFakePixTests).
        /// </summary>
        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task TextoDaRecusaDoFake_EhONeutroAprovado_EmCadaCondicaoDaTrava(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.PreAutorizarAsync(CriarConfiguracao(ambienteDaIntegracao), CriarSolicitacao(), Chave, 5));

            Assert.Equal(TextoNeutroAprovado, ConstantsMessage.ErroCaucaoFakeForaDoSandbox);
            Assert.Equal(TextoNeutroAprovado, excecao.Message);
            Assert.Contains("caução", excecao.Message);
            Assert.DoesNotContain(ConstantsPix.ChavePermitirProvedorFake, excecao.Message);
            Assert.Null(store.Obter(IdentificadorDaChave()));

            // Controle: com as tres condicoes juntas o mesmo pedido reserva no mesmo store, entao a recusa acima veio da condicao
            // fechada na linha (e nao de outro motivo).
            string identificadorReservado = await ReservarComATravaLiberadaAsync(store);
            Assert.Equal(IdentificadorDaChave(), identificadorReservado);
        }

        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task PreAutorizar_TravaFechada_RecusaENaoCriaNada(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.PreAutorizarAsync(CriarConfiguracao(ambienteDaIntegracao), CriarSolicitacao(), Chave, 5));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);
            Assert.Equal(ConstantsIntegration.Pagamento, excecao.CodigoIntegracao);
            Assert.Null(store.Obter(IdentificadorDaChave()));
        }

        /// <summary>Com a trava fechada, nem um id inexistente chega ao store: a resposta e a recusa da trava (502), nao o 404 da consulta.</summary>
        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task Consultar_TravaFechada_RecusaAntesDeLerOStore(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            string identificador = await ReservarComATravaLiberadaAsync(store);
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            IntegrationException existente = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(ambienteDaIntegracao), identificador));
            IntegrationException inexistente = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(ambienteDaIntegracao), IdentificadorInexistente));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, existente.Message);
            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, inexistente.Message);
        }

        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task Capturar_TravaFechada_RecusaENaoCaptura(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            string identificador = await ReservarComATravaLiberadaAsync(store);
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CapturarAsync(CriarConfiguracao(ambienteDaIntegracao), identificador, 200m));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);

            CaucaoResultado? depois = store.Obter(identificador);
            Assert.NotNull(depois);
            Assert.Equal(CaucaoStatus.RESERVADA, depois.Status);
            Assert.Null(depois.ValorCapturado);
        }

        [Theory]
        [MemberData(nameof(TravaFechada))]
        public async Task Cancelar_TravaFechada_RecusaENaoCancela(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            string identificador = await ReservarComATravaLiberadaAsync(store);
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CancelarAsync(CriarConfiguracao(ambienteDaIntegracao), identificador));

            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);

            CaucaoResultado? depois = store.Obter(identificador);
            Assert.NotNull(depois);
            Assert.Equal(CaucaoStatus.RESERVADA, depois.Status);
        }

        /// <summary>As tres condicoes juntas: as quatro operacoes respondem (reservar, consultar e cancelar; capturar em outra caucao).</summary>
        [Fact]
        public async Task TravaLiberada_AsQuatroOperacoesRespondem()
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            FakeCaucaoProvider provider = CriarProviderLiberado(store);
            ConfiguracaoIntegracao sandbox = CriarConfiguracao(AmbienteIntegracao.SANDBOX);

            CaucaoResultado reservada = await provider.PreAutorizarAsync(sandbox, CriarSolicitacao(), Chave, 5);
            CaucaoResultado consultada = await provider.ConsultarAsync(sandbox, reservada.IdentificadorProvedor);
            CaucaoResultado cancelada = await provider.CancelarAsync(sandbox, reservada.IdentificadorProvedor);

            CaucaoResultado outra = await provider.PreAutorizarAsync(sandbox, CriarSolicitacao(), Chave + "1", 5);
            CaucaoResultado capturada = await provider.CapturarAsync(sandbox, outra.IdentificadorProvedor, 200m);

            Assert.Equal(IdentificadorDaChave(), reservada.IdentificadorProvedor);
            Assert.Equal(CaucaoStatus.RESERVADA, reservada.Status);
            Assert.Equal(CaucaoStatus.RESERVADA, consultada.Status);
            Assert.Equal(CaucaoStatus.CANCELADA, cancelada.Status);
            Assert.Equal(CaucaoStatus.CAPTURADA, capturada.Status);
            Assert.Equal(200m, capturada.ValorCapturado);
        }

        [Fact]
        public void SimulacaoLiberada_ComAsTresCondicoes_Verdadeiro()
        {
            FakeCaucaoProvider provider = CriarProviderLiberado(new CaucaoFakeStore());

            Assert.True(provider.SimulacaoLiberada(CriarConfiguracao(AmbienteIntegracao.SANDBOX)));
        }

        /// <summary>Fora de qualquer uma das tres condicoes: falso, sem lancar e sem tocar no store.</summary>
        [Theory]
        [MemberData(nameof(TravaFechada))]
        public void SimulacaoLiberada_TravaFechada_FalsoSemLancar(string ambienteDeHospedagem, string? valorDaChave, AmbienteIntegracao ambienteDaIntegracao)
        {
            CaucaoFakeStore store = new CaucaoFakeStore();
            FakeCaucaoProvider provider = CriarProvider(store, ambienteDeHospedagem, valorDaChave);

            bool liberada = provider.SimulacaoLiberada(CriarConfiguracao(ambienteDaIntegracao));

            Assert.False(liberada);
            Assert.Null(store.Obter(IdentificadorDaChave()));
        }

        /// <summary>A chave e relida a cada chamada (RegrasProvedorFakePix.LerChave): desligada com a API no ar, o FAKE para de responder na hora.</summary>
        [Fact]
        public async Task Chave_ReleidaACadaChamada_DesligarFechaATravaNaHora()
        {
            IConfiguration configuracao = CriarConfiguracaoDaChave("true");
            CaucaoFakeStore store = new CaucaoFakeStore();
            FakeCaucaoProvider provider = new FakeCaucaoProvider(store, new GoogleProvaDeTeste.AmbienteFalso(Environments.Development), configuracao, new FakeLogService());
            ConfiguracaoIntegracao sandbox = CriarConfiguracao(AmbienteIntegracao.SANDBOX);

            bool antes = provider.SimulacaoLiberada(sandbox);
            configuracao[ConstantsPix.ChavePermitirProvedorFake] = "false";
            bool depois = provider.SimulacaoLiberada(sandbox);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.PreAutorizarAsync(sandbox, CriarSolicitacao(), Chave, 5));

            Assert.True(antes);
            Assert.False(depois);
            Assert.Equal(ConstantsMessage.ErroCaucaoFakeForaDoSandbox, excecao.Message);
            Assert.Null(store.Obter(IdentificadorDaChave()));
        }
    }
}
