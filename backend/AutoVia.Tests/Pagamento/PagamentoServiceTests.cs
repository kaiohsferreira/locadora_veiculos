using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Communication.ViewObjects.Utils;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Servico de cobranca com o repositorio em memoria (FakePagamentoRepository), o relogio fixo em 19/11/2026 e o Profile
    /// real. Cobre o lancamento (origem, tipo, caucao direta, valor, vencimento, 404), a baixa (PENDENTE e ATRASADO viram PAGO;
    /// PAGO, CANCELADO e CAUCAO dao 409; inexistente 404), as listagens (filtros, ordem, aba, periodo, situacao, dias para
    /// vencer e dono) e o resumo (totais sem caucao, ano pela data do pagamento). Prova que o id da transacao nunca sai.
    /// </summary>
    public class PagamentoServiceTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 19, 10, 0, 0);
        private static readonly DateOnly Hoje = new DateOnly(2026, 11, 19);
        private const int IdClienteDoTeste = 7;
        private const int IdClienteAlheio = 8;

        private static PagamentoService CriarServico(FakePagamentoRepository repositorio)
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<PagamentoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = configuracao.CreateMapper();

            return new PagamentoService(repositorio, mapper, new FakeRelogio(Agora), ContextoEmpresaDeTeste.Criar(1, CargoFuncionario.FINANCEIRO), new FakeLogService(), new FakePixGateway());
        }

        private static Locacao CriarLocacao(int id, int idCliente)
        {
            return new Locacao { IdLocacao = id, IdCliente = idCliente, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" } };
        }

        private static Venda CriarVenda(int id, int idCliente)
        {
            return new Venda { IdVenda = id, IdCliente = idCliente, Veiculo = new Veiculo { Marca = "VW", Modelo = "Polo" } };
        }

        private static Pagamento Cobranca(int id, Locacao? locacao, TipoPagamento tipo, StatusPagamento status, DateOnly vencimento, DateOnly? dataPagamento = null, decimal valor = 100m)
        {
            return new Pagamento
            {
                IdPagamento = id,
                IdLocacao = locacao?.IdLocacao,
                Locacao = locacao,
                Tipo = tipo,
                Status = status,
                Valor = valor,
                DataVencimento = vencimento,
                DataPagamento = dataPagamento,
                Forma = dataPagamento.HasValue ? "PIX" : null,
                IdTransacao = "tx-segredo-" + id,
                CreatedAt = Agora,
                UpdatedAt = Agora
            };
        }

        private static LancarPagamentoVO Lancamento(int? idLocacao, int? idVenda, string? tipo, decimal valor = 395.60m, DateOnly? vencimento = null)
        {
            return new LancarPagamentoVO
            {
                IdLocacao = idLocacao,
                IdVenda = idVenda,
                Tipo = tipo,
                Valor = valor,
                DataVencimento = vencimento ?? new DateOnly(2026, 11, 25)
            };
        }

        private static FakePagamentoRepository RepositorioComOrigens()
        {
            FakePagamentoRepository repositorio = new FakePagamentoRepository();
            repositorio.Locacoes.Add(CriarLocacao(1, IdClienteDoTeste));
            repositorio.Locacoes.Add(CriarLocacao(2, IdClienteAlheio));
            repositorio.Vendas.Add(CriarVenda(4, IdClienteDoTeste));

            return repositorio;
        }

        // ---------- Lancamento ----------

        [Fact]
        public async Task Lancar_ComLocacao_NasceComoPendenteSemFormaNemDataDePagamento()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            PagamentoService servico = CriarServico(repositorio);

            ReturnPagamentoVO criado = await servico.LancarAsync(Lancamento(1, null, "DIARIA"));

            Assert.Equal(StatusPagamento.PENDENTE.ToString(), criado.Status);
            Assert.Equal(TipoPagamento.DIARIA.ToString(), criado.Tipo);
            Assert.Equal(1, criado.IdLocacao);
            Assert.Null(criado.IdVenda);
            Assert.Null(criado.DataPagamento);
            Assert.Null(criado.DataPagamentoStr);
            Assert.Null(criado.Forma);
            Assert.Equal(StaticMethods.FormateToMonetary(395.60m), criado.ValorStr);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos.Single().Status);
        }

        [Fact]
        public async Task Lancar_ComVenda_Certo()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            PagamentoService servico = CriarServico(repositorio);

            ReturnPagamentoVO criado = await servico.LancarAsync(Lancamento(null, 4, "VENDA", 1000m));

            Assert.Equal(TipoPagamento.VENDA.ToString(), criado.Tipo);
            Assert.Equal(4, criado.IdVenda);
            Assert.Null(criado.IdLocacao);
            Assert.Equal(StatusPagamento.PENDENTE.ToString(), criado.Status);
        }

        [Fact]
        public async Task Lancar_LocacaoDeOutraLocadoraOuInexistente_Devolve404()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            PagamentoService servico = CriarServico(repositorio);

            await Assert.ThrowsAsync<NotFoundException>(() => servico.LancarAsync(Lancamento(999, null, "DIARIA")));
            Assert.Empty(repositorio.Pagamentos);
        }

        [Fact]
        public async Task Lancar_VendaInexistente_Devolve404()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            PagamentoService servico = CriarServico(repositorio);

            await Assert.ThrowsAsync<NotFoundException>(() => servico.LancarAsync(Lancamento(null, 999, "VENDA")));
        }

        [Fact]
        public async Task Lancar_CaucaoChamadaDireto_E_Aceita()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            PagamentoService servico = CriarServico(repositorio);

            ReturnPagamentoVO criado = await servico.LancarAsync(Lancamento(1, null, "CAUCAO", 300m));

            Assert.Equal(TipoPagamento.CAUCAO.ToString(), criado.Tipo);
            Assert.Equal(StatusPagamento.PENDENTE.ToString(), criado.Status);
        }

        [Fact]
        public async Task Lancar_CaucaoComVenda_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(null, 4, "CAUCAO", 300m)));
        }

        [Fact]
        public async Task Lancar_OrigemDuplaOuAusente_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(1, 4, "DIARIA")));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(null, null, "DIARIA")));
        }

        [Fact]
        public async Task Lancar_TipoInvalidoValorZeroOuVencimentoAusente_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(1, null, "OUTRO")));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(1, null, "DIARIA", 0m)));
            LancarPagamentoVO semVencimento = new LancarPagamentoVO { IdLocacao = 1, Tipo = "DIARIA", Valor = 10m, DataVencimento = null };

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(semVencimento));
        }

        [Fact]
        public async Task Lancar_TipoDiariaComVenda_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.LancarAsync(Lancamento(null, 4, "DIARIA")));
        }

        [Fact]
        public void VOsDeSaidaNaoTemIdTransacao()
        {
            Assert.Null(typeof(ReturnPagamentoVO).GetProperty("IdTransacao", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(ReturnMeuPagamentoVO).GetProperty("IdTransacao", BindingFlags.Public | BindingFlags.Instance));
            Assert.Null(typeof(ReturnResumoMeusPagamentosVO).GetProperty("IdTransacao", BindingFlags.Public | BindingFlags.Instance));
        }

        // ---------- Baixa ----------

        [Fact]
        public async Task Baixar_PendenteVirapagoComDataDeHojeEForma()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            PagamentoService servico = CriarServico(repositorio);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(5, new BaixarPagamentoVO { Forma = " PIX " });

            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
            Assert.Equal(Hoje, baixado.DataPagamento);
            Assert.Equal(StaticMethods.FormateDateToPTBR(Hoje), baixado.DataPagamentoStr);
            Assert.Equal("PIX", baixado.Forma);
        }

        [Fact]
        public async Task Baixar_AtrasadoVirapago()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.COBRANCA_EXTRA, StatusPagamento.ATRASADO, new DateOnly(2026, 10, 1)));
            PagamentoService servico = CriarServico(repositorio);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(5, new BaixarPagamentoVO { Forma = "PIX" });

            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
        }

        [Fact]
        public async Task Baixar_AtrasadoComPixGeradoPorOutraForma_Devolve409SemConfirmacao()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.COBRANCA_EXTRA, StatusPagamento.ATRASADO, new DateOnly(2026, 10, 1)));
            PagamentoService servico = CriarServico(repositorio);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(5, new BaixarPagamentoVO { Forma = "CARTAO" }));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaComPixGerado, excecao.Message);
        }

        [Fact]
        public async Task Baixar_PagoCanceladoOuCaucao_Devolve409()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 11, 25), new DateOnly(2026, 11, 20)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.CANCELADO, new DateOnly(2026, 11, 25)));
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            PagamentoService servico = CriarServico(repositorio);

            BusinessException pago = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(1, new BaixarPagamentoVO { Forma = "PIX" }));
            BusinessException cancelado = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(2, new BaixarPagamentoVO { Forma = "PIX" }));
            BusinessException caucao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(3, new BaixarPagamentoVO { Forma = "PIX" }));

            Assert.Equal(ConstantsMessage.ErroPagamentoJaPago, pago.Message);
            Assert.Equal(ConstantsMessage.ErroPagamentoCancelado, cancelado.Message);
            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCaucao, caucao.Message);
        }

        [Fact]
        public async Task Baixar_Inexistente_Devolve404()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<NotFoundException>(() => servico.BaixarAsync(999, new BaixarPagamentoVO { Forma = "PIX" }));
        }

        [Fact]
        public async Task Baixar_FormaAusenteOuInvalida_Recusa400()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            PagamentoService servico = CriarServico(repositorio);

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.BaixarAsync(5, new BaixarPagamentoVO { Forma = null }));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.BaixarAsync(5, new BaixarPagamentoVO { Forma = "CHEQUE" }));
            Assert.Equal(0, repositorio.Baixas);
        }

        // ---------- Listagem da locadora ----------

        [Fact]
        public async Task Listar_FiltraStatusEIntervaloEOrdenaPorVencimentoEId()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 12, 1)));
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 1)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 15)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnPagamentoVO> pendentes = await servico.ListarAsync(new FiltroPagamentoVO { Status = "PENDENTE" });
            PaginaVO<ReturnPagamentoVO> intervalo = await servico.ListarAsync(new FiltroPagamentoVO { VencimentoDe = new DateOnly(2026, 11, 10), VencimentoAte = new DateOnly(2026, 11, 30) });

            Assert.Equal(2, pendentes.Total);
            Assert.Equal(new List<int> { 1, 3 }, pendentes.Itens.Select(x => x.IdPagamento).ToList());
            Assert.Equal(new List<int> { 2 }, intervalo.Itens.Select(x => x.IdPagamento).ToList());
        }

        [Fact]
        public async Task Listar_PaginaPeloDeslocamentoEOTotalSemOTeto()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            for (int indice = 1; indice <= 3; indice++)
            {
                repositorio.Pagamentos.Add(Cobranca(indice, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, indice)));
            }
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnPagamentoVO> segunda = await servico.ListarAsync(new FiltroPagamentoVO { Pagina = 2, TamanhoPagina = 2 });

            Assert.Equal(3, segunda.Total);
            Assert.Equal(new List<int> { 3 }, segunda.Itens.Select(x => x.IdPagamento).ToList());
        }

        [Fact]
        public async Task Listar_StatusInvalidoOuIntervaloInvertido_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.ListarAsync(new FiltroPagamentoVO { Status = "pago" }));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.ListarAsync(new FiltroPagamentoVO { VencimentoDe = new DateOnly(2026, 3, 1), VencimentoAte = new DateOnly(2026, 2, 1) }));
        }

        // ---------- Cobrancas do cliente ----------

        [Fact]
        public async Task Meus_SoDoDonoEMesmoQueOLocacaoSejaDeOutroCliente()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[1], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> minhas = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO());

            Assert.Equal(new List<int> { 1 }, minhas.Itens.Select(x => x.IdPagamento).ToList());
        }

        [Fact]
        public async Task Meus_CaucaoAparecesSoEmTodos()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> todos = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "TODOS" });
            PaginaVO<ReturnMeuPagamentoVO> pendentes = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "PENDENTES" });
            PaginaVO<ReturnMeuPagamentoVO> pagos = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "PAGOS" });

            Assert.Equal(1, todos.Total);
            Assert.Equal(TipoPagamento.CAUCAO.ToString(), todos.Itens[0].Tipo);
            Assert.Equal(0, pendentes.Total);
            Assert.Equal(0, pagos.Total);
        }

        [Fact]
        public async Task Meus_PendentesIncluiAtrasadoEPendenteVencidoEPeriodoNaoCortaPendente()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 12, 1)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.ATRASADO, new DateOnly(2026, 10, 1)));
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2010, 1, 1)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> pendentes = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "PENDENTES", Periodo = "SEIS_MESES" });

            Assert.Equal(3, pendentes.Total);
            Assert.Equal(new List<string> { "PENDENTE", "ATRASADO", "ATRASADO" }, new List<string> { pendentes.Itens[0].Situacao, pendentes.Itens[1].Situacao, pendentes.Itens[2].Situacao });
        }

        [Fact]
        public async Task Meus_PagosRespeitaOPeriodo()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2010, 1, 1), new DateOnly(2010, 1, 1)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> seisMeses = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "PAGOS", Periodo = "SEIS_MESES" });
            PaginaVO<ReturnMeuPagamentoVO> tudo = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "PAGOS", Periodo = "TUDO" });

            Assert.Equal(new List<int> { 1 }, seisMeses.Itens.Select(x => x.IdPagamento).ToList());
            Assert.Equal(2, tudo.Total);
        }

        [Fact]
        public async Task Meus_SituacaoCalculadaNosCasosDoContrato()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.CANCELADO, new DateOnly(2026, 11, 1)));
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.ATRASADO, new DateOnly(2026, 12, 1)));
            repositorio.Pagamentos.Add(Cobranca(4, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 1)));
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 12, 1)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> todos = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "TODOS", Periodo = "TUDO" });
            Dictionary<int, string> situacoes = todos.Itens.ToDictionary(x => x.IdPagamento, x => x.Situacao);

            Assert.Equal(StatusPagamento.PAGO.ToString(), situacoes[1]);
            Assert.Equal(StatusPagamento.CANCELADO.ToString(), situacoes[2]);
            Assert.Equal(StatusPagamento.ATRASADO.ToString(), situacoes[3]);
            Assert.Equal(StatusPagamento.ATRASADO.ToString(), situacoes[4]);
            Assert.Equal(StatusPagamento.PENDENTE.ToString(), situacoes[5]);
        }

        [Fact]
        public async Task Meus_DiasParaVencerEVeiculoEFormaSaemCalculados()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 25)));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 11, 19)));
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 10)));
            PagamentoService servico = CriarServico(repositorio);

            PaginaVO<ReturnMeuPagamentoVO> pagina = await servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "TODOS" });
            Dictionary<int, ReturnMeuPagamentoVO> porId = pagina.Itens.ToDictionary(x => x.IdPagamento);

            Assert.Equal(6, porId[1].DiasParaVencer);
            Assert.Equal(0, porId[2].DiasParaVencer);
            Assert.Equal("Fiat Toro", porId[1].VeiculoDescricao);
            Assert.Equal("PIX", porId[3].Forma);
            Assert.Equal(StaticMethods.FormateDateToPTBR(new DateOnly(2026, 11, 25)), porId[1].DataVencimentoStr);
        }

        [Fact]
        public async Task Meus_AbaOuPeriodoInvalidos_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioComOrigens());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Aba = "pendentes" }));
            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.ListarMeusAsync(IdClienteDoTeste, new FiltroMeusPagamentosVO { Periodo = "MES" }));
        }

        // ---------- Resumo do cliente ----------

        private static FakePagamentoRepository RepositorioDoResumo()
        {
            FakePagamentoRepository repositorio = RepositorioComOrigens();
            repositorio.Pagamentos.Add(Cobranca(1, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2026, 3, 5), new DateOnly(2026, 3, 5), 100m));
            repositorio.Pagamentos.Add(Cobranca(2, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PAGO, new DateOnly(2025, 12, 20), new DateOnly(2025, 12, 20), 50m));
            repositorio.Pagamentos.Add(Cobranca(3, repositorio.Locacoes[0], TipoPagamento.CAUCAO, StatusPagamento.PAGO, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), 300m));
            repositorio.Pagamentos.Add(Cobranca(4, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.PENDENTE, new DateOnly(2026, 12, 1), null, 70m));
            repositorio.Pagamentos.Add(Cobranca(5, repositorio.Locacoes[0], TipoPagamento.DIARIA, StatusPagamento.ATRASADO, new DateOnly(2026, 10, 1), null, 30m));
            repositorio.Pagamentos.Add(Cobranca(6, repositorio.Locacoes[0], TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, new DateOnly(2026, 12, 2), null, 999m));

            return repositorio;
        }

        [Fact]
        public async Task Resumo_TotaisSemCaucaoEAnoPelaDataDoPagamento()
        {
            PagamentoService servico = CriarServico(RepositorioDoResumo());

            ReturnResumoMeusPagamentosVO resumo = await servico.ResumirMeusAsync(IdClienteDoTeste, new FiltroResumoMeusPagamentosVO());

            Assert.Equal(2026, resumo.AnoExercicio);
            Assert.Equal(100m, resumo.TotalLiquidadoAno);
            Assert.Equal(StaticMethods.FormateToMonetary(100m), resumo.TotalLiquidadoAnoStr);
            Assert.Equal(1, resumo.QuantidadeQuitadasAno);
            Assert.Equal(100m, resumo.TotalPendente);
            Assert.Equal(StaticMethods.FormateToMonetary(100m), resumo.TotalPendenteStr);
            Assert.Equal(2, resumo.QuantidadePendentes);
            Assert.Equal(2, resumo.QuantidadeAbaPendentes);
            Assert.Equal(2, resumo.QuantidadeAbaPagos);
        }

        [Fact]
        public async Task Resumo_PeriodoSoAfetaAContagemDaAbaPagos()
        {
            PagamentoService servico = CriarServico(RepositorioDoResumo());

            ReturnResumoMeusPagamentosVO anoAtual = await servico.ResumirMeusAsync(IdClienteDoTeste, new FiltroResumoMeusPagamentosVO { Periodo = "ANO_ATUAL" });

            Assert.Equal(1, anoAtual.QuantidadeAbaPagos);
            Assert.Equal(100m, anoAtual.TotalLiquidadoAno);
            Assert.Equal(2, anoAtual.QuantidadePendentes);
        }

        [Fact]
        public async Task Resumo_PeriodoInvalido_Recusa400()
        {
            PagamentoService servico = CriarServico(RepositorioDoResumo());

            await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => servico.ResumirMeusAsync(IdClienteDoTeste, new FiltroResumoMeusPagamentosVO { Periodo = "MES" }));
        }
    }
}
