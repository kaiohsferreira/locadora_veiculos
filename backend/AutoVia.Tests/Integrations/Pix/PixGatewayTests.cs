using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Pix
{
    public class PixGatewayTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";

        // ------------------------------------------------------------------
        // Escopo de teste: provedor roteirizado (sem rede) para observar a chamada.
        // ------------------------------------------------------------------

        private sealed class ProvedorRoteirizado : IPixProvider
        {
            public ProvedorRoteirizado(string nome)
            {
                Nome = nome;
            }

            public string Nome { get; }

            public int ChamadasCriar { get; private set; }

            public int ChamadasCancelar { get; private set; }

            public int ExpiracaoRecebida { get; private set; }

            public PixStatusCobranca StatusConsulta { get; set; } = PixStatusCobranca.AGUARDANDO_PAGAMENTO;

            public Exception? ExcecaoCriar { get; set; }

            public Task<PixCobrancaResultado> CriarAsync(ConfiguracaoIntegracao configuracao, PixCobrancaSolicitacao solicitacao, int expiracaoSegundos, CancellationToken cancellationToken = default)
            {
                ChamadasCriar++;
                ExpiracaoRecebida = expiracaoSegundos;

                if (ExcecaoCriar != null)
                {
                    throw ExcecaoCriar;
                }

                return Task.FromResult(new PixCobrancaResultado
                {
                    IdentificadorProvedor = "ID-1",
                    ReferenciaExterna = solicitacao.ReferenciaExterna,
                    Valor = solicitacao.Valor,
                    Provedor = Nome,
                    Status = PixStatusCobranca.AGUARDANDO_PAGAMENTO
                });
            }

            public Task<PixCobrancaResultado> ConsultarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new PixCobrancaResultado
                {
                    IdentificadorProvedor = identificadorProvedor,
                    Provedor = Nome,
                    Status = StatusConsulta
                });
            }

            public Task<PixCobrancaResultado> CancelarAsync(ConfiguracaoIntegracao configuracao, string identificadorProvedor, CancellationToken cancellationToken = default)
            {
                ChamadasCancelar++;

                return Task.FromResult(new PixCobrancaResultado
                {
                    IdentificadorProvedor = identificadorProvedor,
                    Provedor = Nome,
                    Status = PixStatusCobranca.CANCELADA
                });
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Integration CriarIntegracao(string? provedor, AmbienteIntegracao ambiente, string? expiracao = null)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Pagamento,
                Nome = "Pagamento",
                Ambiente = ambiente,
                BaseUrl = "https://sandbox.exemplo.com.br",
                TimeoutMs = 15000,
                Ativo = true
            };

            if (provedor != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveProvedor, Valor = provedor, Protegido = false });
            }

            integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChaveAccessToken, Valor = TokenSecreto, Protegido = true });

            if (expiracao != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsPix.ChavePixExpiracaoSegundos, Valor = expiracao, Protegido = false });
            }

            return integration;
        }

        private static FakeIntegrationRepository CriarRepositorio(Integration integration)
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integration);
            return repositorio;
        }

        private static PixGateway CriarGateway(FakeIntegrationRepository repositorio, ILogService logService, params IPixProvider[] provedores)
        {
            IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), logService);
            return new PixGateway(resolver, provedores, logService);
        }

        private static PixCobrancaSolicitacao CriarSolicitacao()
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = "PED-2026-0001",
                Valor = 10.5m,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador { Nome = "Maria da Silva", Email = "maria@exemplo.com", Documento = "12345678901" }
            };
        }

        // ------------------------------------------------------------------
        // Validacao da solicitacao
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task CriarCobrancaAsync_ValorNaoPositivo_LancaValorInvalido(double valor)
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Valor = (decimal)valor;
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixValorInvalido, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCriar);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ValorComTresCasas_LancaValorInvalido()
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Valor = 10.555m;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixValorInvalido, excecao.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("PED 0001")]
        [InlineData("PÉD-0001")]
        [InlineData("PED/0001")]
        public async Task CriarCobrancaAsync_ReferenciaComCaracterInvalido_LancaReferenciaInvalida(string referencia)
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.ReferenciaExterna = referencia;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixReferenciaInvalida, excecao.Message);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ReferenciaComMaisDe64Caracteres_LancaReferenciaInvalida()
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.ReferenciaExterna = new string('a', ConstantsPix.ReferenciaTamanhoMaximo + 1);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixReferenciaInvalida, excecao.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CriarCobrancaAsync_DescricaoVazia_LancaDescricaoInvalida(string descricao)
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Descricao = descricao;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixDescricaoInvalida, excecao.Message);
        }

        [Fact]
        public async Task CriarCobrancaAsync_DescricaoLonga_LancaDescricaoInvalida()
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Descricao = new string('d', ConstantsPix.DescricaoTamanhoMaximo + 1);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixDescricaoInvalida, excecao.Message);
        }

        [Theory]
        [InlineData("sem-arroba")]
        [InlineData("")]
        public async Task CriarCobrancaAsync_EmailInvalido_LancaPagadorInvalido(string email)
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Pagador.Email = email;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixPagadorInvalido, excecao.Message);
        }

        [Fact]
        public async Task CriarCobrancaAsync_CpfComTamanhoErrado_LancaPagadorInvalido()
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Pagador.Documento = "123";
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixPagadorInvalido, excecao.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2592001)]
        public async Task CriarCobrancaAsync_ExpiracaoForaDaFaixa_LancaExpiracaoInvalida(int expiracao)
        {
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.ExpiracaoSegundos = expiracao;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CriarCobrancaAsync(solicitacao));

            Assert.Equal(ConstantsMessage.ErroPixExpiracaoInvalida, excecao.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public async Task ConsultarCobrancaAsync_IdentificadorVazio_LancaIdentificadorObrigatorio(string identificador)
        {
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.ConsultarCobrancaAsync(identificador));

            Assert.Equal(ConstantsMessage.ErroPixIdentificadorObrigatorio, excecao.Message);
        }

        [Fact]
        public async Task CancelarCobrancaAsync_IdentificadorVazio_LancaIdentificadorObrigatorio()
        {
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarCobrancaAsync(string.Empty));

            Assert.Equal(ConstantsMessage.ErroPixIdentificadorObrigatorio, excecao.Message);
        }

        // ------------------------------------------------------------------
        // Configuracao da integracao PAGAMENTO
        // ------------------------------------------------------------------

        [Fact]
        public async Task CriarCobrancaAsync_SemExpiracaoNaSolicitacaoNemNaConfiguracao_UsaPadraoDe3600()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), provedor);

            PixCobrancaResultado resultado = await gateway.CriarCobrancaAsync(CriarSolicitacao());

            Assert.Equal(3600, provedor.ExpiracaoRecebida);
            Assert.Equal(ConstantsPix.ProvedorMercadoPago, resultado.Provedor);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ExpiracaoDaConfiguracao_EhUsadaQuandoASolicitacaoNaoInforma()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO, "1800")), new FakeLogService(), provedor);

            await gateway.CriarCobrancaAsync(CriarSolicitacao());

            Assert.Equal(1800, provedor.ExpiracaoRecebida);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ExpiracaoDaSolicitacao_PrevaleceSobreAConfiguracao()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO, "abc")), new FakeLogService(), provedor);
            PixCobrancaSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.ExpiracaoSegundos = 600;

            await gateway.CriarCobrancaAsync(solicitacao);

            Assert.Equal(600, provedor.ExpiracaoRecebida);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("2592001")]
        public async Task CriarCobrancaAsync_ExpiracaoDeConfiguracaoInvalida_LancaIntegrationException(string expiracao)
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO, expiracao)), new FakeLogService(), provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroPixExpiracaoConfiguracaoInvalida, excecao.Message);
            Assert.Equal(ConstantsPix.ChavePixExpiracaoSegundos, excecao.Chave);
            Assert.Equal(0, provedor.ChamadasCriar);
        }

        [Fact]
        public async Task CriarCobrancaAsync_SemChaveProvedor_LancaIntegrationExceptionComAChave()
        {
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(null, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsPix.ChaveProvedor, excecao.Chave);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ProvedorDesconhecido_LancaProvedorNaoSuportado()
        {
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao("OUTRO-BANCO", AmbienteIntegracao.PRODUCAO)), new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroPixProvedorNaoSuportado, excecao.Message);
        }

        [Fact]
        public async Task CriarCobrancaAsync_FakeForaDoSandbox_LancaFakeForaDoSandbox()
        {
            ProvedorRoteirizado fake = new ProvedorRoteirizado(ConstantsPix.ProvedorFake);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorFake, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), fake);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroPixFakeForaDoSandbox, excecao.Message);
            Assert.Equal(0, fake.ChamadasCriar);
        }

        [Fact]
        public async Task CriarCobrancaAsync_FakeNoSandbox_EhAceito()
        {
            ProvedorRoteirizado fake = new ProvedorRoteirizado(ConstantsPix.ProvedorFake);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorFake, AmbienteIntegracao.SANDBOX)), new FakeLogService(), fake);

            PixCobrancaResultado resultado = await gateway.CriarCobrancaAsync(CriarSolicitacao());

            Assert.Equal(1, fake.ChamadasCriar);
            Assert.Equal(ConstantsPix.ProvedorFake, resultado.Provedor);
        }

        [Fact]
        public async Task CriarCobrancaAsync_IntegracaoInexistente_RepassaAExcecaoDoResolver()
        {
            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            PixGateway gateway = CriarGateway(repositorio, new FakeLogService(), new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago));

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(string.Format(ConstantsMessageIntegration.ErroIntegracaoNaoEncontrada, ConstantsIntegration.Pagamento), excecao.Message);
        }

        [Fact]
        public async Task CriarCobrancaAsync_ExcecaoInesperadaDoProvedor_LogaEEnvolveComInner()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            provedor.ExcecaoCriar = new InvalidOperationException("falha interna");
            FakeLogService logService = new FakeLogService();
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), logService, provedor);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => gateway.CriarCobrancaAsync(CriarSolicitacao()));

            Assert.Equal(ConstantsMessage.ErroPixCriarCobranca, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);
            Assert.Contains(logService.Entries, entrada => entrada.Contains(ConstantsMessage.ErroPixCriarCobranca));
        }

        // ------------------------------------------------------------------
        // Cancelamento
        // ------------------------------------------------------------------

        [Fact]
        public async Task CancelarCobrancaAsync_CobrancaPaga_LancaCancelarPagaSemChamarOCancelamento()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            provedor.StatusConsulta = PixStatusCobranca.PAGA;
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), provedor);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => gateway.CancelarCobrancaAsync("ID-1"));

            Assert.Equal(ConstantsMessage.ErroPixCancelarCobrancaPaga, excecao.Message);
            Assert.Equal(0, provedor.ChamadasCancelar);
        }

        [Fact]
        public async Task CancelarCobrancaAsync_CobrancaAguardando_CancelaNoProvedor()
        {
            ProvedorRoteirizado provedor = new ProvedorRoteirizado(ConstantsPix.ProvedorMercadoPago);
            PixGateway gateway = CriarGateway(CriarRepositorio(CriarIntegracao(ConstantsPix.ProvedorMercadoPago, AmbienteIntegracao.PRODUCAO)), new FakeLogService(), provedor);

            PixCobrancaResultado resultado = await gateway.CancelarCobrancaAsync("ID-1");

            Assert.Equal(PixStatusCobranca.CANCELADA, resultado.Status);
            Assert.Equal(1, provedor.ChamadasCancelar);
        }
    }
}
