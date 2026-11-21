using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Regras puras do painel financeiro (P7-BE-05, RF-35) com os predicados executados em memoria: periodo padrao, cada
    /// indicador e cada aba, caucao fora de tudo, ticket medio com arredondamento e mascara do CPF. Sem banco.
    /// </summary>
    public class RegrasPainelFinanceiroTests
    {
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 21);
        private static readonly DateOnly De = new DateOnly(2026, 11, 1);
        private static readonly DateOnly Ate = new DateOnly(2026, 11, 30);

        private static List<Pagamento> Filtrar(List<Pagamento> pagamentos, Expression<Func<Pagamento, bool>> predicado)
        {
            return pagamentos.Where(predicado.Compile()).ToList();
        }

        private static List<int> Ids(List<Pagamento> pagamentos)
        {
            return pagamentos.Select(x => x.IdPagamento).ToList();
        }

        [Fact]
        public void Recebido_SoPagoSemCaucaoComDataDePagamentoNoPeriodoInclusivo()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, 100m, new DateOnly(2026, 10, 1), De),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, 200m, new DateOnly(2026, 11, 5), Ate),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.DIARIA, StatusPagamento.PAGO, 300m, new DateOnly(2026, 11, 5), new DateOnly(2026, 10, 31)),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.CAUCAO, StatusPagamento.PAGO, 400m, new DateOnly(2026, 11, 5), Hoje),
                FabricaPagamentoFinanceiroDeTeste.Criar(5, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 500m, new DateOnly(2026, 11, 5), null)
            };

            List<Pagamento> recebidos = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoRecebido(De, Ate));

            Assert.Equal(new List<int> { 1, 2 }, Ids(recebidos));
        }

        [Fact]
        public void AReceber_VencimentoHojeEntra_OntemNaoEntra_FuturoEntra_AtrasadoENaoCaucaoNaoEntram()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 10m, Hoje, null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 20m, Hoje.AddDays(-1), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 30m, Hoje.AddDays(5), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 40m, Hoje.AddDays(5), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(5, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 50m, Hoje.AddDays(5), null)
            };

            List<Pagamento> aReceber = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoAReceber(De, Ate, Hoje));

            Assert.Equal(new List<int> { 1, 3 }, Ids(aReceber));
        }

        [Fact]
        public void EmAtraso_PendenteVencidoConta_OMesmoNumeroAntesEDepoisDeVirarAtrasado()
        {
            DateOnly vencimentoPassado = new DateOnly(2026, 11, 10);
            List<Pagamento> antes = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 100m, vencimentoPassado, null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 200m, Hoje, null)
            };
            List<Pagamento> depois = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 100m, vencimentoPassado, null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 200m, Hoje, null)
            };

            List<Pagamento> emAtrasoAntes = Filtrar(antes, RegrasPainelFinanceiro.PredicadoEmAtraso(De, Ate, Hoje));
            List<Pagamento> emAtrasoDepois = Filtrar(depois, RegrasPainelFinanceiro.PredicadoEmAtraso(De, Ate, Hoje));

            Assert.Equal(new List<int> { 1 }, Ids(emAtrasoAntes));
            Assert.Equal(Ids(emAtrasoAntes), Ids(emAtrasoDepois));
        }

        [Fact]
        public void EmAtraso_AtrasadoEntra_PagoEmCaucaoNaoEntram()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 10m, new DateOnly(2026, 11, 5), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, 20m, new DateOnly(2026, 11, 5), Hoje),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.CAUCAO, StatusPagamento.ATRASADO, 30m, new DateOnly(2026, 11, 5), null)
            };

            List<Pagamento> emAtraso = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoEmAtraso(De, Ate, Hoje));

            Assert.Equal(new List<int> { 1 }, Ids(emAtraso));
        }

        [Fact]
        public void EmAtraso_RespeitaOPeriodoDoVencimento()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 10m, new DateOnly(2026, 10, 31), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 10m, Ate, null),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 10m, Ate.AddDays(1), null)
            };

            List<Pagamento> emAtraso = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoEmAtraso(De, Ate, Hoje));

            Assert.Equal(new List<int> { 2 }, Ids(emAtraso));
        }

        [Fact]
        public void Ticket_SomaEQuantidadeDeSoPagosSemCaucaoComVencimentoNoPeriodo()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, 100m, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.VENDA, StatusPagamento.PAGO, 50m, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 21), idVenda: 9),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.CAUCAO, StatusPagamento.PAGO, 999m, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PAGO, 70m, new DateOnly(2026, 10, 5), new DateOnly(2026, 11, 6)),
                FabricaPagamentoFinanceiroDeTeste.Criar(5, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 80m, new DateOnly(2026, 11, 5), null)
            };

            List<Pagamento> pagos = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoTicketMedio(De, Ate));
            decimal soma = pagos.Sum(x => x.Valor);
            int quantidade = pagos.Count;

            Assert.Equal(new List<int> { 1, 2 }, Ids(pagos));
            Assert.Equal(150m, soma);
            Assert.Equal(2, quantidade);
            Assert.Equal(75m, RegrasPainelFinanceiro.CalcularTicketMedio(soma, quantidade));
        }

        [Fact]
        public void Ticket_SemNenhumPagamentoEZero_ECaucaoNaoEntraNaDivisao()
        {
            Assert.Equal(0m, RegrasPainelFinanceiro.CalcularTicketMedio(0m, 0));
        }

        [Fact]
        public void Ticket_ArredondaDuasCasasComAwayFromZero()
        {
            Assert.Equal(33.33m, RegrasPainelFinanceiro.CalcularTicketMedio(100m, 3));
            Assert.Equal(10.01m, RegrasPainelFinanceiro.CalcularTicketMedio(10.005m, 1));
        }

        [Fact]
        public void Periodo_AusenteVale_OMesCorrente()
        {
            (DateOnly De, DateOnly Ate) periodo = RegrasPainelFinanceiro.ResolverPeriodo(null, null, Hoje);

            Assert.Equal(new DateOnly(2026, 11, 1), periodo.De);
            Assert.Equal(new DateOnly(2026, 11, 30), periodo.Ate);
        }

        [Fact]
        public void Periodo_InformadoVale_ComoVeio()
        {
            (DateOnly De, DateOnly Ate) periodo = RegrasPainelFinanceiro.ResolverPeriodo(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15), Hoje);

            Assert.Equal(new DateOnly(2026, 9, 1), periodo.De);
            Assert.Equal(new DateOnly(2026, 9, 15), periodo.Ate);
        }

        [Fact]
        public void Titulos_VenceHojeSoEntraSeHojeEstiverNoPeriodo()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 10m, Hoje, null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 10m, Hoje.AddDays(1), null)
            };

            List<Pagamento> dentro = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosVenceHoje, De, Ate, Hoje));
            List<Pagamento> foraDoPeriodo = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosVenceHoje, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), Hoje));

            Assert.Equal(new List<int> { 1 }, Ids(dentro));
            Assert.Empty(foraDoPeriodo);
        }

        [Fact]
        public void Titulos_PagosRecentesIgnoraOPeriodoEPegaTrintaDiasAteHoje()
        {
            DateOnly inicioJanela = Hoje.AddDays(-(ConstantsValues.DiasPagosRecentes - 1));
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, 10m, new DateOnly(2026, 10, 25), inicioJanela),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, 10m, new DateOnly(2026, 10, 25), inicioJanela.AddDays(-1)),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.DIARIA, StatusPagamento.PAGO, 10m, new DateOnly(2026, 10, 25), Hoje),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.CAUCAO, StatusPagamento.PAGO, 10m, new DateOnly(2026, 10, 25), Hoje)
            };

            List<Pagamento> recentes = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosPagosRecentes, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), Hoje));

            Assert.Equal(new List<int> { 1, 3 }, Ids(recentes));
            Assert.Equal(new DateOnly(2026, 10, 23), inicioJanela);
        }

        [Fact]
        public void Titulos_TodosNaoTrazCaucaoNemPagoForaDoPeriodo()
        {
            List<Pagamento> pagamentos = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 10m, new DateOnly(2026, 9, 1), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, 10m, new DateOnly(2026, 11, 5), Hoje),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 10m, new DateOnly(2026, 11, 5), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PAGO, 10m, new DateOnly(2026, 9, 5), Hoje)
            };

            List<Pagamento> todos = Filtrar(pagamentos, RegrasPainelFinanceiro.PredicadoDaAba(ConstantsValues.AbaTitulosTodos, De, Ate, Hoje));

            Assert.Equal(new List<int> { 2 }, Ids(todos));
        }

        [Theory]
        [InlineData("19412345612", "194.***.***-12")]
        [InlineData("194.123.456-12", "194.***.***-12")]
        [InlineData("1941234561", null)]
        [InlineData("194123456123", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void CpfMascarado_TresPrimeirosEDoisUltimos_OuNuloQuandoNaoTemOnze(string? cpf, string? esperado)
        {
            Assert.Equal(esperado, RegrasPainelFinanceiro.MascararCpf(cpf));
        }

        [Fact]
        public void DiasParaVencer_HojeEZeroOntemENegativo()
        {
            Assert.Equal(0, RegrasPagamento.DiasParaVencer(Hoje, Hoje));
            Assert.Equal(-6, RegrasPagamento.DiasParaVencer(Hoje.AddDays(-6), Hoje));
        }
    }
}
