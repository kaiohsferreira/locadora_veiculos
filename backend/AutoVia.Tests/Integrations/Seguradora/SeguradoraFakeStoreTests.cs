using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Seguradora;
using AutoVia.Integrations.Helpers.Seguradora;
using AutoVia.Integrations.Services.Seguradora;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Integrations.Seguradora
{
    /// <summary>
    /// Testes do SeguradoraFakeStore e das TransicoesSinistro: so os caminhos validos do RF-43
    /// (ABERTO -> EM_ANALISE -> APROVADO|NEGADO -> ENCERRADO), protocolo inexistente, copias
    /// independentes do que esta guardado e a data dos eventos vinda do relogio (IRelogio).
    /// </summary>
    public class SeguradoraFakeStoreTests
    {
        /// <summary>Relogio fixo dos testes do armazenamento.</summary>
        private static readonly DateTime AgoraFixa = new DateTime(2026, 11, 16, 9, 0, 0);

        private static SeguradoraFakeStore CriarStore()
        {
            return new SeguradoraFakeStore(new FakeRelogio(AgoraFixa));
        }

        [Theory]
        [InlineData(StatusSinistro.ABERTO, StatusSinistro.EM_ANALISE, true)]
        [InlineData(StatusSinistro.EM_ANALISE, StatusSinistro.APROVADO, true)]
        [InlineData(StatusSinistro.EM_ANALISE, StatusSinistro.NEGADO, true)]
        [InlineData(StatusSinistro.APROVADO, StatusSinistro.ENCERRADO, true)]
        [InlineData(StatusSinistro.NEGADO, StatusSinistro.ENCERRADO, true)]
        [InlineData(StatusSinistro.ABERTO, StatusSinistro.APROVADO, false)]
        [InlineData(StatusSinistro.ABERTO, StatusSinistro.ENCERRADO, false)]
        [InlineData(StatusSinistro.EM_ANALISE, StatusSinistro.ABERTO, false)]
        [InlineData(StatusSinistro.APROVADO, StatusSinistro.NEGADO, false)]
        [InlineData(StatusSinistro.ENCERRADO, StatusSinistro.ABERTO, false)]
        [InlineData(StatusSinistro.ENCERRADO, StatusSinistro.ENCERRADO, false)]
        [InlineData(StatusSinistro.EM_ANALISE, StatusSinistro.EM_ANALISE, false)]
        public void Transicoes_SoOsCaminhosValidos(StatusSinistro atual, StatusSinistro novo, bool esperado)
        {
            Assert.Equal(esperado, TransicoesSinistro.Permitida(atual, novo));
        }

        [Fact]
        public void AvancarStatus_CaminhoCompletoAteEncerrado_RegistraTodosOsEventos()
        {
            SeguradoraFakeStore store = CriarStore();
            store.ObterOuCriar("P1", "SIN-1", ConstantsSeguradora.ProvedorFake);

            store.AvancarStatus("P1", StatusSinistro.EM_ANALISE, null);
            store.AvancarStatus("P1", StatusSinistro.NEGADO, "Sem cobertura");
            SinistroSeguradoraResultado final = store.AvancarStatus("P1", StatusSinistro.ENCERRADO, null);

            Assert.Equal(StatusSinistro.ENCERRADO, final.Status);
            List<StatusSinistro> caminho = final.Eventos.Select(x => x.StatusNovo).ToList();
            Assert.Equal(new List<StatusSinistro> { StatusSinistro.ABERTO, StatusSinistro.EM_ANALISE, StatusSinistro.NEGADO, StatusSinistro.ENCERRADO }, caminho);
        }

        [Fact]
        public void AvancarStatus_TransicaoInvalida_LancaBusinessExceptionENaoMudaNada()
        {
            SeguradoraFakeStore store = CriarStore();
            store.ObterOuCriar("P2", "SIN-2", ConstantsSeguradora.ProvedorFake);

            BusinessException excecao = Assert.Throws<BusinessException>(() => store.AvancarStatus("P2", StatusSinistro.APROVADO, null));

            Assert.Equal(string.Format(ConstantsMessage.ErroSeguradoraTransicaoInvalida, StatusSinistro.ABERTO, StatusSinistro.APROVADO), excecao.Message);
            SinistroSeguradoraResultado atual = store.Obter("P2")!;
            Assert.Equal(StatusSinistro.ABERTO, atual.Status);
            Assert.Single(atual.Eventos);
        }

        [Fact]
        public void AvancarStatus_ProtocoloInexistente_LancaNotFound()
        {
            SeguradoraFakeStore store = CriarStore();

            Assert.Throws<NotFoundException>(() => store.AvancarStatus("NAO-EXISTE", StatusSinistro.EM_ANALISE, null));
        }

        [Fact]
        public void DataDosEventos_VemDoRelogioNoMomentoDaChamada()
        {
            FakeRelogio relogio = new FakeRelogio(AgoraFixa);
            SeguradoraFakeStore store = new SeguradoraFakeStore(relogio);

            SinistroSeguradoraResultado aberto = store.ObterOuCriar("P4", "SIN-4", ConstantsSeguradora.ProvedorFake);
            Assert.Equal(AgoraFixa, aberto.Eventos[0].DataAlteracao);

            DateTime depois = new DateTime(2026, 11, 20, 15, 30, 0);
            relogio.Agora = depois;
            SinistroSeguradoraResultado avancado = store.AvancarStatus("P4", StatusSinistro.EM_ANALISE, null);

            Assert.Equal(depois, avancado.Eventos[1].DataAlteracao);
        }

        [Fact]
        public void Obter_DevolveCopia_AlterarResultadoNaoMudaOStore()
        {
            SeguradoraFakeStore store = CriarStore();
            SinistroSeguradoraResultado copia = store.ObterOuCriar("P3", "SIN-3", ConstantsSeguradora.ProvedorFake);

            copia.Status = StatusSinistro.ENCERRADO;
            copia.Eventos.Clear();

            SinistroSeguradoraResultado guardado = store.Obter("P3")!;
            Assert.Equal(StatusSinistro.ABERTO, guardado.Status);
            Assert.Single(guardado.Eventos);
        }
    }
}
