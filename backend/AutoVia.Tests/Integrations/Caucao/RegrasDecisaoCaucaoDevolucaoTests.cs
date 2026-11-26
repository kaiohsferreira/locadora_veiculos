using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers.Caucao;
using Xunit;

namespace AutoVia.Tests.Integrations.Caucao;

/// <summary>
/// Tabela C1 a C14 da caucao no encerramento (caucao-devolucao v4). R = valor reservado 1000, T = total das extras.
/// </summary>
public class RegrasDecisaoCaucaoDevolucaoTests
{
    private const decimal R = 1000m;

    /// <summary>C1 (T = 0) e C4 (T maior que R) cancelam; C2 (0 &lt; T &lt; R) e C3 (T = R) capturam.</summary>
    [Theory]
    [InlineData(0, AcaoProvedorCaucao.CANCELAR)]
    [InlineData(300, AcaoProvedorCaucao.CAPTURAR)]
    [InlineData(1000, AcaoProvedorCaucao.CAPTURAR)]
    [InlineData(1500, AcaoProvedorCaucao.CANCELAR)]
    public void AcaoNoProvedor_ReservadaCapturaSoQuandoTCabeNoReservado(decimal t, AcaoProvedorCaucao esperada)
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(CaucaoStatus.RESERVADA, R, t);

