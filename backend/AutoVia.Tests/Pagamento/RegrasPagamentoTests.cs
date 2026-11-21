using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Integrations.Helpers.Pix;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Regras puras da cobranca (RegrasPagamento): situacao calculada, dias para vencer, inicio do periodo e os predicados das
    /// abas, dos totais e da listagem. Os predicados sao executados em memoria (Compile), a mesma expressao que o EF traduz;
    /// assim a regra do cliente e provada sem banco. O SQL e provado em PagamentoConsultasSqlTests.
    /// </summary>
    public class RegrasPagamentoTests
    {
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 19);

        private static Pagamento CriarPagamento(int id, TipoPagamento tipo, StatusPagamento status, DateOnly vencimento, DateOnly? dataPagamento = null, decimal valor = 10m)
        {
            return new Pagamento
            {
                IdPagamento = id,
                Tipo = tipo,
                Status = status,
                Valor = valor,
                DataVencimento = vencimento,
                DataPagamento = dataPagamento
            };
        }

        private static List<int> Ids(IEnumerable<Pagamento> lista)
        {
            return lista.Select(x => x.IdPagamento).OrderBy(x => x).ToList();
        }

        [Fact]
        public void Situacao_PendenteVencidoViraAtrasado()
        {
            StatusPagamento situacao = RegrasPagamento.Situacao(StatusPagamento.PENDENTE, new DateOnly(2026, 11, 18), Hoje);

            Assert.Equal(StatusPagamento.ATRASADO, situacao);
        }

        [Fact]
        public void Situacao_PendenteQueVenceHojeOuDepoisFicaPendente()
        {
            Assert.Equal(StatusPagamento.PENDENTE, RegrasPagamento.Situacao(StatusPagamento.PENDENTE, Hoje, Hoje));
            Assert.Equal(StatusPagamento.PENDENTE, RegrasPagamento.Situacao(StatusPagamento.PENDENTE, new DateOnly(2026, 11, 20), Hoje));
        }

        [Fact]
        public void Situacao_PagoCanceladoEAtrasadoFicamComoEstao()
        {
            DateOnly vencido = new DateOnly(2020, 1, 1);

            Assert.Equal(StatusPagamento.PAGO, RegrasPagamento.Situacao(StatusPagamento.PAGO, vencido, Hoje));
            Assert.Equal(StatusPagamento.CANCELADO, RegrasPagamento.Situacao(StatusPagamento.CANCELADO, vencido, Hoje));
            Assert.Equal(StatusPagamento.ATRASADO, RegrasPagamento.Situacao(StatusPagamento.ATRASADO, new DateOnly(2030, 1, 1), Hoje));
        }

        [Fact]
        public void DiasParaVencer_HojeZeroAmanhaUmOntemMenosUm()
        {
            Assert.Equal(0, RegrasPagamento.DiasParaVencer(Hoje, Hoje));
            Assert.Equal(1, RegrasPagamento.DiasParaVencer(new DateOnly(2026, 11, 20), Hoje));
            Assert.Equal(-1, RegrasPagamento.DiasParaVencer(new DateOnly(2026, 11, 18), Hoje));
        }

        [Fact]
        public void InicioDoPeriodo_DozeMesesSeisMesesAnoAtualETudo()
        {
            Assert.Equal(new DateOnly(2025, 11, 19), RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoDozeMeses, Hoje));
            Assert.Equal(new DateOnly(2026, 5, 19), RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoSeisMeses, Hoje));
            Assert.Equal(new DateOnly(2026, 1, 1), RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoAnoAtual, Hoje));
            Assert.Null(RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoTudo, Hoje));
        }

        [Fact]
        public void PredicadoPendente_PendenteEAtrasadoSemCaucao()
        {
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, Hoje),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, Hoje),
                CriarPagamento(3, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, Hoje),
                CriarPagamento(4, TipoPagamento.DIARIA, StatusPagamento.PAGO, Hoje, Hoje)
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoPendente().Compile()));

            Assert.Equal(new List<int> { 1, 2 }, ids);
        }

        [Fact]
        public void PredicadoDaAbaPendentes_PendenteVencidoAntigoEntraMesmoForaDoPeriodo()
        {
            DateOnly inicio = RegrasPagamento.InicioDoPeriodo(ConstantsValues.PeriodoSeisMeses, Hoje) ?? DateOnly.MinValue;
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2010, 1, 1)),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 1))
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaPendentes, inicio).Compile()));

            Assert.Equal(new List<int> { 1 }, ids);
        }

        [Fact]
        public void PredicadoDaAbaTodos_PendentesSempreMaisORestoDentroDoPeriodo()
        {
            DateOnly inicio = new DateOnly(2026, 5, 19);
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2010, 1, 1)),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)),
                CriarPagamento(3, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 1)),
                CriarPagamento(4, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 6, 1)),
                CriarPagamento(5, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2010, 1, 1)),
                CriarPagamento(6, TipoPagamento.DIARIA, StatusPagamento.CANCELADO, new DateOnly(2026, 6, 1))
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaTodos, inicio).Compile()));

            Assert.Equal(new List<int> { 1, 2, 4, 6 }, ids);
        }

        [Fact]
        public void PredicadoDaAbaTodos_PeriodoTudoNaoCortaNada()
        {
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 1)),
                CriarPagamento(2, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2010, 1, 1))
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaTodos, null).Compile()));

            Assert.Equal(new List<int> { 1, 2 }, ids);
        }

        [Fact]
        public void PredicadoDaAbaPagos_SoPagoSemCaucaoDentroDoPeriodo()
        {
            DateOnly inicio = new DateOnly(2026, 5, 19);
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 1)),
                CriarPagamento(3, TipoPagamento.CAUCAO, StatusPagamento.PAGO, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)),
                CriarPagamento(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 6, 1))
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaAba(ConstantsValues.AbaPagos, inicio).Compile()));

            Assert.Equal(new List<int> { 1 }, ids);
        }

        [Fact]
        public void PredicadoQuitadoNoAno_PagoNoAnoSemCaucao()
        {
            DateOnly inicioAno = new DateOnly(2026, 1, 1);
            DateOnly inicioProximoAno = new DateOnly(2027, 1, 1);
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2)),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2025, 3, 1), new DateOnly(2025, 12, 31)),
                CriarPagamento(3, TipoPagamento.CAUCAO, StatusPagamento.PAGO, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2)),
                CriarPagamento(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 3, 1))
            };

            List<int> ids = Ids(cobrancas.Where(RegrasPagamento.PredicadoQuitadoNoAno(inicioAno, inicioProximoAno).Compile()));

            Assert.Equal(new List<int> { 1 }, ids);
        }

        [Fact]
        public void PredicadoDaListagem_StatusExatoEIntervaloDeVencimentoSemLimitesDevolveTudo()
        {
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                CriarPagamento(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 1, 10)),
                CriarPagamento(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 10)),
                CriarPagamento(3, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 3, 10))
            };

            List<int> porStatus = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaListagem(StatusPagamento.PENDENTE, null, null).Compile()));
            List<int> porIntervalo = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaListagem(null, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)).Compile()));
            List<int> semFiltro = Ids(cobrancas.Where(RegrasPagamento.PredicadoDaListagem(null, null, null).Compile()));

            Assert.Equal(new List<int> { 1, 3 }, porStatus);
            Assert.Equal(new List<int> { 2 }, porIntervalo);
            Assert.Equal(new List<int> { 1, 2, 3 }, semFiltro);
        }

        [Fact]
        public void SituacaoComTipo_CaucaoPendenteVencidoFicaPendente()
        {
            StatusPagamento situacao = RegrasPagamento.Situacao(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 7), Hoje);

            Assert.Equal(StatusPagamento.PENDENTE, situacao);
        }

        [Fact]
        public void SituacaoComTipo_CaucaoPendenteComVencimentoFuturoFicaPendente()
        {
            StatusPagamento situacao = RegrasPagamento.Situacao(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 30), Hoje);

            Assert.Equal(StatusPagamento.PENDENTE, situacao);
        }

        [Fact]
        public void SituacaoComTipo_DiariaPendenteVencidoContinuaAtrasado()
        {
            StatusPagamento situacao = RegrasPagamento.Situacao(TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 7), Hoje);

            Assert.Equal(StatusPagamento.ATRASADO, situacao);
        }

        [Theory]
        [InlineData(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, ConstantsMessage.ErroPixTipoNaoPagavel)]
        [InlineData(TipoPagamento.CAUCAO, StatusPagamento.PAGO, ConstantsMessage.ErroPixTipoNaoPagavel)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PAGO, ConstantsMessage.ErroPixPagamentoJaPago)]
        [InlineData(TipoPagamento.VENDA, StatusPagamento.CANCELADO, ConstantsMessage.ErroPixPagamentoCancelado)]
        public void ErroDaGeracaoPix_RecusaNaOrdemCaucaoPagoCancelado(TipoPagamento tipo, StatusPagamento status, string erroEsperado)
        {
            Assert.False(RegrasPagamento.PodeGerarPix(tipo, status));
            Assert.Equal(erroEsperado, RegrasPagamento.ErroDaGeracaoPix(tipo, status));
        }

        [Theory]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PENDENTE)]
        [InlineData(TipoPagamento.COBRANCA_EXTRA, StatusPagamento.ATRASADO)]
        [InlineData(TipoPagamento.VENDA, StatusPagamento.PENDENTE)]
        public void PodeGerarPix_PendenteOuAtrasadoSemCaucao_EhLiberado(TipoPagamento tipo, StatusPagamento status)
        {
            Assert.True(RegrasPagamento.PodeGerarPix(tipo, status));
            Assert.Null(RegrasPagamento.ErroDaGeracaoPix(tipo, status));
        }

        [Fact]
        public void ReferenciaDaCobranca_SemPixGravado_EPrefixoEId()
        {
            Assert.Equal("PAG-12", RegrasPagamento.ReferenciaDaCobranca(12, null));
        }

        [Fact]
        public void ReferenciaDaCobranca_ComPixExpirado_IncluiAValidadeAnteriorEEhDeterministica()
        {
            DateTime validade = new DateTime(2026, 11, 20, 10, 5, 3);

            string primeira = RegrasPagamento.ReferenciaDaCobranca(12, validade);
            string segunda = RegrasPagamento.ReferenciaDaCobranca(12, validade);

            Assert.Equal("PAG-12-20261120100503", primeira);
            Assert.Equal(primeira, segunda);
            Assert.True(primeira.Length <= ConstantsPix.ReferenciaTamanhoMaximo);
            Assert.Matches(ConstantsPix.PadraoReferenciaExterna, primeira);
        }

        [Theory]
        [InlineData(900, 900)]
        [InlineData(1, 1)]
        [InlineData(-5, 0)]
        public void SegundosParaExpirar_ContaParaBaixoELimitaEmZero(int segundosRestantes, int esperado)
        {
            DateTime agora = new DateTime(2026, 11, 20, 10, 0, 0);

            Assert.Equal(esperado, RegrasPagamento.SegundosParaExpirar(agora.AddSeconds(segundosRestantes), agora));
        }

        [Fact]
        public void SegundosParaExpirar_FracaoDeSegundoNaoArredondaParaCima()
        {
            DateTime agora = new DateTime(2026, 11, 20, 10, 0, 0);

            Assert.Equal(1, RegrasPagamento.SegundosParaExpirar(agora.AddMilliseconds(1900), agora));
        }

        [Fact]
        public void PixAindaValido_ExigeIdentificadorEValidadeFutura()
        {
            DateTime agora = new DateTime(2026, 11, 20, 10, 0, 0);

            Assert.True(RegrasPagamento.PixAindaValido("FAKE-1", agora.AddSeconds(1), agora));
            Assert.False(RegrasPagamento.PixAindaValido("FAKE-1", agora, agora));
            Assert.False(RegrasPagamento.PixAindaValido("FAKE-1", null, agora));
            Assert.False(RegrasPagamento.PixAindaValido(null, agora.AddSeconds(900), agora));
            Assert.False(RegrasPagamento.PixAindaValido(string.Empty, agora.AddSeconds(900), agora));
        }

        [Theory]
        [InlineData(null, "DINHEIRO", false, false)]
        [InlineData("FAKE-1", "PIX", false, false)]
        [InlineData("FAKE-1", "DINHEIRO", false, true)]
        [InlineData("FAKE-1", "DINHEIRO", true, false)]
        [InlineData("FAKE-1", "CARTAO", false, true)]
        [InlineData("FAKE-1", " pix ", false, false)]
        [InlineData("FAKE-1", "Pix", false, false)]
        [InlineData("", "DINHEIRO", false, false)]
        public void ExigeConfirmacaoBaixaComPix_TabelaVerdade(string? idTransacao, string forma, bool confirmar, bool esperado)
        {
            Assert.Equal(esperado, RegrasPagamento.ExigeConfirmacaoBaixaComPix(idTransacao, forma, confirmar));
        }

        [Fact]
        public void MotivoDivergenciaRetornoPix_ValorIgualEReferenciaIgual_NaoDiverge()
        {
            Assert.Null(RegrasPagamento.MotivoDivergenciaRetornoPix(395.60m, 395.6m, "PAG-12", "PAG-12"));
        }

        [Fact]
        public void MotivoDivergenciaRetornoPix_ValorDiferente_DevolveVALOR()
        {
            Assert.Equal(ConstantsValues.MotivoDivergenciaValor, RegrasPagamento.MotivoDivergenciaRetornoPix(395.60m, 10m, "PAG-12", "PAG-12"));
        }

        [Theory]
        [InlineData("PAG-12", "PAG-13")]
        [InlineData("PAG-12", null)]
        [InlineData("PAG-12", "pag-12")]
        public void MotivoDivergenciaRetornoPix_ReferenciaDiferente_DevolveREFERENCIA(string pedida, string? retornada)
        {
            Assert.Equal(ConstantsValues.MotivoDivergenciaReferencia, RegrasPagamento.MotivoDivergenciaRetornoPix(10m, 10m, pedida, retornada));
        }

        [Fact]
        public void RetornoPixUtilizavel_ExigeCopiaQrEValidadeFuturaELimiteDe512()
        {
            DateTime agora = new DateTime(2026, 11, 20, 10, 0, 0);
            DateTime futuro = agora.AddSeconds(900);
            string copia = new string('c', ConstantsValues.TamanhoMaximoPixCopiaECola);

            Assert.True(RegrasPagamento.RetornoPixUtilizavel(copia, "QR", futuro, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(null, "QR", futuro, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(string.Empty, "QR", futuro, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(copia, null, futuro, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(copia, "QR", null, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(copia, "QR", agora, agora));
            Assert.False(RegrasPagamento.RetornoPixUtilizavel(new string('c', ConstantsValues.TamanhoMaximoPixCopiaECola + 1), "QR", futuro, agora));
        }
    }
}
