using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.PainelFinanceiro;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoVia.Tests.Painel
{
    /// <summary>
    /// Servico do painel financeiro (P7-BE-05, RF-35) com o repositorio falso (que aplica os mesmos predicados do real) e o
    /// relogio em 21/11/2026. Confere: os quatro indicadores e o eixo de data (recebido pelo pagamento, o resto pelo
    /// vencimento), o periodo padrao, o ticket medio, a lista de titulos (ordem, paginacao, contadores, mascara do CPF, rotulos
    /// e situacao), duas locadoras e a falha com log.
    /// </summary>
    public class PainelFinanceiroServiceTests
    {
        private static readonly DateTime AgoraFixo = new DateTime(2026, 11, 21, 10, 0, 0);
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 21);
        private const int EmpresaA = 1;
        private const int EmpresaB = 2;
        private const string CpfJoao = "10987654321";
        private const string NomeJoao = "Joao Lima";

        private static PainelFinanceiroService Criar(IContextoEmpresa contexto, Dictionary<int, List<Pagamento>> dados, FakeLogService log, bool falharConsulta = false)
        {
            FakePainelFinanceiroRepository repositorio = new FakePainelFinanceiroRepository(contexto, dados) { FalharConsulta = falharConsulta };

            return new PainelFinanceiroService(repositorio, new FakeRelogio(AgoraFixo), log);
        }

        /// <summary>Locadora A: recebidos em novembro, um a receber, um vencido, uma caucao (fora de tudo) e uma venda paga.</summary>
        private static List<Pagamento> CobrancasDaLocadoraA()
        {
            return new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, 100m, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6)),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PAGO, 300m, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 12)),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 150m, new DateOnly(2026, 11, 25), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 80m, new DateOnly(2026, 11, 10), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(5, TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, 999m, new DateOnly(2026, 11, 25), null)
            };
        }

        [Fact]
        public async Task Indicadores_ValoresEFormatacaoNoPeriodoPadrao()
        {
            FakeLogService log = new FakeLogService();
            Dictionary<int, List<Pagamento>> dados = new Dictionary<int, List<Pagamento>> { [EmpresaA] = CobrancasDaLocadoraA() };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), dados, log);

            ReturnPainelFinanceiroVO retorno = await servico.ObterIndicadoresAsync(new FiltroPainelFinanceiroVO());

            Assert.Equal(new DateOnly(2026, 11, 1), retorno.De);
            Assert.Equal(new DateOnly(2026, 11, 30), retorno.Ate);
            Assert.Equal("01/11/2026", retorno.DeStr);
            Assert.Equal("30/11/2026", retorno.AteStr);
            Assert.Equal(400m, retorno.RecebidoValor);
            Assert.Equal(StaticMethods.FormateToMonetary(400m), retorno.RecebidoStr);
            Assert.Equal(2, retorno.QuantidadeRecebido);
            Assert.Equal(150m, retorno.AReceberValor);
            Assert.Equal(1, retorno.QuantidadeAReceber);
            Assert.Equal(80m, retorno.EmAtrasoValor);
            Assert.Equal(1, retorno.QuantidadeEmAtraso);
            Assert.Equal(200m, retorno.TicketMedioValor);
            Assert.Equal(StaticMethods.FormateToMonetary(200m), retorno.TicketMedioStr);
            Assert.Equal(2, retorno.QuantidadeTicket);
        }

        [Fact]
        public async Task Recebido_UsaADataDoPagamento_OsOutrosUsamOVencimento()
        {
            FakeLogService log = new FakeLogService();
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PAGO, 70m, new DateOnly(2026, 10, 5), new DateOnly(2026, 11, 6))
            };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = cobrancas }, log);

            ReturnPainelFinanceiroVO retorno = await servico.ObterIndicadoresAsync(new FiltroPainelFinanceiroVO
            {
                De = new DateOnly(2026, 11, 1),
                Ate = new DateOnly(2026, 11, 30)
            });

            Assert.Equal(70m, retorno.RecebidoValor);
            Assert.Equal(0, retorno.QuantidadeTicket);
            Assert.Equal(0m, retorno.TicketMedioValor);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), retorno.TicketMedioStr);
        }

        [Fact]
        public async Task EmAtraso_OMesmoValorComOPendenteVencidoOuJaMarcadoComoAtrasado()
        {
            FakeLogService logAntes = new FakeLogService();
            FakeLogService logDepois = new FakeLogService();
            List<Pagamento> antes = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 80m, new DateOnly(2026, 11, 10), null)
            };
            List<Pagamento> depois = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.ATRASADO, 80m, new DateOnly(2026, 11, 10), null)
            };

            ReturnPainelFinanceiroVO retornoAntes = await Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = antes }, logAntes)
                .ObterIndicadoresAsync(new FiltroPainelFinanceiroVO());
            ReturnPainelFinanceiroVO retornoDepois = await Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = depois }, logDepois)
                .ObterIndicadoresAsync(new FiltroPainelFinanceiroVO());

            Assert.Equal(retornoAntes.EmAtrasoValor, retornoDepois.EmAtrasoValor);
            Assert.Equal(retornoAntes.QuantidadeEmAtraso, retornoDepois.QuantidadeEmAtraso);
            Assert.Equal(80m, retornoAntes.EmAtrasoValor);
        }

        [Fact]
        public async Task Indicadores_DuasLocadorasNaoSeMisturam()
        {
            FakeLogService log = new FakeLogService();
            Dictionary<int, List<Pagamento>> dados = new Dictionary<int, List<Pagamento>>
            {
                [EmpresaA] = CobrancasDaLocadoraA(),
                [EmpresaB] = new List<Pagamento>
                {
                    FabricaPagamentoFinanceiroDeTeste.Criar(90, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 5000m, new DateOnly(2026, 11, 25), null)
                }
            };

            ReturnPainelFinanceiroVO daA = await Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), dados, log).ObterIndicadoresAsync(new FiltroPainelFinanceiroVO());
            ReturnPainelFinanceiroVO daB = await Criar(ContextoEmpresaDeTeste.Criar(EmpresaB), dados, log).ObterIndicadoresAsync(new FiltroPainelFinanceiroVO());

            Assert.Equal(150m, daA.AReceberValor);
            Assert.Equal(5000m, daB.AReceberValor);
            Assert.Equal(1, daB.QuantidadeAReceber);
        }

        [Fact]
        public async Task Indicadores_FalhaDeConsultaVaiParaOLogComOMensagemDoPainelERelanca()
        {
            FakeLogService log = new FakeLogService();
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>>(), log, falharConsulta: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ObterIndicadoresAsync(new FiltroPainelFinanceiroVO()));

            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroLogPainelFinanceiro));
        }

        [Fact]
        public async Task Titulos_TodosEmOrdemDeVencimentoComRotulosCpfEContadores()
        {
            FakeLogService log = new FakeLogService();
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 100m, new DateOnly(2026, 11, 25), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 50m, new DateOnly(2026, 11, 10), null, nome: NomeJoao, cpf: CpfJoao),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.VENDA, StatusPagamento.PAGO, 70m, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6), idVenda: 9),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 30m, Hoje, null)
            };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = cobrancas }, log);

            ReturnTitulosFinanceirosVO retorno = await servico.ObterTitulosAsync(new FiltroTitulosFinanceirosVO());

            Assert.Equal(4, retorno.QuantidadeTodos);
            Assert.Equal(1, retorno.QuantidadeEmAtraso);
            Assert.Equal(1, retorno.QuantidadeVenceHoje);
            Assert.Equal(1, retorno.QuantidadePagosRecentes);
            Assert.Equal(4, retorno.Pagina.Total);
            Assert.Equal(new List<int> { 3, 2, 4, 1 }, retorno.Pagina.Itens.Select(x => x.IdPagamento).ToList());

            ReturnTituloFinanceiroVO venda = retorno.Pagina.Itens[0];
            Assert.Equal("VENDA", venda.Origem);
            Assert.Equal("Venda nº 9", venda.OrigemStr);
            Assert.Equal("Venda", venda.TipoStr);
            Assert.Equal("PAGO", venda.Situacao);
            Assert.False(venda.PodeDarBaixa);
            Assert.Equal("06/11/2026", venda.DataPagamentoStr);
            Assert.Equal("PIX", venda.Forma);
            Assert.Equal("194.***.***-12", venda.ClienteCpfMascarado);

            ReturnTituloFinanceiroVO vencido = retorno.Pagina.Itens[1];
            Assert.Equal("LOCACAO", vencido.Origem);
            Assert.Equal("Locação nº 1", vencido.OrigemStr);
            Assert.Equal("Diária", vencido.TipoStr);
            Assert.Equal(NomeJoao, vencido.ClienteNome);
            Assert.Equal("109.***.***-21", vencido.ClienteCpfMascarado);
            Assert.Equal("10/11/2026", vencido.DataVencimentoStr);
            Assert.Equal(-11, vencido.DiasParaVencer);
            Assert.Equal("ATRASADO", vencido.Situacao);
            Assert.True(vencido.PodeDarBaixa);
            Assert.Null(vencido.DataPagamentoStr);
        }

        [Fact]
        public async Task Titulos_PaginacaoMantemOTotalEOsContadores()
        {
            FakeLogService log = new FakeLogService();
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(1, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 100m, new DateOnly(2026, 11, 25), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(2, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 50m, new DateOnly(2026, 11, 10), null),
                FabricaPagamentoFinanceiroDeTeste.Criar(3, TipoPagamento.VENDA, StatusPagamento.PAGO, 70m, new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 6), idVenda: 9),
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 30m, Hoje, null)
            };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = cobrancas }, log);

            ReturnTitulosFinanceirosVO segundaPagina = await servico.ObterTitulosAsync(new FiltroTitulosFinanceirosVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(4, segundaPagina.Pagina.Total);
            Assert.Equal(2, segundaPagina.Pagina.Pagina);
            Assert.Equal(new List<int> { 4, 1 }, segundaPagina.Pagina.Itens.Select(x => x.IdPagamento).ToList());
            Assert.Equal(4, segundaPagina.QuantidadeTodos);
        }

        [Fact]
        public async Task Titulos_VenceHojeSoAparececomHojeNoPeriodo()
        {
            FakeLogService log = new FakeLogService();
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(4, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 30m, Hoje, null)
            };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = cobrancas }, log);

            ReturnTitulosFinanceirosVO fora = await servico.ObterTitulosAsync(new FiltroTitulosFinanceirosVO
            {
                De = new DateOnly(2026, 11, 1),
                Ate = new DateOnly(2026, 11, 10)
            });

            Assert.Equal(0, fora.QuantidadeVenceHoje);
        }

        [Fact]
        public async Task Titulos_PagosRecentesIgnoraOPeriodo()
        {
            FakeLogService log = new FakeLogService();
            List<Pagamento> cobrancas = new List<Pagamento>
            {
                FabricaPagamentoFinanceiroDeTeste.Criar(7, TipoPagamento.DIARIA, StatusPagamento.PAGO, 40m, new DateOnly(2026, 9, 5), new DateOnly(2026, 10, 25))
            };
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>> { [EmpresaA] = cobrancas }, log);

            ReturnTitulosFinanceirosVO retorno = await servico.ObterTitulosAsync(new FiltroTitulosFinanceirosVO
            {
                Aba = ConstantsValues.AbaTitulosPagosRecentes,
                De = new DateOnly(2026, 11, 1),
                Ate = new DateOnly(2026, 11, 30)
            });

            Assert.Equal(1, retorno.QuantidadePagosRecentes);
            Assert.Equal(new List<int> { 7 }, retorno.Pagina.Itens.Select(x => x.IdPagamento).ToList());
        }

        [Fact]
        public async Task Titulos_DuasLocadorasNaoSeMisturam()
        {
            FakeLogService log = new FakeLogService();
            Dictionary<int, List<Pagamento>> dados = new Dictionary<int, List<Pagamento>>
            {
                [EmpresaA] = CobrancasDaLocadoraA(),
                [EmpresaB] = new List<Pagamento>
                {
                    FabricaPagamentoFinanceiroDeTeste.Criar(90, TipoPagamento.DIARIA, StatusPagamento.PENDENTE, 5000m, new DateOnly(2026, 11, 25), null)
                }
            };

            ReturnTitulosFinanceirosVO daB = await Criar(ContextoEmpresaDeTeste.Criar(EmpresaB), dados, log).ObterTitulosAsync(new FiltroTitulosFinanceirosVO());

            Assert.Equal(1, daB.QuantidadeTodos);
            Assert.Equal(new List<int> { 90 }, daB.Pagina.Itens.Select(x => x.IdPagamento).ToList());
        }

        [Fact]
        public async Task Titulos_FalhaDeConsultaVaiParaOLogComOMensagemDeTitulosERelanca()
        {
            FakeLogService log = new FakeLogService();
            PainelFinanceiroService servico = Criar(ContextoEmpresaDeTeste.Criar(EmpresaA), new Dictionary<int, List<Pagamento>>(), log, falharConsulta: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.ObterTitulosAsync(new FiltroTitulosFinanceirosVO()));

            Assert.Contains(log.Entries, entrada => entrada.Contains(ConstantsMessage.ErroLogTitulosFinanceiros));
        }
    }
}
