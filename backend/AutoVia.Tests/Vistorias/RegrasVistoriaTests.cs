using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using AutoVia.Helpers;

namespace AutoVia.Tests.Vistorias;

public class RegrasVistoriaTests
{
    [Theory]
    [InlineData(StatusLocacao.EM_ANDAMENTO, true)]
    [InlineData(StatusLocacao.ATRASADA, true)]
    [InlineData(StatusLocacao.FINALIZADA, false)]
    [InlineData(StatusLocacao.CANCELADA, false)]
    public void LocacaoAceitaVistoriaSomenteQuandoEstaEmAberto(StatusLocacao status, bool esperado)
    {
        Assert.Equal(esperado, RegrasVistoria.LocacaoAceitaVistoria(status));
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(101, true)]
    public void KmDaEntradaNaoPodeFicarAbaixoDoKmDeSaida(int kmEntrada, bool esperado)
    {
        Assert.Equal(esperado, RegrasVistoria.KmEntradaValido(kmEntrada, 100));
    }

    [Fact]
    public void AvariaEmVistoriaDeSaidaEhRecusadaComMensagemPropria()
    {
        Assert.Equal(ConstantsMessage.ErroAvariaVistoriaSaida, RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.SAIDA, StatusLocacao.EM_ANDAMENTO, false));
    }

    [Fact]
    public void AvariaNaEntradaComSaidaAtivaEhRecusadaComMensagemPropria()
    {
        Assert.Equal(ConstantsMessage.ErroAvariaEntradaAposSaida, RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.ENTRADA, StatusLocacao.EM_ANDAMENTO, true));
    }

    [Theory]
    [InlineData(StatusLocacao.FINALIZADA)]
    [InlineData(StatusLocacao.CANCELADA)]
    public void AvariaComLocacaoFechadaEhRecusadaComMensagemPropria(StatusLocacao status)
    {
        Assert.Equal(ConstantsMessage.ErroAvariaLocacaoFechada, RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.ENTRADA, status, false));
    }

    [Theory]
    [InlineData(StatusLocacao.EM_ANDAMENTO)]
    [InlineData(StatusLocacao.ATRASADA)]
    public void AvariaNaEntradaSemSaidaComLocacaoEmAbertoEhLiberada(StatusLocacao status)
    {
        Assert.Null(RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.ENTRADA, status, false));
    }

    [Fact]
    public void VistoriaDeSaidaTemPrecedenciaSobreStatusDaLocacao()
    {
        Assert.Equal(ConstantsMessage.ErroAvariaVistoriaSaida, RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.SAIDA, StatusLocacao.FINALIZADA, false));
    }

    [Fact]
    public void SaidaAtivaTemPrecedenciaSobreStatusDaLocacaoNaEntrada()
    {
        Assert.Equal(ConstantsMessage.ErroAvariaEntradaAposSaida, RegrasVistoria.MotivoRecusaAvaria(TipoVistoria.ENTRADA, StatusLocacao.FINALIZADA, true));
    }
}
