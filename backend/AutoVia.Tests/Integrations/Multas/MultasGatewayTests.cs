using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Core.ServicesInterface;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Multas;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Extensions;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Multas;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Multas;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AutoVia.Tests.Integrations.Multas
{
    public class MultasGatewayTests
    {
        private sealed class ProvedorRoteirizado : IMultasProvider
        {
            public ProvedorRoteirizado(string nome) { Nome = nome; }
            public string Nome { get; }
            public MultasConsultaSolicitacao? SolicitacaoRecebida { get; private set; }
            public Exception? Falha { get; set; }
            public MultasConsultaResultado Resultado { get; set; } = new MultasConsultaResultado();

            public Task<MultasConsultaResultado> ConsultarInfracoesAsync(ConfiguracaoIntegracao configuracao, MultasConsultaSolicitacao solicitacao, CancellationToken cancellationToken = default)
            {
                SolicitacaoRecebida = solicitacao;
                if (Falha != null) { throw Falha; }
                return Task.FromResult(Resultado);
            }
        }

        [Fact]
        public async Task Consultar_NormalizaPlacaERetornaResultado()
        {
            DateTime inicio = new DateTime(2026, 11, 1, 10, 0, 0);
            MultaResultado infracao = CriarInfracao("ID-1", inicio.AddDays(1));
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake)
            {
                Resultado = new MultasConsultaResultado { Infracoes = new List<MultaResultado> { infracao } }
            };
            FakeLogService log = new FakeLogService();
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, ConstantsMultas.ProvedorFake, log, provider);

            MultasConsultaResultado resultado = await gateway.ConsultarInfracoesAsync(new MultasConsultaSolicitacao
            {
                Placa = "abc-1231", InicioLocal = inicio, FimLocal = inicio.AddDays(3)
            });

            Assert.Single(resultado.Infracoes);
            Assert.Equal("ABC1231", provider.SolicitacaoRecebida?.Placa);
            Assert.Empty(log.Entries);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ABC-12")]
        public async Task Consultar_PlacaInvalida_RejeitaAntesDoProvider(string placa)
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake);
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, ConstantsMultas.ProvedorFake, log, provider);

            await Assert.ThrowsAsync<BusinessException>(() => gateway.ConsultarInfracoesAsync(new MultasConsultaSolicitacao
            {
                Placa = placa, InicioLocal = new DateTime(2026, 11, 1), FimLocal = new DateTime(2026, 11, 2)
            }));
            Assert.Null(provider.SolicitacaoRecebida);
        }

        [Fact]
        public async Task Consultar_IntervaloInvertido_RejeitaAntesDoProvider()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake);
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, ConstantsMultas.ProvedorFake, log, provider);

            await Assert.ThrowsAsync<BusinessException>(() => gateway.ConsultarInfracoesAsync(new MultasConsultaSolicitacao
            {
                Placa = "ABC1231", InicioLocal = new DateTime(2026, 11, 2), FimLocal = new DateTime(2026, 11, 1)
            }));
            Assert.Null(provider.SolicitacaoRecebida);
        }

        [Fact]
        public async Task Consultar_FakeForaDoSandbox_RejeitaProvider()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake);
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.PRODUCAO, ConstantsMultas.ProvedorFake, log, provider);

            IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ConsultarInfracoesAsync(SolicitacaoValida()));
            Assert.Equal(ConstantsMessage.ErroMultasFakeForaDoSandbox, erro.Message);
            Assert.Null(provider.SolicitacaoRecebida);
        }

        [Fact]
        public async Task Consultar_ProvedorDesconhecido_RejeitaConfiguracao()
        {
            FakeLogService log = new FakeLogService();
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, "NAO_CADASTRADO", log);

            IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ConsultarInfracoesAsync(SolicitacaoValida()));
            Assert.Equal(ConstantsMultas.ChaveProvedor, erro.Chave);
        }

        [Fact]
        public async Task Consultar_FalhaDoProvider_RegistraLogSemPlaca()
        {
            FakeLogService log = new FakeLogService();
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake) { Falha = new InvalidOperationException("falha") };
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, ConstantsMultas.ProvedorFake, log, provider);

            IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ConsultarInfracoesAsync(SolicitacaoValida()));
            Assert.Equal(ConstantsMessage.ErroMultasConsulta, erro.Message);
            Assert.Single(log.Entries);
            Assert.DoesNotContain("ABC1231", string.Join(" ", log.Entries));
        }

        [Fact]
        public async Task Consultar_ResultadoForaDoPeriodo_RejeitaResposta()
        {
            FakeLogService log = new FakeLogService();
            DateTime foraDoPeriodo = new DateTime(2026, 11, 30);
            ProvedorRoteirizado provider = new ProvedorRoteirizado(ConstantsMultas.ProvedorFake)
            {
                Resultado = new MultasConsultaResultado { Infracoes = new List<MultaResultado> { CriarInfracao("ID-1", foraDoPeriodo) } }
            };
            MultasGateway gateway = CriarGateway(AmbienteIntegracao.SANDBOX, ConstantsMultas.ProvedorFake, log, provider);

            IntegrationException erro = await Assert.ThrowsAsync<IntegrationException>(() => gateway.ConsultarInfracoesAsync(SolicitacaoValida()));
            Assert.Equal(ConstantsMessage.ErroMultasResultadoInvalido, erro.Message);
        }

        [Fact]
        public async Task AddMultasIntegration_ResolveGatewayEFAKE_EExecutaConsultaSandbox()
        {
            DateTime inicio = new DateTime(2026, 11, 1, 10, 0, 0);
            Integration integracao = new Integration
            {
                IdIntegration = 1,
                Codigo = ConstantsIntegration.Multas,
                Nome = "Consulta de multas",
                Ambiente = AmbienteIntegracao.SANDBOX,
                BaseUrl = "https://sandbox.multas.exemplo.com.br",
                TimeoutMs = ConstantsIntegration.TimeoutPadraoMs,
                Ativo = true
            };
            integracao.Settings.Add(new IntegrationSetting
            {
                Chave = ConstantsMultas.ChaveProvedor,
                Valor = ConstantsMultas.ProvedorFake,
                Protegido = false
            });

            FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
            repositorio.Add(integracao);
            FakeLogService log = new FakeLogService();
            ServiceCollection services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddSingleton<ILogService>(log);
            services.AddSingleton<IIntegrationRepository>(repositorio);
            services.AddScoped<IIntegrationResolver, IntegrationResolver>();
            services.AddMultasIntegration();

            using ServiceProvider provider = services.BuildServiceProvider();
            IEnumerable<IMultasProvider> providers = provider.GetServices<IMultasProvider>();
            Assert.Single(providers);
            Assert.IsType<FakeMultasProvider>(providers.Single());

            IMultasGateway gateway = provider.GetRequiredService<IMultasGateway>();
            MultasConsultaResultado resultado = await gateway.ConsultarInfracoesAsync(new MultasConsultaSolicitacao
            {
                Placa = "ABC1231",
                InicioLocal = inicio,
                FimLocal = inicio.AddDays(1)
            });

            Assert.Single(resultado.Infracoes);
        }
        private static MultasGateway CriarGateway(AmbienteIntegracao ambiente, string? nomeProvider, ILogService log, params IMultasProvider[] providers)
        {
            Integration integration = new Integration
            {
                IdIntegration = 1, Codigo = ConstantsIntegration.Multas, Nome = "Consulta de multas",
                Ambiente = ambiente, BaseUrl = "https://sandbox.multas.exemplo.com.br",
                TimeoutMs = ConstantsIntegration.TimeoutPadraoMs, Ativo = true
            };
            if (nomeProvider != null)
            {
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsMultas.ChaveProvedor, Valor = nomeProvider, Protegido = false });
            }
            FakeIntegrationRepository repository = new FakeIntegrationRepository();
            repository.Add(integration);
            IntegrationResolver resolver = new IntegrationResolver(repository, new MemoryCache(new MemoryCacheOptions()), log);
            return new MultasGateway(resolver, providers, log);
        }

        private static MultasConsultaSolicitacao SolicitacaoValida()
        {
            DateTime inicio = new DateTime(2026, 11, 1, 10, 0, 0);
            return new MultasConsultaSolicitacao { Placa = "ABC1231", InicioLocal = inicio, FimLocal = inicio.AddDays(5) };
        }

        private static MultaResultado CriarInfracao(string identificador, DateTime dataHora)
        {
            return new MultaResultado
            {
                IdentificadorProvedor = identificador, DataHoraLocal = dataHora,
                Descricao = ConstantsMultas.DescricaoInfracaoFakeUm, Valor = ConstantsMultas.ValorInfracaoFakeUm
            };
        }
    }
}