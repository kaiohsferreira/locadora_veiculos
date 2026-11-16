using AutoVia.Communication.Enums;
using AutoVia.Core.Regras;
using System;
using Xunit;

namespace AutoVia.Tests.Reservas
{
    /// <summary>Funcoes puras da reserva (RegrasReserva), testadas direto, sem banco e sem servico.</summary>
    public class RegrasReservaTests
    {
        [Theory]
        [InlineData(StatusVeiculo.DISPONIVEL, true)]
        [InlineData(StatusVeiculo.RESERVADO, true)]
        [InlineData(StatusVeiculo.LOCADO, false)]
        [InlineData(StatusVeiculo.MANUTENCAO, false)]
        [InlineData(StatusVeiculo.VENDIDO, false)]
        [InlineData(StatusVeiculo.BAIXADO, false)]
        public void VeiculoAceitaReserva_SoDisponivelOuReservado(StatusVeiculo status, bool esperado)
        {
            Assert.Equal(esperado, RegrasReserva.VeiculoAceitaReserva(status));
        }

        [Theory]
        [InlineData(StatusVeiculo.DISPONIVEL, false, StatusVeiculo.DISPONIVEL)]
        [InlineData(StatusVeiculo.DISPONIVEL, true, StatusVeiculo.RESERVADO)]
        [InlineData(StatusVeiculo.RESERVADO, false, StatusVeiculo.DISPONIVEL)]
        [InlineData(StatusVeiculo.RESERVADO, true, StatusVeiculo.RESERVADO)]
        [InlineData(StatusVeiculo.LOCADO, true, StatusVeiculo.LOCADO)]
        [InlineData(StatusVeiculo.LOCADO, false, StatusVeiculo.LOCADO)]
        [InlineData(StatusVeiculo.MANUTENCAO, false, StatusVeiculo.MANUTENCAO)]
        [InlineData(StatusVeiculo.VENDIDO, true, StatusVeiculo.VENDIDO)]
        [InlineData(StatusVeiculo.BAIXADO, false, StatusVeiculo.BAIXADO)]
        public void StatusDoVeiculoAposCancelamento_RecalculaSoDisponivelOuReservado(StatusVeiculo atual, bool haOutra, StatusVeiculo esperado)
        {
            Assert.Equal(esperado, RegrasReserva.StatusDoVeiculoAposCancelamento(atual, haOutra));
        }

        [Fact]
        public void CalcularDiarias_PelaDiferencaDeDayNumber()
        {
            Assert.Equal(5, RegrasReserva.CalcularDiarias(new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 15)));
        }

        [Fact]
        public void CalcularDiarias_ViradaDeMesEDeAno()
        {
            Assert.Equal(4, RegrasReserva.CalcularDiarias(new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 18)));
            Assert.Equal(3, RegrasReserva.CalcularDiarias(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2)));
        }

        [Fact]
        public void CalcularValorEstimado_DiariasVezesDiaria()
        {
            Assert.Equal(602.00m, RegrasReserva.CalcularValorEstimado(4, 150.50m));
        }

        [Theory]
        [InlineData("AV-00042", 42)]
        [InlineData("av-42", 42)]
        [InlineData("#AV-00042", 42)]
        [InlineData("42", 42)]
        [InlineData("  42  ", 42)]
        [InlineData("208", 208)]
        public void LerCodigoDaBusca_CasaSoPeloCodigoQuandoHaNumero(string busca, int esperado)
        {
            Assert.Equal(esperado, RegrasReserva.LerCodigoDaBusca(busca));
        }

        [Theory]
        [InlineData("renegade")]
        [InlineData("RENE")]
        [InlineData("jeep renegade")]
        [InlineData("AV-")]
        [InlineData("99999999999")]
        [InlineData("AV-99999999999")]
        [InlineData("%")]
        public void LerCodigoDaBusca_NulosQuandoNaoHaCodigo(string busca)
        {
            Assert.Null(RegrasReserva.LerCodigoDaBusca(busca));
        }

        [Theory]
        [InlineData("AV-00042", null)]
        [InlineData("#AV-00042", null)]
        [InlineData("42", "42")]
        [InlineData("208", "208")]
        [InlineData("renegade", "renegade")]
        [InlineData("RENE", "rene")]
        [InlineData("Jeep Renegade", "jeep renegade")]
        [InlineData("AV-", "av-")]
        [InlineData("99999999999", "99999999999")]
        [InlineData("AV-99999999999", "av-99999999999")]
        [InlineData("%", "%")]
        [InlineData("42a", "42a")]
        public void LerTextoDaBusca_TermoLiteralEmMinusculasFora_DoCodigo(string busca, string? esperado)
        {
            Assert.Equal(esperado, RegrasReserva.LerTextoDaBusca(busca));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("#")]
        public void BuscaAusenteOuVazia_NaoCasaNemCodigoNemTexto(string? busca)
        {
            Assert.Null(RegrasReserva.LerCodigoDaBusca(busca));
            Assert.Null(RegrasReserva.LerTextoDaBusca(busca));
        }

        [Fact]
        public void FormateCodigoReserva_ComCincoDigitosNoMinimo()
        {
            Assert.Equal("AV-00042", RegrasReserva.FormateCodigoReserva(42));
            Assert.Equal("AV-123456", RegrasReserva.FormateCodigoReserva(123456));
        }
    }
}
