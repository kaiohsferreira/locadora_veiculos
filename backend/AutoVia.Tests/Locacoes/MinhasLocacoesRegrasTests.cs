using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Regras puras da lista do cliente: situacao para status e atraso nos limites (sem banco e sem relogio).</summary>
    public class MinhasLocacoesRegrasTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 26, 18, 0, 0);

        [Fact]
        public void SituacaoEmAndamento_DevolveEmAndamentoEAtrasada()
        {
            IReadOnlyCollection<StatusLocacao> status = RegrasMinhasLocacoes.SituacaoParaStatus(ConstantsValues.SituacaoMinhasLocacoesEmAndamento);

            Assert.Equal(2, status.Count);
            Assert.Contains(StatusLocacao.EM_ANDAMENTO, status);
            Assert.Contains(StatusLocacao.ATRASADA, status);
        }

        [Fact]
        public void SituacaoHistorico_DevolveFinalizadaECancelada()
        {
            IReadOnlyCollection<StatusLocacao> status = RegrasMinhasLocacoes.SituacaoParaStatus(ConstantsValues.SituacaoMinhasLocacoesHistorico);

            Assert.Equal(2, status.Count);
            Assert.Contains(StatusLocacao.FINALIZADA, status);
            Assert.Contains(StatusLocacao.CANCELADA, status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SituacaoAusenteOuVazia_ValeEmAndamento(string? situacao)
        {
            IReadOnlyCollection<StatusLocacao> status = RegrasMinhasLocacoes.SituacaoParaStatus(situacao);

            Assert.Contains(StatusLocacao.EM_ANDAMENTO, status);
            Assert.Contains(StatusLocacao.ATRASADA, status);
            Assert.DoesNotContain(StatusLocacao.FINALIZADA, status);
        }

        [Fact]
        public void Atraso_ExatamenteNaHora_NaoEAtrasada()
        {
            Assert.False(RegrasMinhasLocacoes.EstaAtrasada(Agora, Agora, StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public void Atraso_UmSegundoAntes_EAtrasada()
        {
            Assert.True(RegrasMinhasLocacoes.EstaAtrasada(Agora.AddSeconds(-1), Agora, StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public void Atraso_UmSegundoDepois_NaoEAtrasada()
        {
            Assert.False(RegrasMinhasLocacoes.EstaAtrasada(Agora.AddSeconds(1), Agora, StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public void Atraso_ViradaDoDia_DevolucaoAntesDaMeiaNoitePassaComUmSegundo()
        {
            DateTime devolucao = new DateTime(2026, 11, 25, 23, 59, 59);
            DateTime meiaNoite = new DateTime(2026, 11, 26, 0, 0, 0);

            Assert.True(RegrasMinhasLocacoes.EstaAtrasada(devolucao, meiaNoite, StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public void Atraso_ViradaDoDia_DevolucaoNaMeiaNoiteAindaNaoPassou()
        {
            DateTime devolucao = new DateTime(2026, 11, 26, 0, 0, 0);
            DateTime meiaNoite = new DateTime(2026, 11, 26, 0, 0, 0);

            Assert.False(RegrasMinhasLocacoes.EstaAtrasada(devolucao, meiaNoite, StatusLocacao.EM_ANDAMENTO));
        }

        [Fact]
        public void Atraso_StatusAtrasadaComDataFutura_EAtrasada()
        {
            Assert.True(RegrasMinhasLocacoes.EstaAtrasada(Agora.AddDays(3), Agora, StatusLocacao.ATRASADA));
        }
    }
}