        Assert.Equal(esperada, acao);
    }

    [Theory]
    [InlineData(CaucaoStatus.CAPTURADA)]
    [InlineData(CaucaoStatus.CANCELADA)]
    [InlineData(CaucaoStatus.EXPIRADA)]
    [InlineData(CaucaoStatus.RECUSADA)]
    [InlineData(CaucaoStatus.EM_ANALISE)]
    [InlineData(CaucaoStatus.ESTORNADA)]
    public void AcaoNoProvedor_EstadosQueNaoRecebemAcaoNaoPedemAcao(CaucaoStatus status)
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(status, R, 300m);

        Assert.Equal(AcaoProvedorCaucao.NENHUMA, acao);
    }

    [Fact]
    public void C2_CapturaParcialRetemSoAsExtrasELiberaORestante()
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(CaucaoStatus.RESERVADA, R, 300m);
        Assert.Equal(AcaoProvedorCaucao.CAPTURAR, acao);
        Assert.Equal(300m, RegrasDecisaoCaucaoDevolucao.ValorDaCaptura(300m));

        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CAPTURADA, 300m, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL, decisao.Acao);
        Assert.Equal(StatusPagamento.PAGO, decisao.NovoStatusCaucao);
        Assert.Equal(300m, decisao.ValorRetido);
        Assert.Equal(700m, decisao.ValorLiberado);
        Assert.True(decisao.QuitarAlvo);
        Assert.True(decisao.DataHojeNaCaucao);
        Assert.Null(decisao.MensagemAviso);
    }

    [Fact]
    public void C3_ExtrasIgualAoReservadoRetemTudo()
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(CaucaoStatus.RESERVADA, R, R);
        Assert.Equal(AcaoProvedorCaucao.CAPTURAR, acao);

        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CAPTURADA, R, R, R);

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA, decisao.Acao);
        Assert.Equal(R, decisao.ValorRetido);
        Assert.Equal(0m, decisao.ValorLiberado);
        Assert.True(decisao.QuitarAlvo);
    }

    [Fact]
    public void C1_SemExtrasBaixaEDevolveTudo()
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(CaucaoStatus.RESERVADA, R, 0m);
        Assert.Equal(AcaoProvedorCaucao.CANCELAR, acao);

        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CANCELADA, null, R, 0m);

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA, decisao.Acao);
        Assert.Equal(StatusPagamento.CANCELADO, decisao.NovoStatusCaucao);
        Assert.Equal(R, decisao.ValorLiberado);
        Assert.Null(decisao.MensagemAviso);
    }

    [Fact]
    public void C4_ExtrasAcimaDoReservadoCancelaEAvisaQueFicouParaPix()
    {
        AcaoProvedorCaucao acao = RegrasDecisaoCaucaoDevolucao.AcaoNoProvedor(CaucaoStatus.RESERVADA, R, 1200m);
        Assert.Equal(AcaoProvedorCaucao.CANCELAR, acao);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoExtrasAcimaDaCaucao, RegrasDecisaoCaucaoDevolucao.AvisoDaAcao(acao, R, 1200m));

        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CANCELADA, null, R, 1200m);

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA, decisao.Acao);
        Assert.False(decisao.QuitarAlvo);
    }

    [Fact]
    public void C5_CapturadaComOValorDeTEhRetencaoQuandoJaFoiFeitaAntesDeUmaFalhaDeBanco()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CAPTURADA, 300m, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.RETIDA_PARCIAL, decisao.Acao);
        Assert.True(decisao.QuitarAlvo);
        Assert.False(decisao.TratarAMao);
    }

    [Fact]
    public void C6_CapturadaComValorDiferenteDeTEncerraComoNaoConfirmadaETratarAMao()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CAPTURADA, 500m, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR, decisao.Acao);
        Assert.Null(decisao.NovoStatusCaucao);
        Assert.True(decisao.TratarAMao);
        Assert.False(decisao.QuitarAlvo);
        Assert.Equal(0m, decisao.ValorRetido);
        Assert.Equal(0m, decisao.ValorLiberado);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoCaucaoDivergente, decisao.MensagemAviso);
    }

    [Fact]
    public void C7_CanceladaBaixaSemAviso()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.CANCELADA, null, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA, decisao.Acao);
        Assert.Null(decisao.MensagemAviso);
        Assert.False(decisao.TratarAMao);
    }

    [Theory]
    [InlineData(CaucaoStatus.EXPIRADA)]
    [InlineData(CaucaoStatus.RECUSADA)]
    [InlineData(CaucaoStatus.ESTORNADA)]
    public void C8_C9_C10_ExpiradaRecusadaOuEstornadaBaixamComAvisoSemGarantia(CaucaoStatus status)
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(status, null, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.BAIXADA, decisao.Acao);
        Assert.Equal(StatusPagamento.CANCELADO, decisao.NovoStatusCaucao);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoCaucaoSemGarantia, decisao.MensagemAviso);
        Assert.False(decisao.TratarAMao);
        Assert.False(decisao.QuitarAlvo);
    }

    [Fact]
    public void C10_EstornadaNaoRegravaADataDaCaucao()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.ESTORNADA, null, R, 300m);

        Assert.False(decisao.DataHojeNaCaucao);
    }

    [Fact]
    public void C11_EmAnaliseFicaTratarAMao()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.EM_ANALISE, null, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR, decisao.Acao);
        Assert.True(decisao.TratarAMao);
        Assert.Null(decisao.NovoStatusCaucao);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoCaucaoEmAnalise, decisao.MensagemAviso);
    }

    [Fact]
    public void C12_NaoEncontradaNoProvedorEncerraComoNaoConfirmada()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(null, null, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR, decisao.Acao);
        Assert.True(decisao.TratarAMao);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoCaucaoNaoEncontrada, decisao.MensagemAviso);
    }

    [Fact]
    public void C14_ReservadaDepoisDeUmaFalhaNaoEhResultadoFinal()
    {
        DecisaoCaucaoEncerramento decisao = RegrasDecisaoCaucaoDevolucao.Decidir(CaucaoStatus.RESERVADA, null, R, 300m);

        Assert.Equal(AcaoCaucaoDevolucao.NAO_CONFIRMADA_NO_PROVEDOR, decisao.Acao);
        Assert.True(decisao.TratarAMao);
        Assert.Equal(ConstantsMessage.AvisoDevolucaoCaucaoDivergente, decisao.MensagemAviso);
    }
}
