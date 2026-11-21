using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Email;
using AutoVia.Tests.Fakes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AutoVia.Tests.Integrations.Email
{
    public sealed class NotificacaoTransacionalServiceTests
    {
        private const string Destinatario = "titular@example.test";
        private const string OutroDestinatario = "outro@example.test";
        private const int IdReserva = 42013;
        private const int IdTitular = 21;
        private const int IdOutroCliente = 22;
        private const int IdEmpresa = 13;
        private const int IdOutraEmpresa = 14;
        private const int IdVeiculo = 11;
        private const int IdLocacao = 207;
        private const int IdVenda = 1309;
        private const int IdCobranca = 9803;
        private const decimal Valor = 837.50m;
        private static readonly DateOnly Retirada = new DateOnly(2026, 12, 10);
        private static readonly DateOnly Devolucao = new DateOnly(2026, 12, 15);
        private static readonly DateOnly Vencimento = new DateOnly(2026, 12, 30);
        private static readonly DateTime DevolucaoEfetiva = new DateTime(2026, 12, 15, 18, 35, 0);

        [Fact]
        public async Task Confirmacao_UsaTitularDaReservaPersistidaEOMesmoCodigoDaApi()
        {
            FakeReservaRepository reservas = CriarReservas();
            FakeEmailFila fila = new FakeEmailFila();
            NotificacaoTransacionalService notificacoes = new NotificacaoTransacionalService(reservas, fila, new FakeLogService());

            await notificacoes.ConfirmarReservaAsync(new ReservaCriadaEmail(IdReserva, IdTitular, IdEmpresa, Retirada, Devolucao, Valor));

            EmailMensagem mensagem = Assert.Single(fila.Snapshot());
            Assert.Equal(Destinatario, mensagem.DestinatarioEmail);
            Assert.Contains(RegrasReserva.FormateCodigoReserva(IdReserva), mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateDateToPTBR(Retirada), mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateDateToPTBR(Devolucao), mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateToMonetary(Valor), mensagem.CorpoTexto, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(IdOutroCliente, IdEmpresa)]
        [InlineData(IdTitular, IdOutraEmpresa)]
        public async Task Confirmacao_TitularOuEmpresaIncorretos_NaoEnfileira(int idCliente, int idEmpresa)
        {
            FakeEmailFila fila = new FakeEmailFila();
            NotificacaoTransacionalService notificacoes = new NotificacaoTransacionalService(CriarReservas(), fila, new FakeLogService());

            await notificacoes.ConfirmarReservaAsync(new ReservaCriadaEmail(IdReserva, idCliente, idEmpresa, Retirada, Devolucao, Valor));

            Assert.Equal(0, fila.TotalChamadas);
        }

        [Fact]
        public async Task Recibo_CarregaReferenciaDataEfetivaEValorFinal()
        {
            FakeEmailFila fila = new FakeEmailFila();
            NotificacaoTransacionalService notificacoes = new NotificacaoTransacionalService(new FakeReservaRepository(), fila, new FakeLogService());

            await notificacoes.EmitirReciboDevolucaoAsync(new LocacaoFinalizadaEmail(IdLocacao, Destinatario, DevolucaoEfetiva, Valor));

            EmailMensagem mensagem = Assert.Single(fila.Snapshot());
            Assert.Equal(Destinatario, mensagem.DestinatarioEmail);
            Assert.Contains("207", mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateDateTimeToPTBR(DevolucaoEfetiva), mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateToMonetary(Valor), mensagem.CorpoTexto, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(IdLocacao, null, "207")]
        [InlineData(null, IdVenda, "1309")]
        public async Task Cobranca_CarregaIdentificadorOrigemValorEVencimento(int? idLocacao, int? idVenda, string origem)
        {
            FakeEmailFila fila = new FakeEmailFila();
            NotificacaoTransacionalService notificacoes = new NotificacaoTransacionalService(new FakeReservaRepository(), fila, new FakeLogService());

            await notificacoes.AvisarCobrancaAsync(new CobrancaLancadaEmail(IdCobranca, idLocacao, idVenda, Destinatario, Valor, Vencimento));

            EmailMensagem mensagem = Assert.Single(fila.Snapshot());
            Assert.Equal(Destinatario, mensagem.DestinatarioEmail);
            Assert.Contains("9803", mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(origem, mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateToMonetary(Valor), mensagem.CorpoTexto, StringComparison.Ordinal);
            Assert.Contains(StaticMethods.FormateDateToPTBR(Vencimento), mensagem.CorpoTexto, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(IdLocacao, IdVenda)]
        [InlineData(0, null)]
        public async Task Cobranca_OrigemInvalida_NaoEnfileira(int? idLocacao, int? idVenda)
        {
            FakeEmailFila fila = new FakeEmailFila();
            NotificacaoTransacionalService notificacoes = new NotificacaoTransacionalService(new FakeReservaRepository(), fila, new FakeLogService());

            BusinessException erro = await Assert.ThrowsAsync<BusinessException>(() => notificacoes.AvisarCobrancaAsync(
                new CobrancaLancadaEmail(IdCobranca, idLocacao, idVenda, Destinatario, Valor, Vencimento)));

            Assert.Equal(ConstantsMessage.ErroEmailCobrancaOrigemInvalida, erro.Message);
            Assert.Equal(0, fila.TotalChamadas);
        }

        private static FakeReservaRepository CriarReservas()
        {
            FakeReservaRepository reservas = new FakeReservaRepository();
            Veiculo veiculo = new Veiculo { IdVeiculo = IdVeiculo, IdEmpresa = IdEmpresa, Status = StatusVeiculo.RESERVADO };
            reservas.VincularVeiculo(veiculo);
            reservas.VincularEmailTitular(IdTitular, Destinatario);
            reservas.VincularEmailTitular(IdOutroCliente, OutroDestinatario);
            reservas.Adicionar(new Reserva { IdReserva = IdReserva, IdCliente = IdTitular, IdVeiculo = IdVeiculo, Veiculo = veiculo });
            return reservas;
        }
    }
}
