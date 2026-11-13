using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Seguradora;
using AutoVia.Integrations.Entities;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Seguradora;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services;
using AutoVia.Integrations.Services.Seguradora;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;

namespace AutoVia.Tests.Integrations.Seguradora
{
    /// <summary>
    /// Testes do SeguradoraGateway com o provedor FAKE real e a configuracao em memoria:
    /// vigencia (vigente e marcador VENCIDA), abertura idempotente, acompanhamento com a
    /// linha do tempo, validacoes (409) e erros de configuracao (502).
    /// </summary>
    public class SeguradoraGatewayTests
    {
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Now);

        [Fact]
        public async Task ConsultarVigencia_ApoliceNormal_DevolveVigente()
        {
            Cenario cenario = new Cenario();

            VigenciaApoliceResultado resultado = await cenario.Gateway.ConsultarVigenciaAsync("AP-2026-0001", Hoje);

            Assert.True(resultado.Vigente);
            Assert.Equal(ConstantsSeguradora.ProvedorFake, resultado.Provedor);
            Assert.Equal(Hoje, resultado.DataReferencia);
        }

        [Fact]
        public async Task ConsultarVigencia_ApoliceComMarcadorVencida_DevolveNaoVigente()
        {
            Cenario cenario = new Cenario();

            VigenciaApoliceResultado resultado = await cenario.Gateway.ConsultarVigenciaAsync("AP-vencida-01", Hoje);

            Assert.False(resultado.Vigente);
        }

        [Fact]
        public async Task ConsultarVigencia_SemData_LancaBusinessException()
        {
            Cenario cenario = new Cenario();

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Gateway.ConsultarVigenciaAsync("AP-1", default(DateOnly)));

