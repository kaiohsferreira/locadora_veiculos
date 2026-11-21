using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using AutoVia.Tests.Integrations.Google;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Decisao 1 de 21/11/2026 ('Bloqueado, com chave') provada no servico de cobranca: gateway PIX real e provedor FAKE real,
    /// integracao PAGAMENTO em SANDBOX e ambiente Development. Sem a chave Pagamentos:PermitirProvedorFake a geracao recusa com
    /// o erro de integracao do padrao e nada e gravado (nem na cobranca, nem no provedor de teste). Com a chave, responde.
    /// </summary>
    public class PixFakeBloqueadoServicoTests
    {
        private static readonly DateTime Agora = new DateTime(2026, 11, 19, 10, 0, 0);
        private const int IdCobranca = 5;

        private static IConfiguration CriarConfiguracaoDaChave(string? valor)
        {
            Dictionary<string, string?> valores = new Dictionary<string, string?>();

            if (valor != null)
            {
                valores.Add(ConstantsPix.ChavePermitirProvedorFake, valor);
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        }

        private static FakePagamentoRepository RepositorioComCobranca()
        {
            Cliente cliente = new Cliente { IdCliente = 7, Nome = "Maria da Silva", Cpf = "12345678901", Email = "maria@exemplo.com" };
            Locacao locacao = new Locacao { IdLocacao = 1, IdCliente = 7, Cliente = cliente, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" } };
            FakePagamentoRepository repositorio = new FakePagamentoRepository();
            repositorio.Locacoes.Add(locacao);
            repositorio.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdCobranca,
                IdLocacao = locacao.IdLocacao,
                Locacao = locacao,
                Tipo = TipoPagamento.DIARIA,
                Status = StatusPagamento.PENDENTE,
                Valor = 395.60m,
                DataVencimento = new DateOnly(2026, 11, 22),
                CreatedAt = Agora,
                UpdatedAt = Agora
            });

            return repositorio;
        }

        private static Integration CriarIntegracaoFake()
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = AmbienteIntegracao.SANDBOX,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 15000,
                Ativo = true
            };

            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveProvedor, Valor = ConstantsPix.ProvedorFake, Protegido = false });
            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveAccessToken, Valor = "token-de-teste-nao-usado", Protegido = true });

            return integration;
        }

        private static CobrancaPixService CriarServico(FakePagamentoRepository repositorio, PixFakeStore store, IConfiguration configuracao)
        {
            FakeIntegrationRepository integracoes = new FakeIntegrationRepository();
            integracoes.Add(CriarIntegracaoFake());
            IntegrationResolver resolver = new IntegrationResolver(integracoes, new MemoryCache(new MemoryCacheOptions()), new FakeLogService());
            FakePixProvider provider = new FakePixProvider(store, new GoogleProvaDeTeste.AmbienteFalso(Environments.Development), configuracao, new FakeLogService());
            PixGateway gateway = new PixGateway(resolver, new IPixProvider[] { provider }, new FakeLogService());

            MapperConfiguration mapeamento = new MapperConfiguration(cfg => cfg.AddProfile<PagamentoProfile>(), NullLoggerFactory.Instance);
            IMapper mapper = mapeamento.CreateMapper();

            return new CobrancaPixService(repositorio, gateway, mapper, new FakeRelogio(Agora), new FakeLogService());
        }

        [Fact]
        public async Task GerarPix_SemAChave_RecusaComErroDeIntegracaoESemGravarNada()
        {
            PixFakeStore store = new PixFakeStore();
            FakePagamentoRepository repositorio = RepositorioComCobranca();
            CobrancaPixService servico = CriarServico(repositorio, store, CriarConfiguracaoDaChave(null));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.GerarOuReabrirDaLocadoraAsync(IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Null(repositorio.Pagamentos[0].IdTransacao);
            Assert.Null(repositorio.Pagamentos[0].PixCopiaECola);
            Assert.Equal(0, repositorio.GravacoesDePix);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task GerarPix_ComAChaveFalse_RecusaSemGravarNada()
        {
            PixFakeStore store = new PixFakeStore();
            FakePagamentoRepository repositorio = RepositorioComCobranca();
            CobrancaPixService servico = CriarServico(repositorio, store, CriarConfiguracaoDaChave("false"));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => servico.GerarOuReabrirDaLocadoraAsync(IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Null(repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(0, store.Quantidade);
        }

        [Fact]
        public async Task GerarPix_ComAChaveVerdadeira_DevolveOCopiaEColaDeTeste()
        {
            PixFakeStore store = new PixFakeStore();
            FakePagamentoRepository repositorio = RepositorioComCobranca();
            CobrancaPixService servico = CriarServico(repositorio, store, CriarConfiguracaoDaChave("true"));

            ReturnCobrancaPixFuncionarioVO saida = await servico.GerarOuReabrirDaLocadoraAsync(IdCobranca);

            Assert.False(string.IsNullOrWhiteSpace(saida.CopiaECola));
            Assert.Equal(1, store.Quantidade);
            Assert.Equal(1, repositorio.GravacoesDePix);
        }
    }
}
