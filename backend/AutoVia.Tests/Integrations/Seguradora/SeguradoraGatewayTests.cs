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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;

namespace AutoVia.Tests.Integrations.Seguradora
{
    /// <summary>
    /// Testes do SeguradoraGateway com o provedor FAKE real e a configuracao em memoria:
    /// vigencia (vigente e marcador VENCIDA), abertura idempotente, acompanhamento com a
    /// linha do tempo, validacoes (409), erros de configuracao (502) e a trava dupla do FAKE
    /// (ambiente de hospedagem Development E integracao SANDBOX). A data de hoje vem do relogio fixo.
    /// </summary>
    public class SeguradoraGatewayTests
    {
        /// <summary>Relogio fixo dos testes: a data de "hoje" nao depende do dia em que a suite roda.</summary>
        private static readonly DateTime AgoraFixa = new DateTime(2026, 11, 16, 10, 0, 0);
        private static readonly DateOnly Hoje = DateOnly.FromDateTime(AgoraFixa);

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

        [Fact]
        public async Task AbrirSinistro_DataDeOcorrenciaSegueORelogio_AceitaHojeERecusaAmanha()
        {
            Cenario cenario = new Cenario();
            DateOnly hojeDoRelogio = DateOnly.FromDateTime(cenario.Relogio.Agora);

            SinistroAberturaSolicitacao naData = CriarSolicitacao("SIN-REL-1");
            naData.DataOcorrencia = hojeDoRelogio;
            SinistroSeguradoraResultado aberto = await cenario.Gateway.AbrirSinistroAsync(naData);
            Assert.Equal(StatusSinistro.ABERTO, aberto.Status);

            SinistroAberturaSolicitacao amanha = CriarSolicitacao("SIN-REL-2");
            amanha.DataOcorrencia = hojeDoRelogio.AddDays(1);
            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Gateway.AbrirSinistroAsync(amanha));
            Assert.Equal(ConstantsMessage.ErroSeguradoraDataOcorrenciaInvalida, excecao.Message);
        }

        [Fact]
        public async Task AbrirSinistro_HojeDoSistemaComRelogioDezDiasAntes_ReprovaDataOcorrencia()
        {
            Cenario cenario = new Cenario();
            cenario.Relogio.Agora = AgoraFixa.AddDays(-10);

            SinistroAberturaSolicitacao solicitacao = CriarSolicitacao("SIN-REL-3");
            solicitacao.DataOcorrencia = Hoje;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Gateway.AbrirSinistroAsync(solicitacao));
            Assert.Equal(ConstantsMessage.ErroSeguradoraDataOcorrenciaInvalida, excecao.Message);
        }

        /// <summary>D-13: FAKE fora de Development recusa nas 3 operacoes, sem criar nada no armazenamento.</summary>
        [Fact]
        public async Task Seguradora_FakeFora_DeDesenvolvimento_Bloqueia()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, ConstantsSeguradora.ProvedorFake, true, Environments.Production);
            string referencia = "SIN-TRAVA";

            IntegrationException consulta = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarVigenciaAsync("AP-1", Hoje));
            IntegrationException abertura = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao(referencia)));
            IntegrationException acompanhamento = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarSinistroAsync(ProtocoloDaReferencia(referencia)));

            foreach (IntegrationException excecao in new List<IntegrationException> { consulta, abertura, acompanhamento })
            {
                Assert.Equal(ConstantsMessage.ErroSeguradoraFakeForaDoDesenvolvimento, excecao.Message);
                Assert.Equal(ConstantsSeguradora.ChaveProvedor, excecao.Chave);
                Assert.Equal(ConstantsIntegration.Seguradora, excecao.CodigoIntegracao);
            }

            Assert.Null(cenario.Store.Obter(ProtocoloDaReferencia(referencia)));
            Assert.Empty(cenario.Log.Entries);
        }

        /// <summary>Prova o helper ProtocoloDaReferencia contra o protocolo que o FAKE realmente gera na abertura.</summary>
        [Fact]
        public async Task Protocolo_DaAbertura_BateComOCalculoDoTeste()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, ConstantsSeguradora.ProvedorFake, true, Environments.Development);
            string referencia = "SIN-PROTOCOLO";

            SinistroSeguradoraResultado resultado = await cenario.Gateway.AbrirSinistroAsync(CriarSolicitacao(referencia));

            Assert.Equal(ProtocoloDaReferencia(referencia), resultado.Protocolo);
        }

        [Fact]
        public async Task FakeForaDoSandboxEForaDeDesenvolvimento_GatewayBarraAntesDoProvedor()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.PRODUCAO, ConstantsSeguradora.ProvedorFake, true, Environments.Production);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Gateway.ConsultarVigenciaAsync("AP-1", Hoje));

            Assert.Equal(ConstantsMessage.ErroSeguradoraFakeForaDoSandbox, excecao.Message);
        }

        [Fact]
        public async Task FakeEmDevelopmentESandbox_Responde()
        {
            Cenario cenario = new Cenario(AmbienteIntegracao.SANDBOX, ConstantsSeguradora.ProvedorFake, true, Environments.Development);

            VigenciaApoliceResultado resultado = await cenario.Gateway.ConsultarVigenciaAsync("AP-1", Hoje);

            Assert.True(resultado.Vigente);
        }

        /// <summary>Mesmo calculo do FAKE: FAKE-SIN- + 12 hex do SHA-256 da referencia em UTF-8 (conferido por teste de abertura).</summary>
        private static string ProtocoloDaReferencia(string referencia)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(referencia));

            return ConstantsSeguradora.PrefixoProtocoloFake + Convert.ToHexString(hash).Substring(0, ConstantsSeguradora.TamanhoHashProtocoloFake);
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

        /// <summary>IHostEnvironment de teste: so o nome do ambiente de hospedagem importa para a trava do FAKE.</summary>
        private sealed class AmbienteFalso : IHostEnvironment
        {
            public AmbienteFalso(string nomeAmbiente)
            {
                EnvironmentName = nomeAmbiente;
                ContentRootPath = Path.GetTempPath();
                ContentRootFileProvider = new NullFileProvider();
            }

            public string EnvironmentName { get; set; }

            public string ApplicationName { get; set; } = "AutoVia.Tests";

            public string ContentRootPath { get; set; }

            public IFileProvider ContentRootFileProvider { get; set; }
        }

        /// <summary>Configuracao SEGURADORA em memoria, relogio fixo, store e provedor FAKE reais e o gateway.</summary>
        private sealed class Cenario
        {
            public Cenario(AmbienteIntegracao ambiente = AmbienteIntegracao.SANDBOX, string provedor = ConstantsSeguradora.ProvedorFake, bool ativo = true, string? ambienteHospedagem = null)
            {
                string nomeAmbienteHospedagem = ambienteHospedagem ?? Environments.Development;

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

                Log = new FakeLogService();
                IntegrationResolver resolver = new IntegrationResolver(repositorio, new MemoryCache(new MemoryCacheOptions()), Log);
                Relogio = new FakeRelogio(AgoraFixa);
                Store = new SeguradoraFakeStore(Relogio);
                ISeguradoraProvider[] providers = new ISeguradoraProvider[] { new FakeSeguradoraProvider(Store, new AmbienteFalso(nomeAmbienteHospedagem), Log) };
                Gateway = new SeguradoraGateway(resolver, providers, Relogio, Log);
            }

            public FakeLogService Log { get; }

            public FakeRelogio Relogio { get; }

            public SeguradoraFakeStore Store { get; }

            public SeguradoraGateway Gateway { get; }
        }
    }
}
