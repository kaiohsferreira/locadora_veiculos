using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Decisao 2 de 21/11/2026 ('Sim, cancelar antes'): a baixa manual com PIX gerado cancela o PIX no provedor ANTES da baixa.
    /// Um teste por caso da tabela baixa_manual_cancela_pix_antes do contrato pagamento v3. Cada teste confere o que ficou
    /// gravado e quantas vezes o provedor foi chamado (FakePixGateway), com o repositorio em memoria e o relogio fixo.
    /// </summary>
    public class BaixaComPixCanceladoAntesTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 19, 10, 0, 0);
        private const int IdClienteDoTeste = 7;
        private const int IdCobranca = 5;
        private const string IdTransacaoAntiga = "tx-antiga";
        private const string IdTransacaoNova = "tx-nova";

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<PagamentoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static PagamentoService CriarServico(FakePagamentoRepository repositorio, FakePixGateway gateway)
        {
            return new PagamentoService(repositorio, CriarMapper(), new FakeRelogio(Agora), ContextoEmpresaDeTeste.Criar(1, CargoFuncionario.FINANCEIRO), new FakeLogService(), gateway);
        }

        /// <summary>Semeia a cobranca com locacao do cliente. Com minutosAteExpirar, grava PIX (id e validade); sem, nao grava PIX.</summary>
        private static FakePagamentoRepository RepositorioComCobranca(StatusPagamento status, string? idTransacao, int? minutosAteExpirar, TipoPagamento tipo = TipoPagamento.DIARIA)
        {
            FakePagamentoRepository repositorio = new FakePagamentoRepository();
            Cliente cliente = new Cliente { IdCliente = IdClienteDoTeste, Nome = "Maria Souza", Cpf = "12345678901", Email = "maria@exemplo.com" };
            Locacao locacao = new Locacao { IdLocacao = 1, IdCliente = IdClienteDoTeste, Cliente = cliente, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" } };
            repositorio.Locacoes.Add(locacao);
            repositorio.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdCobranca,
                IdLocacao = locacao.IdLocacao,
                Locacao = locacao,
                Tipo = tipo,
                Status = status,
                Valor = 100m,
                DataVencimento = new DateOnly(2026, 11, 25),
                IdTransacao = idTransacao,
                PixExpiraEm = minutosAteExpirar.HasValue ? Agora.AddMinutes(minutosAteExpirar.Value) : null,
                CreatedAt = Agora,
                UpdatedAt = Agora
            });

            return repositorio;
        }

        private static BaixarPagamentoVO Baixa(string forma, bool confirmar = false)
        {
            return new BaixarPagamentoVO { Forma = forma, ConfirmarBaixaComPixGerado = confirmar };
        }

        [Fact]
        public async Task Baixa_DinheiroComPixValidoSemConfirmacao_Devolve409SemChamarOProvedor()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO")));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaComPixGerado, excecao.Message);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
        }

        [Fact]
        public async Task Baixa_DinheiroComPixValidoComConfirmacao_CancelaNoProvedorAntesDaBaixa()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            string? statusNoCancelamento = null;
            gateway.AoCancelar = (string identificador) => statusNoCancelamento = repositorio.Pagamentos[0].Status.ToString();
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true));

            Assert.Equal(new List<string> { IdTransacaoAntiga }, gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE.ToString(), statusNoCancelamento);
            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
            Assert.Equal("DINHEIRO", baixado.Forma);
            Assert.Equal(StatusPagamento.PAGO, repositorio.Pagamentos[0].Status);
        }

        [Fact]
        public async Task Baixa_ProvedorInformaExpirada_SegueABaixa()
        {
            FakePixGateway gateway = new FakePixGateway
            {
                RespostaCancelamento = (string identificador) => new PixCobrancaResultado { IdentificadorProvedor = identificador, Status = PixStatusCobranca.EXPIRADA }
            };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true));

            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
            Assert.Single(gateway.Cancelamentos);
        }

        [Fact]
        public async Task Baixa_ProvedorNaoConheceOPix_Devolve502ComTextoNovoSemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway { ExcecaoCancelamento = new NotFoundException(ConstantsMessage.ErroPixCobrancaNaoEncontrada) };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            StatusPagamento statusAntes = repositorio.Pagamentos[0].Status;
            string? formaAntes = repositorio.Pagamentos[0].Forma;
            string? idTransacaoAntes = repositorio.Pagamentos[0].IdTransacao;
            DateTime? validadeAntes = repositorio.Pagamentos[0].PixExpiraEm;
            PagamentoService servico = CriarServico(repositorio, gateway);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, excecao.Message);
            Assert.IsType<NotFoundException>(excecao.InnerException);
            Assert.Single(gateway.Cancelamentos);
            Assert.Equal(0, repositorio.Baixas);
            Assert.Equal(statusAntes, repositorio.Pagamentos[0].Status);
            Assert.Equal(formaAntes, repositorio.Pagamentos[0].Forma);
            Assert.Equal(idTransacaoAntes, repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(validadeAntes, repositorio.Pagamentos[0].PixExpiraEm);
        }

        [Fact]
        public async Task Baixa_ProvedorInformaPaga_Devolve409ComTextoNovoSemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway { ExcecaoCancelamento = new BusinessException(ConstantsMessage.ErroPixCancelarCobrancaPaga) };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaPixJaPago, excecao.Message);
            Assert.Single(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_ProvedorDevolveStatusPagaNoRetorno_Devolve409ComTextoNovoSemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway
            {
                RespostaCancelamento = (string identificador) => new PixCobrancaResultado { IdentificadorProvedor = identificador, Status = PixStatusCobranca.PAGA }
            };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaPixJaPago, excecao.Message);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_FalhaDoProvedor_Devolve502ComTextoNovoSemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway { FalharCancelamento = true };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, excecao.Message);
            Assert.Single(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_ProvedorDevolveStatusNaoEncerrado_Devolve502SemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway
            {
                RespostaCancelamento = (string identificador) => new PixCobrancaResultado { IdentificadorProvedor = identificador, Status = PixStatusCobranca.AGUARDANDO_PAGAMENTO }
            };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, excecao.Message);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_PixExpiradoPelaValidadeSalvaComConfirmacao_NaoChamaOProvedorESegue()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, -1);
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true));

            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
        }

        [Fact]
        public async Task Baixa_PixExpiradoPelaValidadeSalvaSemConfirmacao_Devolve409SemChamarOProvedor()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, -1);
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO")));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaComPixGerado, excecao.Message);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
        }

        [Fact]
        public async Task Baixa_FormaPixComPixValido_NaoCancelaESegue()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("PIX"));

            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
            Assert.Equal("PIX", baixado.Forma);
        }

        [Fact]
        public async Task Baixa_SemPixGravado_NaoChamaOProvedorESegue()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, null, null);
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO"));

            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
        }

        [Fact]
        public async Task Baixa_CorridaComOutroPixValido_Devolve409SemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            gateway.AoCancelar = (string identificador) =>
            {
                repositorio.Pagamentos[0].IdTransacao = IdTransacaoNova;
                repositorio.Pagamentos[0].PixExpiraEm = Agora.AddMinutes(15);
            };
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaComPixGerado, excecao.Message);
            Assert.Equal(new List<string> { IdTransacaoAntiga }, gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(IdTransacaoNova, repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(1, repositorio.Baixas);
        }

        [Theory]
        [InlineData(StatusPagamento.PAGO, TipoPagamento.DIARIA, ConstantsMessage.ErroPagamentoJaPago)]
        [InlineData(StatusPagamento.CANCELADO, TipoPagamento.DIARIA, ConstantsMessage.ErroPagamentoCancelado)]
        [InlineData(StatusPagamento.PENDENTE, TipoPagamento.CAUCAO, ConstantsMessage.ErroPagamentoBaixaCaucao)]
        public async Task Baixa_CobrancaPagaCanceladaOuCaucaoComPix_Devolve409SemChamarOProvedor(StatusPagamento status, TipoPagamento tipo, string erroEsperado)
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(status, IdTransacaoAntiga, 15, tipo);
            PagamentoService servico = CriarServico(repositorio, gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(erroEsperado, excecao.Message);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(status, repositorio.Pagamentos[0].Status);
        }

        [Fact]
        public async Task Baixa_CobrancaInexistente_Devolve404SemChamarOProvedor()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => servico.BaixarAsync(IdCobranca + 900, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoNaoEncontrado, excecao.Message);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_ProvedorInformaRecusadaNoCancelamento_SegueABaixa()
        {
            FakePixGateway gateway = new FakePixGateway
            {
                RespostaCancelamento = (string identificador) => new PixCobrancaResultado { IdentificadorProvedor = identificador, Status = PixStatusCobranca.RECUSADA }
            };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            ReturnPagamentoVO baixado = await servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true));

            Assert.Equal(StatusPagamento.PAGO.ToString(), baixado.Status);
            Assert.Single(gateway.Cancelamentos);
        }

        [Fact]
        public async Task Baixa_ProvedorRecusaOCancelamentoComErroDeNegocio_Devolve502ComTextoNovoSemBaixa()
        {
            FakePixGateway gateway = new FakePixGateway { ExcecaoCancelamento = new BusinessException(ConstantsMessage.ErroPixCobrancaRecusada) };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, excecao.Message);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(0, repositorio.Baixas);
        }

        [Fact]
        public async Task Baixa_FalhaDoProvedor_PreservaACausaNaCadeiaDeExcecao()
        {
            FakePixGateway gateway = new FakePixGateway { FalharCancelamento = true };
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            PagamentoService servico = CriarServico(repositorio, gateway);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCancelarPixFalhou, excecao.Message);
            Assert.NotNull(excecao.InnerException);
            Assert.Equal(ConstantsMessage.ErroPixProvedorIndisponivel, excecao.InnerException.Message);
        }

        [Fact]
        public async Task Baixa_FalhaDepoisDoCancelamento_LimpaOIdCanceladoSemPagarEMantemAValidade()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            repositorio.FalharBaixa = true;
            PagamentoService servico = CriarServico(repositorio, gateway);

            await Assert.ThrowsAsync<InvalidOperationException>(() => servico.BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            Assert.Equal(new List<string> { IdTransacaoAntiga }, gateway.Cancelamentos);
            Assert.Null(repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(Agora.AddMinutes(15), repositorio.Pagamentos[0].PixExpiraEm);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
        }

        [Fact]
        public async Task ReabrirAposBaixaFalhadaComPixCancelado_GeraPixNovoComAReferenciaDaValidadeGravada()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCobranca(StatusPagamento.PENDENTE, IdTransacaoAntiga, 15);
            repositorio.FalharBaixa = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => CriarServico(repositorio, gateway).BaixarAsync(IdCobranca, Baixa("DINHEIRO", true)));

            repositorio.FalharBaixa = false;
            CobrancaPixService reabertura = new CobrancaPixService(repositorio, gateway, CriarMapper(), new FakeRelogio(Agora), new FakeLogService());
            await reabertura.GerarOuReabrirDaLocadoraAsync(IdCobranca);

            Assert.Single(gateway.Solicitacoes);
            Assert.Equal(RegrasPagamento.ReferenciaDaCobranca(IdCobranca, Agora.AddMinutes(15)), gateway.Solicitacoes[0].ReferenciaExterna);
            Assert.NotEqual(IdTransacaoAntiga, repositorio.Pagamentos[0].IdTransacao);
        }
    }
}
