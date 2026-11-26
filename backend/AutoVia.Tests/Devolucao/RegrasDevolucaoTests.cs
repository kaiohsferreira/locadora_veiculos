using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using System;
using Xunit;

namespace AutoVia.Tests.Devolucao;

/// <summary>Regras puras do encerramento (RegrasDevolucao), sem banco e sem relogio.</summary>
public class RegrasDevolucaoTests
{
    [Theory]
    [InlineData(StatusLocacao.EM_ANDAMENTO, true)]
    [InlineData(StatusLocacao.ATRASADA, true)]
    [InlineData(StatusLocacao.FINALIZADA, false)]
    [InlineData(StatusLocacao.CANCELADA, false)]
    public void LocacaoPodeEncerrar_SoEmAndamentoOuAtrasada(StatusLocacao status, bool esperado)
    {
        bool pode = RegrasDevolucao.LocacaoPodeEncerrar(status);

        Assert.Equal(esperado, pode);
    }

    [Theory]
    [InlineData(false, StatusVeiculo.DISPONIVEL)]
    [InlineData(true, StatusVeiculo.RESERVADO)]
    public void StatusVeiculoAposDevolucao_LocadoVaiParaDisponivelOuReservadoConformeOutraReserva(bool haOutraReserva, StatusVeiculo esperado)
    {
        StatusVeiculo? status = RegrasDevolucao.StatusVeiculoAposDevolucao(StatusVeiculo.LOCADO, haOutraReserva);

        Assert.Equal(esperado, status);
    }

    [Theory]
    [InlineData(StatusVeiculo.DISPONIVEL)]
    [InlineData(StatusVeiculo.RESERVADO)]
    [InlineData(StatusVeiculo.MANUTENCAO)]
    [InlineData(StatusVeiculo.VENDIDO)]
    [InlineData(StatusVeiculo.BAIXADO)]
    public void StatusVeiculoAposDevolucao_OutroStatusEhRecusaDefensiva(StatusVeiculo atual)
    {
        StatusVeiculo? status = RegrasDevolucao.StatusVeiculoAposDevolucao(atual, false);

        Assert.Null(status);
    }

    [Fact]
    public void CalcularDiarias_UsaADataDaRetiradaRealAteADevolucaoPrevista()
    {
        DateTime retirada = new DateTime(2026, 11, 18, 10, 0, 0);
        DateTime previsao = new DateTime(2026, 11, 20, 0, 0, 0);

        int diarias = RegrasDevolucao.CalcularDiarias(retirada, previsao);

        Assert.Equal(2, diarias);
    }

    [Fact]
    public void CalcularValorTotal_SomaDiariasEExtrasComArredondamentoParaCima()
    {
        decimal total = RegrasDevolucao.CalcularValorTotal(3, 100.005m, 0.10m);

        Assert.Equal(300.12m, total);
    }

    [Fact]
    public void CalcularValorTotal_SemDiariasEComExtrasDaOTotalDasExtras()
    {
        decimal total = RegrasDevolucao.CalcularValorTotal(0, 0m, 250m);

        Assert.Equal(250m, total);
    }

    [Theory]
    [InlineData(1, 100, true)]
    [InlineData(0, 100, false)]
    [InlineData(2, 0, false)]
    public void DiariaDeveLancar_SoComDiariasEValorMaioresQueZero(int diarias, decimal valorDiaria, bool esperado)
    {
        bool deve = RegrasDevolucao.DiariaDeveLancar(diarias, valorDiaria);

        Assert.Equal(esperado, deve);
    }

    [Fact]
    public void TotalConfere_ArredondaOTotalConfirmadoAntesDeComparar()
    {
        Assert.True(RegrasDevolucao.TotalConfere(300.125m, 300.13m));
        Assert.False(RegrasDevolucao.TotalConfere(300.12m, 300.13m));
    }

    [Fact]
    public void ValorLiberadoDaCaucao_E_OReservadoMenosORetido()
    {
        Assert.Equal(700m, RegrasDevolucao.ValorLiberadoDaCaucao(1000m, 300m));
        Assert.Equal(0m, RegrasDevolucao.ValorLiberadoDaCaucao(1000m, 1000m));
    }

    [Fact]
    public void ValorDaDiaria_MultiplicaEArredondaADuasCasas()
    {
        Assert.Equal(201.00m, RegrasDevolucao.ValorDaDiaria(2, 100.50m));
    }

    [Theory]
    [InlineData(StatusPagamento.PENDENTE, true)]
    [InlineData(StatusPagamento.ATRASADO, true)]
    [InlineData(StatusPagamento.PAGO, false)]
    [InlineData(StatusPagamento.CANCELADO, false)]
    public void AlvoRetencaoEmAberto_SoPendenteOuAtrasado(StatusPagamento status, bool esperado)
    {
        bool aberto = RegrasDevolucao.AlvoRetencaoEmAberto(status);

        Assert.Equal(esperado, aberto);
    }
}