            Assert.Equal(ConstantsMessage.ErroSeguradoraDataReferenciaObrigatoria, excecao.Message);
        }

        [Fact]
        public async Task AbrirSinistro_DevolveProtocoloFakeComStatusAbertoEEventoDeAbertura()
        {
            Cenario cenario = new Cenario();

            SinistroSeguradoraResultado resultado = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-1"));

            Assert.StartsWith(ConstantsSeguradora.PrefixoProtocoloFake, resultado.Protocolo);
            Assert.Equal(ConstantsSeguradora.PrefixoProtocoloFake.Length + ConstantsSeguradora.TamanhoHashProtocoloFake, resultado.Protocolo.Length);
            Assert.Equal(StatusSinistro.ABERTO, resultado.Status);
            SinistroEventoSeguradora abertura = Assert.Single(resultado.Eventos);
            Assert.Null(abertura.StatusAnterior);
            Assert.Equal(StatusSinistro.ABERTO, abertura.StatusNovo);
        }

        [Fact]
        public async Task AbrirSinistro_MesmaReferencia_DevolveMesmoProtocoloSemNovoEvento()
        {
            Cenario cenario = new Cenario();

            SinistroSeguradoraResultado primeiro = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-2"));
            SinistroSeguradoraResultado segundo = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-2"));
            SinistroSeguradoraResultado outro = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-3"));

            Assert.Equal(primeiro.Protocolo, segundo.Protocolo);
            Assert.Single(segundo.Eventos);
            Assert.NotEqual(primeiro.Protocolo, outro.Protocolo);
        }

        [Fact]
        public async Task ConsultarSinistro_DepoisDeAvancos_TrazLinhaDoTempoEmOrdem()
        {
            Cenario cenario = new Cenario();
            SinistroSeguradoraResultado aberto = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-4"));
            cenario.Store.AvancarStatus(aberto.Protocolo, StatusSinistro.EM_ANALISE, "Vistoria agendada");
            cenario.Store.AvancarStatus(aberto.Protocolo, StatusSinistro.APROVADO, null);

            SinistroSeguradoraResultado consultado = await cenario.Gateway.ConsultarSinistroAsync("  " + aberto.Protocolo + " ");

            Assert.Equal(StatusSinistro.APROVADO, consultado.Status);
            Assert.Equal(3, consultado.Eventos.Count);
            Assert.Equal(StatusSinistro.ABERTO, consultado.Eventos[1].StatusAnterior);
            Assert.Equal(StatusSinistro.EM_ANALISE, consultado.Eventos[1].StatusNovo);
            Assert.Equal("Vistoria agendada", consultado.Eventos[1].Observacao);
            Assert.Equal(StatusSinistro.EM_ANALISE, consultado.Eventos[2].StatusAnterior);
            Assert.Equal(StatusSinistro.APROVADO, consultado.Eventos[2].StatusNovo);
            Assert.True(consultado.Eventos[1].DataAlteracao <= consultado.Eventos[2].DataAlteracao);
        }

        [Fact]
        public async Task ConsultarSinistro_ProtocoloInexistente_LancaNotFound()
        {
            Cenario cenario = new Cenario();

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.Gateway.ConsultarSinistroAsync("FAKE-SIN-000000000000"));

            Assert.Equal(ConstantsMessage.ErroSeguradoraSinistroNaoEncontrado, excecao.Message);
        }

        [Fact]
        public async Task ConsultarSinistro_ProtocoloVazio_LancaBusinessException()
        {
            Cenario cenario = new Cenario();

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Gateway.ConsultarSinistroAsync("   "));
        }

        [Theory]
        [InlineData("referencia")]
        [InlineData("apolice")]
        [InlineData("placa")]
        [InlineData("dataFutura")]
        [InlineData("dataVazia")]
        [InlineData("tipo")]
        [InlineData("descricao")]
        [InlineData("valor")]
        public async Task AbrirSinistro_SolicitacaoInvalida_LancaBusinessException(string campo)
        {
            Cenario cenario = new Cenario();
            SinistroAberturaSolicitacao solicitacao = CriarSolicitacao("SIN-5");

            if (campo == "referencia") solicitacao.ReferenciaExterna = "SIN 5 com espaco";
            if (campo == "apolice") solicitacao.NumeroApolice = new string('9', ConstantsSeguradora.NumeroApoliceTamanhoMaximo + 1);
            if (campo == "placa") solicitacao.Placa = " ";
            if (campo == "dataFutura") solicitacao.DataOcorrencia = Hoje.AddDays(1);
            if (campo == "dataVazia") solicitacao.DataOcorrencia = default(DateOnly);
            if (campo == "tipo") solicitacao.Tipo = (TipoSinistro)99;
            if (campo == "descricao") solicitacao.Descricao = new string('d', ConstantsSeguradora.DescricaoTamanhoMaximo + 1);
            if (campo == "valor") solicitacao.ValorPrejuizo = 0m;

            await Assert.ThrowsAsync<BusinessException>(() => cenario.Gateway.AbrirSinistroAsync(solicitacao));
        }

        [Theory]
        [InlineData(AmbienteIntegracao.PRODUCAO)]
        [InlineData(AmbienteIntegracao.HOMOLOGACAO)]
        public async Task FakeForaDoSandbox_LancaIntegrationException(AmbienteIntegracao ambiente)
        {
            Cenario cenario = new Cenario(ambiente);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarVigenciaAsync("AP-1", Hoje));

            Assert.Equal(ConstantsMessage.ErroSeguradoraFakeForaDoSandbox, excecao.Message);
            Assert.Equal(ConstantsSeguradora.ChaveProvedor, excecao.Chave);
        }

        [Fact]
        public async Task ProvedorDesconhecido_LancaIntegrationException()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, "OUTRA_SEGURADORA");

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao("SIN-6")));

            Assert.Equal(ConstantsMessage.ErroSeguradoraProvedorNaoSuportado, excecao.Message);
        }

        [Fact]
        public async Task ProvedorAindaDefinirNoBanco_LancaIntegrationExceptionSemExporValor()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, ConstantsValues.MarcadorValorNaoDefinido);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarVigenciaAsync("AP-1", Hoje));

            Assert.Equal(ConstantsSeguradora.ChaveProvedor, excecao.Chave);
            Assert.Equal(ConstantsIntegration.Seguradora, excecao.CodigoIntegracao);
        }

        [Fact]
        public async Task IntegracaoInativa_LancaIntegrationException()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, ConstantsSeguradora.ProvedorFake, false);

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarSinistroAsync("FAKE-SIN-1"));
        }

        private static SinistroAberturaSolicitacao CriarSolicitacao(string referencia)
        {
            SinistroAberturaSolicitacao solicitacao = new SinistroAberturaSolicitacao
            {
                ReferenciaExterna = referencia,
                NumeroApolice = "AP-2026-0001",
                Placa = "ABC1D23",
                DataOcorrencia = Hoje,
                Tipo = TipoSinistro.COLISAO,
                Descricao = "Colisao traseira no estacionamento",
                ValorPrejuizo = 3500m
            };

            return solicitacao;
        }

        /// <summary>Configuracao SEGURADORA em memoria, store e provedor FAKE reais e o gateway.</summary>
        private sealed class Cenario
        {
            public Cenario(AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX, string provedor = ConstantsSeguradora.ProvedorFake, bool ativo = true)
            {
                Integration integration = new Integration
                {
                    IdIntegration = 1,
                    Codigo = ConstantsIntegration.Seguradora,
                    Nome = "Seguradora de teste",
                    Ambiente = ambiente,
                    BaseUrl = "https://sandbox.seguradora.exemplo.com.br",
                    TimeoutMs = 30000,
                    Ativo = ativo
                };
                integration.Settings.Add(new IntegrationSetting { Chave = ConstantsSeguradora.ChaveProvedor, Valor = provedor, Protegido = false });
                integration.Settings.Add(new IntegrationSetting { Chave = "senha", Valor = ConstantsValues.MarcadorValorNaoDefinido, Protegido = true });

                FakeIntegrationRepository repositorio = new FakeIntegrationRepository();
                repositorio.Add(integration);

                FakeLogService log = new FakeLogService();
                IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), log);
                Store = new SeguradoraFakeStore();
                ISeguradoraProvider[] providers = new ISeguradoraProvider[] { new FakeSeguradoraProvider(Store, log) };
                Gateway = new SeguradoraGateway(resolver, providers, log);
            }

            public SeguradoraFakeStore Store { get; }

            public SeguradoraGateway Gateway { get; }
        }
    }
}
