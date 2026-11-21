using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Servico da rotina de atraso (contrato pagamento-atraso v1) com o repositorio em memoria e relogio fixo em 19/11/2026.
    /// Prova: so PENDENTE vencido sem caucao vira ATRASADO; o numero devolvido e o da atualizacao; a segunda rodada devolve zero;
    /// cobranca paga antes nao muda; falha do repositorio e relancada e registrada no log (so o numero, sem dado de cobranca).
    /// </summary>
    public class PagamentoAtrasoServiceTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 19, 10, 0, 0);
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 19);
        private const int IdVencidaDiaria = 1;
        private const int IdVencidaCaucao = 2;
        private const int IdVencendoHoje = 3;
        private const int IdFutura = 4;
        private const int IdPagaVencida = 5;
        private const int QuantidadeEsperadaUmaCobranca = 1;
        private const int QuantidadeEsperadaNenhuma = 0;

        private static Pagamento CriarCobranca(int id, TipoPagamento tipo, StatusPagamento status, DateOnly vencimento)
        {
            return new Pagamento
            {
                IdPagamento = id,
                Tipo = tipo,
                Status = status,
                Valor = 10m,
                DataVencimento = vencimento,
                CreatedAt = Agora,
                UpdatedAt = Agora
            };
        }

        private static FakePagamentoRepository CriarRepositorioComCobrancas()
        {
            FakePagamentoRepository repositorio = new FakePagamentoRepository();
            repositorio.Pagamentos.Add(CriarCobranca(IdVencidaDiaria, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje.AddDays(-1)));
            repositorio.Pagamentos.Add(CriarCobranca(IdVencidaCaucao, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, Hoje.AddDays(-1)));
            repositorio.Pagamentos.Add(CriarCobranca(IdVencendoHoje, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje));
            repositorio.Pagamentos.Add(CriarCobranca(IdFutura, TipoPagamento.COBRANCA_EXTRA, StatusPagamento.PENDENTE, Hoje.AddDays(1)));
            repositorio.Pagamentos.Add(CriarCobranca(IdPagaVencida, TipoPagamento.DIARIA, StatusPagamento.PAGO, Hoje.AddDays(-1)));

            return repositorio;
        }

        private static StatusPagamento StatusDe(FakePagamentoRepository repositorio, int id)
        {
            return repositorio.Pagamentos.Single(x => x.IdPagamento == id).Status;
        }

        [Fact]
        public async Task MarcarAtrasosDaLocadora_SoPendenteVencidoSemCaucao_MarcaEDevolveONumero()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            PagamentoAtrasoService servico = new PagamentoAtrasoService(repositorio, new FakeRelogio(Agora), new FakeLogService());

            ReturnResultadoAtrasosVO resultado = await servico.MarcarAtrasosDaLocadoraAsync();

            Assert.Equal(QuantidadeEsperadaUmaCobranca, resultado.QuantidadeAtualizada);
            Assert.Equal(StatusPagamento.ATRASADO, StatusDe(repositorio, IdVencidaDiaria));
            Assert.Equal(StatusPagamento.PENDENTE, StatusDe(repositorio, IdVencidaCaucao));
            Assert.Equal(StatusPagamento.PENDENTE, StatusDe(repositorio, IdVencendoHoje));
            Assert.Equal(StatusPagamento.PENDENTE, StatusDe(repositorio, IdFutura));
            Assert.Equal(StatusPagamento.PAGO, StatusDe(repositorio, IdPagaVencida));
        }

        [Fact]
        public async Task MarcarAtrasosDaLocadora_SegundaRodada_DevolveZero()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            PagamentoAtrasoService servico = new PagamentoAtrasoService(repositorio, new FakeRelogio(Agora), new FakeLogService());

            await servico.MarcarAtrasosDaLocadoraAsync();
            ReturnResultadoAtrasosVO segunda = await servico.MarcarAtrasosDaLocadoraAsync();

            Assert.Equal(QuantidadeEsperadaNenhuma, segunda.QuantidadeAtualizada);
        }

        [Fact]
        public async Task MarcarAtrasosDaLocadora_CobrancaPagaAntes_NaoMuda()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            repositorio.Pagamentos.Single(x => x.IdPagamento == IdVencidaDiaria).Status = StatusPagamento.PAGO;
            PagamentoAtrasoService servico = new PagamentoAtrasoService(repositorio, new FakeRelogio(Agora), new FakeLogService());

            ReturnResultadoAtrasosVO resultado = await servico.MarcarAtrasosDaLocadoraAsync();

            Assert.Equal(QuantidadeEsperadaNenhuma, resultado.QuantidadeAtualizada);
            Assert.Equal(StatusPagamento.PAGO, StatusDe(repositorio, IdVencidaDiaria));
        }

        [Fact]
        public async Task MarcarAtrasosDaLocadora_FalhaDoRepositorio_RelancaERegistraNoLog()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            repositorio.FalharAtrasos = true;
            FakeLogService log = new FakeLogService();
            PagamentoAtrasoService servico = new PagamentoAtrasoService(repositorio, new FakeRelogio(Agora), log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.MarcarAtrasosDaLocadoraAsync());

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLogAtualizarAtrasosLocadora, StringComparison.Ordinal));
        }

        [Fact]
        public async Task MarcarAtrasosTodasLocadoras_DevolveONumeroERegistraSoOContador()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            FakeLogService log = new FakeLogService();
            PagamentoAtrasoRotinaService rotina = new PagamentoAtrasoRotinaService(repositorio, new FakeRelogio(Agora), log);

            int quantidade = await rotina.MarcarAtrasosTodasLocadorasAsync();

            Assert.Equal(QuantidadeEsperadaUmaCobranca, quantidade);
            Assert.Contains(string.Format(ConstantsMessage.InfoAtrasosAtualizados, QuantidadeEsperadaUmaCobranca), log.Entries);
        }

        [Fact]
        public async Task MarcarAtrasosTodasLocadoras_FalhaDoRepositorio_RelancaERegistraNoLog()
        {
            FakePagamentoRepository repositorio = CriarRepositorioComCobrancas();
            repositorio.FalharAtrasos = true;
            FakeLogService log = new FakeLogService();
            PagamentoAtrasoRotinaService rotina = new PagamentoAtrasoRotinaService(repositorio, new FakeRelogio(Agora), log);

            await Assert.ThrowsAsync<InvalidOperationException>(() => rotina.MarcarAtrasosTodasLocadorasAsync());

            Assert.Contains(log.Entries, entrada => entrada.StartsWith(ConstantsMessage.ErroLogAtualizarAtrasosTodasLocadoras, StringComparison.Ordinal));
        }
    }
}
