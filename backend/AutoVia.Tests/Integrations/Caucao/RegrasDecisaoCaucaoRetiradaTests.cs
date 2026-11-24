using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;

namespace AutoVia.Tests.Integrations.Caucao
{
    /// <summary>
    /// Decisao da abertura sobre o estado da caucao no provedor (contrato caucao-retirada v1.1, correcao c): so RESERVADA abre;
    /// so EM_ANALISE e cancelada antes de recusar; EM_ANALISE tem a mensagem propria e os demais pedem nova tentativa.
    /// </summary>
    public class RegrasDecisaoCaucaoRetiradaTests
    {
        [Theory]
        [InlineData(CaucaoStatus.RESERVADA, true)]
        [InlineData(CaucaoStatus.EM_ANALISE, false)]
        [InlineData(CaucaoStatus.RECUSADA, false)]
        [InlineData(CaucaoStatus.CAPTURADA, false)]
        [InlineData(CaucaoStatus.CANCELADA, false)]
        [InlineData(CaucaoStatus.EXPIRADA, false)]
        [InlineData(CaucaoStatus.ESTORNADA, false)]
        public void PermiteAbrir_SoReservada(CaucaoStatus status, bool esperado)
        {
            bool permite = RegrasDecisaoCaucaoRetirada.PermiteAbrir(status);

            Assert.Equal(esperado, permite);
        }

        [Theory]
        [InlineData(CaucaoStatus.RESERVADA, false)]
        [InlineData(CaucaoStatus.EM_ANALISE, true)]
        [InlineData(CaucaoStatus.RECUSADA, false)]
        [InlineData(CaucaoStatus.CAPTURADA, false)]
        [InlineData(CaucaoStatus.CANCELADA, false)]
        [InlineData(CaucaoStatus.EXPIRADA, false)]
        [InlineData(CaucaoStatus.ESTORNADA, false)]
        public void CancelarAntesDeRecusar_SoEmAnalise(CaucaoStatus status, bool esperado)
        {
            bool cancelar = RegrasDecisaoCaucaoRetirada.CancelarAntesDeRecusar(status);

            Assert.Equal(esperado, cancelar);
        }

        [Fact]
        public void MensagemDaRecusa_EmAnalise_MensagemPropriaDeAnalise()
        {
            string mensagem = RegrasDecisaoCaucaoRetirada.MensagemDaRecusa(CaucaoStatus.EM_ANALISE);

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoEmAnalise, mensagem);
        }

        [Theory]
        [InlineData(CaucaoStatus.RECUSADA)]
        [InlineData(CaucaoStatus.CAPTURADA)]
        [InlineData(CaucaoStatus.CANCELADA)]
        [InlineData(CaucaoStatus.EXPIRADA)]
        [InlineData(CaucaoStatus.ESTORNADA)]
        public void MensagemDaRecusa_EstadosEncerrados_PedemNovaTentativa(CaucaoStatus status)
        {
            string mensagem = RegrasDecisaoCaucaoRetirada.MensagemDaRecusa(status);

            Assert.Equal(ConstantsMessage.ErroLocacaoCaucaoNovaTentativa, mensagem);
        }

        /// <summary>Guarda: se o gateway ganhar um estado novo, a decisao precisa ser revista (hoje sao 7, e so um abre).</summary>
        [Fact]
        public void TodosOsEstados_SoUmAbre()
        {
            CaucaoStatus[] estados = Enum.GetValues<CaucaoStatus>();
            int quantosAbrem = estados.Count(x => RegrasDecisaoCaucaoRetirada.PermiteAbrir(x));
            int quantosCancelam = estados.Count(x => RegrasDecisaoCaucaoRetirada.CancelarAntesDeRecusar(x));

            Assert.Equal(7, estados.Length);
            Assert.Equal(1, quantosAbrem);
            Assert.Equal(1, quantosCancelam);
        }
    }
}
