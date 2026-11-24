using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Locacoes;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Options;
using AutoVia.Core.Profiles;
using AutoVia.Core.Regras;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Tests.DocumentosCliente;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Tests.Locacoes
{
    /// <summary>
    /// Leitura da retirada com a caucao (GET /api/locacoes/retirada/{idReserva}; contrato caucao-retirada v1.1,
    /// leitura_da_retirada; correcao f) no servico, com o FakeCaucaoGateway: valor 0 sem consulta e com os campos neutros, inclusive
    /// com a integracao quebrada (igual ao de hoje); caucao exigida com a simulacao liberada, sem simulacao, com falha da consulta
    /// (continua 200, indisponivel, com log so da reserva) e com a locacao ja aberta (sem consulta e sem log); reserva de outra
    /// locadora ou inexistente responde 404 sem chegar ao gateway. A concordancia com a trava do FAKE real fica em
    /// CaucaoRetiradaFluxoTests.
    /// </summary>
    public class CaucaoRetiradaLeituraTests
    {
        private const int IdReserva = 10;
        private const decimal ValorDaCaucao = 1200.00m;
        private static readonly DateTime AgoraDoRelogio = new DateTime(2026, 11, 24, 10, 0, 0);

        private sealed class Cenario
        {
            public Cenario(decimal valorCaucao, int idEmpresaVeiculo = 1)
            {
                Repositorio.Adicionar(CriarReserva(idEmpresaVeiculo));
                CaucaoRetiradaService caucao = new CaucaoRetiradaService(Gateway, Repositorio, new CaucaoRetiradaOptions(valorCaucao), Log);
                Servico = new LocacaoService(Repositorio, new FakeRelogio(AgoraDoRelogio), CriarMapper(), Log, ContextoEmpresaDeTeste.Criar(), new FotoCapaVeiculoService(new FakeArmazenamentoGateway(), Log), new FakeClienteDocumentoRepository(), caucao);
            }

            public FakeLocacaoRepository Repositorio { get; } = new FakeLocacaoRepository();

            public FakeCaucaoGateway Gateway { get; } = new FakeCaucaoGateway();

            public FakeLogService Log { get; } = new FakeLogService();

            public LocacaoService Servico { get; }

            public Task<ReturnRetiradaVO> LerAsync(int idReserva = IdReserva)
            {
                return Servico.GetRetiradaAsync(idReserva);
            }
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<LocacaoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        private static Reserva CriarReserva(int idEmpresaVeiculo)
        {
            Cliente cliente = new Cliente
            {
                IdCliente = 30, Nome = "Maria da Silva", Cpf = "12345678909", Email = "maria.cliente@exemplo.com", CnhNumero = "12345678901",
                CnhCategoria = CategoriaCnh.B, CnhValidade = new DateOnly(2030, 1, 1)
            };
            Veiculo veiculo = new Veiculo
            {
                IdVeiculo = 40, IdEmpresa = idEmpresaVeiculo, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Toro", AnoModelo = 2024,
                KmAtual = 1500, ValorDiaria = 200m, Status = StatusVeiculo.RESERVADO, IdCategoria = 3,
                Categoria = new Categoria { IdCategoria = 3, Nome = "SUV" },
                Empresa = new Empresa { IdEmpresa = idEmpresaVeiculo, Ativo = true, NomeFantasia = "Locadora Teste", Logradouro = "Rua das Flores", Numero = "100", Bairro = "Centro", Cidade = "Campinas", Uf = "SP" }
            };

            return new Reserva
            {
                IdReserva = IdReserva, IdCliente = 30, Cliente = cliente, IdVeiculo = 40, Veiculo = veiculo,
                DataRetiradaPrevista = new DateTime(2026, 11, 24, 9, 30, 0), DataDevolucaoPrevista = new DateTime(2026, 11, 27, 9, 30, 0),
                ValorEstimado = 600m, Status = StatusReserva.CONFIRMADA
            };
        }

        private static string RegistroDeConfiguracaoIndisponivel()
        {
            return string.Format(CultureInfo.InvariantCulture, ConstantsMessage.LogCaucaoRetiradaConfiguracaoIndisponivel, IdReserva);
        }

        // --- Valor 0: a leitura de hoje ---

        [Fact]
        public async Task ValorZero_CamposNeutros_SemConsultarOGatewayNemRegistrar()
        {
            Cenario cenario = new Cenario(0m);

            ReturnRetiradaVO retorno = await cenario.LerAsync();

            Assert.False(retorno.CaucaoExigida);
            Assert.Equal(0m, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(0m), retorno.ValorCaucaoStr);
            Assert.False(retorno.CaucaoDisponivel);
            Assert.False(retorno.CaucaoModoSimulacao);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Empty(cenario.Log.Entries);
        }

        /// <summary>Correcao f: integracao quebrada com valor 0 = a mesma leitura de uma integracao boa, sem consulta e sem log.</summary>
        [Fact]
        public async Task ValorZero_IntegracaoQuebrada_LeituraIgualAComAIntegracaoBoa_SemConsultaNemLog()
        {
            Cenario integracaoBoa = new Cenario(0m);
            Cenario integracaoQuebrada = new Cenario(0m);
            integracaoQuebrada.Gateway.ExcecaoConfiguracao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorNaoSuportado);

            ReturnRetiradaVO esperado = await integracaoBoa.LerAsync();
            ReturnRetiradaVO retorno = await integracaoQuebrada.LerAsync();

            Assert.Equal(JsonSerializer.Serialize(esperado), JsonSerializer.Serialize(retorno));
            Assert.Equal(0, integracaoQuebrada.Gateway.TotalDeChamadas);
            Assert.Empty(integracaoQuebrada.Log.Entries);
        }

        // --- Caucao exigida ---

        [Fact]
        public async Task ComCaucaoESimulacaoLiberada_ExigidaDisponivelEModoSimulacao_ComOValorFormatado()
        {
            Cenario cenario = new Cenario(ValorDaCaucao);

            ReturnRetiradaVO retorno = await cenario.LerAsync();

            Assert.True(retorno.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(1200.00m), retorno.ValorCaucaoStr);
            Assert.True(retorno.CaucaoDisponivel);
            Assert.True(retorno.CaucaoModoSimulacao);
            Assert.Equal(new List<string> { nameof(FakeCaucaoGateway.ObterConfiguracaoCartaoAsync) }, cenario.Gateway.Chamadas);
            Assert.Empty(cenario.Log.Entries);
        }

        /// <summary>A consulta vai com CancellationToken.None (nunca o do pedido): nao ha cancelamento possivel.</summary>
        [Fact]
        public async Task ComCaucao_ConsultaSemCancelamentoPossivel()
        {
            Cenario cenario = new Cenario(ValorDaCaucao);

            await cenario.LerAsync();

            CancellationToken recebido = Assert.Single(cenario.Gateway.TokensRecebidos);
            Assert.False(recebido.CanBeCanceled);
        }

        /// <summary>FAKE com a trava desligada ou Mercado Pago (sem formulario de cartao real na tela): exigida, mas indisponivel; nao e falha, entao nao registra.</summary>
        [Theory]
        [InlineData(ConstantsCaucao.ProvedorFake)]
        [InlineData(ConstantsCaucao.ProvedorMercadoPago)]
        public async Task ComCaucaoSemSimulacaoLiberada_ExigidaMasIndisponivel_SemLog(string provedor)
        {
            Cenario cenario = new Cenario(ValorDaCaucao);
            cenario.Gateway.Configuracao = new CaucaoConfiguracaoCartao { Provedor = provedor, ChavePublica = "chave-publica-de-teste", ModoSimulacao = false };

            ReturnRetiradaVO retorno = await cenario.LerAsync();

            Assert.True(retorno.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(ValorDaCaucao), retorno.ValorCaucaoStr);
            Assert.False(retorno.CaucaoDisponivel);
            Assert.False(retorno.CaucaoModoSimulacao);
            Assert.Equal(1, cenario.Gateway.ChamadasConfiguracao);
            Assert.Empty(cenario.Log.Entries);
        }

        public static TheoryData<string> FalhasDaConsulta => new TheoryData<string> { "integracao", "nao-encontrada", "negocio", "inesperada" };

        /// <summary>Qualquer falha da consulta vira caucao exigida e indisponivel; a leitura continua (200) e registra so a reserva.</summary>
        [Theory]
        [MemberData(nameof(FalhasDaConsulta))]
        public async Task ComCaucaoConsultaLanca_LeituraContinuaComCaucaoIndisponivel_ELogSoComAReserva(string falha)
        {
            Cenario cenario = new Cenario(ValorDaCaucao);
            cenario.Gateway.ExcecaoConfiguracao = falha == "integracao" ? new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorNaoSuportado)
                : falha == "nao-encontrada" ? new NotFoundException(ConstantsMessage.ErroCaucaoNaoEncontrada)
                : falha == "negocio" ? new BusinessException(ConstantsMessage.ErroCaucaoSolicitacaoRecusada)
                : new InvalidOperationException();

            ReturnRetiradaVO retorno = await cenario.LerAsync();

            Assert.Equal(IdReserva, retorno.IdReserva);
            Assert.True(retorno.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(ValorDaCaucao), retorno.ValorCaucaoStr);
            Assert.False(retorno.CaucaoDisponivel);
            Assert.False(retorno.CaucaoModoSimulacao);
            Assert.Equal(1, cenario.Gateway.ChamadasConfiguracao);
            string entrada = Assert.Single(cenario.Log.Entries);
            Assert.StartsWith(RegistroDeConfiguracaoIndisponivel(), entrada, StringComparison.Ordinal);
        }

        /// <summary>CE-5: com a locacao ja aberta, a caucao fala so da configuracao; o gateway (aqui quebrado) nao e consultado e nada e registrado.</summary>
        [Fact]
        public async Task ComCaucaoELocacaoJaAberta_SemConsultaESemLog_ValorDaConfiguracaoEIndicadoresFalsos()
        {
            Cenario cenario = new Cenario(ValorDaCaucao);
            cenario.Repositorio.Adicionar(new Locacao { IdLocacao = 7, IdReserva = IdReserva });
            cenario.Gateway.ExcecaoConfiguracao = new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroCaucaoProvedorNaoSuportado);

            ReturnRetiradaVO retorno = await cenario.LerAsync();

            Assert.Equal(7, retorno.IdLocacaoExistente);
            Assert.True(retorno.CaucaoExigida);
            Assert.Equal(ValorDaCaucao, retorno.ValorCaucao);
            Assert.Equal(StaticMethods.FormateToMonetary(ValorDaCaucao), retorno.ValorCaucaoStr);
            Assert.False(retorno.CaucaoDisponivel);
            Assert.False(retorno.CaucaoModoSimulacao);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
            Assert.Empty(cenario.Log.Entries);
        }

        /// <summary>As conferencias do 404 vem antes da caucao: reserva de outra locadora (ou inexistente) nunca chega ao gateway.</summary>
        [Theory]
        [InlineData("outra-locadora")]
        [InlineData("inexistente")]
        public async Task ComCaucaoReservaForaDoEscopo_404SemConsultarOGateway(string caso)
        {
            Cenario cenario = new Cenario(ValorDaCaucao, caso == "outra-locadora" ? 2 : 1);

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => cenario.LerAsync(caso == "inexistente" ? 99 : IdReserva));

            Assert.Equal(ConstantsMessage.ErroReservaNaoEncontrada, excecao.Message);
            Assert.Equal(0, cenario.Gateway.TotalDeChamadas);
        }

        /// <summary>Nunca devolve chave publica, nome do provedor, id_transacao nem dado de cartao: so os cinco campos da caucao.</summary>
        [Fact]
        public async Task ComCaucao_LeituraSemChavePublicaNomeDoProvedorNemIdDoProvedor()
        {
            const string ProvedorDeTeste = "PROVEDOR-DE-TESTE-X";
            const string ChavePublicaDeTeste = "CHAVE-PUBLICA-DE-TESTE-X";
            Cenario cenario = new Cenario(ValorDaCaucao);
            cenario.Gateway.Configuracao = new CaucaoConfiguracaoCartao { Provedor = ProvedorDeTeste, ChavePublica = ChavePublicaDeTeste, ModoSimulacao = true };

            ReturnRetiradaVO retorno = await cenario.LerAsync();
            string json = JsonSerializer.Serialize(retorno);

            Assert.True(retorno.CaucaoModoSimulacao);
            Assert.DoesNotContain(ProvedorDeTeste, json);
            Assert.DoesNotContain(ChavePublicaDeTeste, json);
            Assert.DoesNotContain("ChavePublica", json);
            Assert.DoesNotContain("Provedor", json);
            Assert.DoesNotContain("IdTransacao", json);
            Assert.DoesNotContain(FakeCaucaoGateway.CartaoFinalDeTeste, json);
        }

        // --- Regras puras da leitura ---

        [Theory]
        [InlineData(false, null, false)]
        [InlineData(false, 7, false)]
        [InlineData(true, 7, false)]
        [InlineData(true, null, true)]
        public void ConsultaConfiguracaoNaRetirada_SoComCaucaoExigidaESemLocacaoAberta(bool caucaoExigida, int? idLocacaoExistente, bool esperado)
        {
            Assert.Equal(esperado, RegrasCaucaoRetirada.ConsultaConfiguracaoNaRetirada(caucaoExigida, idLocacaoExistente));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, true)]
        public void CaucaoDisponivelNaRetirada_SoComCaucaoExigidaESimulacaoLiberada(bool caucaoExigida, bool modoSimulacao, bool esperado)
        {
            Assert.Equal(esperado, RegrasCaucaoRetirada.CaucaoDisponivelNaRetirada(caucaoExigida, modoSimulacao));
        }
    }
}
