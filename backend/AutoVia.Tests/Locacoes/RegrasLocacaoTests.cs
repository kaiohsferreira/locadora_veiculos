using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.RepositoriesInterface;
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

            Locacao locacao = RegrasLocacao.CriarLocacaoDeAbertura(reserva, veiculo, 20, DataRetiradaDeTeste, MomentoDeTeste, null);

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
            Assert.Empty(locacao.Pagamentos);
        }

        /// <summary>
        /// Abertura com caucao (contrato caucao-retirada v1.1, CE-2): valor_caucao = valor da caucao e exatamente uma linha CAUCAO
        /// em Pagamentos, com as colunas da linha_caucao. O id_locacao vem pela navegacao na gravacao (aqui ainda nulo).
        /// </summary>
        [Fact]
        public void CriarLocacaoDeAbertura_ComCaucao_ValorDaCaucaoEUmaUnicaLinhaCaucao()
        {
            Reserva reserva = new Reserva
            {
                IdReserva = 42,
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
            CaucaoAberturaRegistro caucao = new CaucaoAberturaRegistro(1200.00m, "FAKE-CAU-0123456789ab", "CAUCAO-RES-42");

            Locacao locacao = RegrasLocacao.CriarLocacaoDeAbertura(reserva, veiculo, 20, DataRetiradaDeTeste, MomentoDeTeste, caucao);

            Assert.Equal(1200.00m, locacao.ValorCaucao);
            Assert.Equal(StatusLocacao.EM_ANDAMENTO, locacao.Status);

            Pagamento pagamento = Assert.Single(locacao.Pagamentos);
            Assert.Equal(TipoPagamento.CAUCAO, pagamento.Tipo);
            Assert.Null(pagamento.IdLocacao);
            Assert.Null(pagamento.IdVenda);
            Assert.Equal(1200.00m, pagamento.Valor);
            Assert.Equal("CARTAO", pagamento.Forma);
            Assert.Equal(StatusPagamento.PENDENTE, pagamento.Status);
            Assert.Equal("FAKE-CAU-0123456789ab", pagamento.IdTransacao);
            Assert.Equal(DateOnly.FromDateTime(DataDevolucaoDeTeste), pagamento.DataVencimento);
            Assert.Null(pagamento.DataPagamento);
            Assert.Null(pagamento.QrCodeBase64);
            Assert.Null(pagamento.PixCopiaECola);
            Assert.Null(pagamento.PixExpiraEm);
            Assert.Equal(MomentoDeTeste, pagamento.CreatedAt);
            Assert.Equal(MomentoDeTeste, pagamento.UpdatedAt);
            Assert.Null(pagamento.DisabledAt);
        }

        [Fact]
        public void CnhVencida_ValidadeOntem_EhVencida()
        {
            bool vencida = RegrasLocacao.CnhVencida(new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 24));

            Assert.True(vencida);
        }

        [Fact]
        public void CnhVencida_ValidadeHoje_AindaVale()
        {
            bool vencida = RegrasLocacao.CnhVencida(new DateOnly(2026, 11, 24), new DateOnly(2026, 11, 24));

            Assert.False(vencida);
        }

        [Fact]
        public void CnhVencida_ValidadeAmanha_NaoEhVencida()
        {
            bool vencida = RegrasLocacao.CnhVencida(new DateOnly(2026, 11, 25), new DateOnly(2026, 11, 24));

            Assert.False(vencida);
        }

        [Fact]
        public void EnderecoLocadora_TodasAsPartes_NoFormatoDaTela29()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("Rua das Flores", "100", "Centro", "Campinas", "SP");

            Assert.Equal("Rua das Flores, 100 - Centro, Campinas - SP", endereco);
        }

        [Fact]
        public void EnderecoLocadora_SemNumero_NaoSobraSeparador()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("Rua das Flores", "", "Centro", "Campinas", "SP");

            Assert.Equal("Rua das Flores - Centro, Campinas - SP", endereco);
        }

        [Fact]
        public void EnderecoLocadora_SemBairro_NaoSobraTraco()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("Rua das Flores", "100", "", "Campinas", "SP");

            Assert.Equal("Rua das Flores, 100, Campinas - SP", endereco);
        }

        [Fact]
        public void EnderecoLocadora_SoCidadeEUf()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("", "", "", "Campinas", "SP");

            Assert.Equal("Campinas - SP", endereco);
        }

        [Fact]
        public void EnderecoLocadora_TudoVazio_TextoVazio()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("", "", "", "", "");

            Assert.Equal(string.Empty, endereco);
        }

        [Fact]
        public void EnderecoLocadora_PartesComEspacoNasPontas_SaemAparadas()
        {
            string endereco = RegrasLocacao.MontarEnderecoLocadora("  Rua das Flores  ", " 100 ", "   ", " Campinas ", " SP ");

            Assert.Equal("Rua das Flores, 100, Campinas - SP", endereco);
        }
    }
}
