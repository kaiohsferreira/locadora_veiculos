using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using System;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>Regra pura da abertura de locacao, testada direto (sem repositorio nem fake).</summary>
    public class RegrasLocacaoTests
    {
        private static readonly DateTime DataRetiradaDeTeste = new DateTime(2026, 11, 19, 9, 30, 0);
        private static readonly DateTime MomentoDeTeste = new DateTime(2026, 11, 19, 9, 30, 5);
        private static readonly DateTime DataDevolucaoDeTeste = new DateTime(2026, 11, 20, 0, 0, 0);

        [Theory]
        [InlineData(StatusReserva.ATIVA, true)]
        [InlineData(StatusReserva.CONFIRMADA, true)]
        [InlineData(StatusReserva.CANCELADA, false)]
        [InlineData(StatusReserva.CONCLUIDA, false)]
        public void ReservaPermiteAbrirLocacao_SoAtivaEConfirmada(StatusReserva status, bool esperado)
        {
            bool permite = RegrasLocacao.ReservaPermiteAbrirLocacao(status);

            Assert.Equal(esperado, permite);
        }

        [Theory]
        [InlineData(StatusVeiculo.RESERVADO, true)]
        [InlineData(StatusVeiculo.DISPONIVEL, false)]
        [InlineData(StatusVeiculo.LOCADO, false)]
        [InlineData(StatusVeiculo.MANUTENCAO, false)]
        [InlineData(StatusVeiculo.VENDIDO, false)]
        [InlineData(StatusVeiculo.BAIXADO, false)]
        public void VeiculoPermiteAbrirLocacao_SoReservado(StatusVeiculo status, bool esperado)
        {
            bool permite = RegrasLocacao.VeiculoPermiteAbrirLocacao(status);

            Assert.Equal(esperado, permite);
        }

        [Fact]
        public void TransicoesDaAbertura_ReservaVaiParaConcluidaEVeiculoParaLocado()
        {
            StatusReserva statusReserva = RegrasLocacao.StatusReservaAposAbertura();
            StatusVeiculo statusVeiculo = RegrasLocacao.StatusVeiculoAposAbertura();

            Assert.Equal(StatusReserva.CONCLUIDA, statusReserva);
            Assert.Equal(StatusVeiculo.LOCADO, statusVeiculo);
        }

        [Fact]
        public void CriarLocacaoDeAbertura_KmSaidaEAtualCaucaoZeroEmAndamentoERetiradaDoServico()
        {
            Reserva reserva = new Reserva
            {
                IdReserva = 10,
                IdCliente = 30,
                IdVeiculo = 40,
                DataDevolucaoPrevista = DataDevolucaoDeTeste,
                Status = StatusReserva.ATIVA
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40,
                KmAtual = 1234,
                ValorDiaria = 175m,
                Status = StatusVeiculo.RESERVADO
            };

            Locacao locacao = RegrasLocacao.CriarLocacaoDeAbertura(reserva, veiculo, 20, DataRetiradaDeTeste, MomentoDeTeste);

            Assert.Equal<int?>(10, locacao.IdReserva);
            Assert.Equal(30, locacao.IdCliente);
            Assert.Equal(40, locacao.IdVeiculo);
            Assert.Equal(20, locacao.IdFuncionario);
            Assert.Equal(DataRetiradaDeTeste, locacao.DataRetirada);
            Assert.Equal(DataDevolucaoDeTeste, locacao.DataDevolucaoPrevista);
            Assert.Equal(1234, locacao.KmSaida);
            Assert.Equal(175m, locacao.ValorDiaria);
            Assert.Equal(0m, locacao.ValorCaucao);
            Assert.Equal(StatusLocacao.EM_ANDAMENTO, locacao.Status);
            Assert.Equal(MomentoDeTeste, locacao.CreatedAt);
            Assert.Equal(MomentoDeTeste, locacao.UpdatedAt);
            Assert.Null(locacao.DisabledAt);
        }
    }
}
