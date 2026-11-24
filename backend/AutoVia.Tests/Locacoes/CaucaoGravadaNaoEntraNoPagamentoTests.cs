using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Regressao da caucao gravada na abertura (contrato caucao-retirada v1.1, testes_obrigatorios.regressao): uma linha CAUCAO
    /// com forma CARTAO e id_transacao do provedor (FAKE-CAU-...) continua recusada pela baixa e pela geracao de PIX ANTES de
    /// olhar o id, sem chamar o provedor do PIX e sem mudar nada.
    /// </summary>
    public class CaucaoGravadaNaoEntraNoPagamentoTests
    {
        private const int IdCobranca = 5;
        private static readonly DateTime Agora = new DateTime(2026, 11, 24, 10, 0, 0);

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<PagamentoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        /// <summary>A CAUCAO como a abertura grava: PENDENTE, CARTAO, id do provedor, vencimento na devolucao prevista e sem PIX.</summary>
        private static FakePagamentoRepository RepositorioComCaucaoGravada()
        {
            FakePagamentoRepository repositorio = new FakePagamentoRepository();
            Cliente cliente = new Cliente { IdCliente = 7, Nome = "Maria Souza", Cpf = "12345678901", Email = "maria@exemplo.com" };
            Locacao locacao = new Locacao { IdLocacao = 1, IdCliente = 7, Cliente = cliente, ValorCaucao = 1200.00m, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" } };
            repositorio.Locacoes.Add(locacao);
            repositorio.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdCobranca,
                IdLocacao = locacao.IdLocacao,
                Locacao = locacao,
                Tipo = TipoPagamento.CAUCAO,
                Status = StatusPagamento.PENDENTE,
                Forma = ConstantsValues.FormaCartao,
                Valor = 1200.00m,
                DataVencimento = new DateOnly(2026, 11, 27),
                IdTransacao = FakeCaucaoGateway.IdentificadorPadrao,
                CreatedAt = Agora,
                UpdatedAt = Agora
            });

            return repositorio;
        }

        [Fact]
        public async Task Baixa_DeCaucaoComIdDoProvedor_409SemChamarOProvedorNemMudarNada()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCaucaoGravada();
            PagamentoService servico = new PagamentoService(repositorio, CriarMapper(), new FakeRelogio(Agora), ContextoEmpresaDeTeste.Criar(1, CargoFuncionario.FINANCEIRO), new FakeLogService(), gateway);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.BaixarAsync(IdCobranca, new BaixarPagamentoVO { Forma = ConstantsValues.FormaCartao, ConfirmarBaixaComPixGerado = true }));

            Assert.Equal(ConstantsMessage.ErroPagamentoBaixaCaucao, excecao.Message);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(StatusPagamento.PENDENTE, repositorio.Pagamentos[0].Status);
            Assert.Equal(FakeCaucaoGateway.IdentificadorPadrao, repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task GeracaoDePix_DeCaucaoComIdDoProvedor_409SemChamarOProvedorNemMudarNada()
        {
            FakePixGateway gateway = new FakePixGateway();
            FakePagamentoRepository repositorio = RepositorioComCaucaoGravada();
            CobrancaPixService servico = new CobrancaPixService(repositorio, gateway, CriarMapper(), new FakeRelogio(Agora), new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => servico.GerarOuReabrirDaLocadoraAsync(IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixTipoNaoPagavel, excecao.Message);
            Assert.Empty(gateway.Solicitacoes);
            Assert.Empty(gateway.Cancelamentos);
            Assert.Equal(FakeCaucaoGateway.IdentificadorPadrao, repositorio.Pagamentos[0].IdTransacao);
            Assert.Null(repositorio.Pagamentos[0].PixCopiaECola);
        }
    }
}
