using AutoMapper;
using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Communication.ViewObjects.Pagamento;
using AutoVia.Core.Entities.PgSql;
using AutoVia.Core.Profiles;
using AutoVia.Helpers;
using AutoVia.Infrastructure.Services;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoVia.Tests.Cobranca
{
    /// <summary>
    /// Servico de PIX da cobranca (CobrancaPixService) com fakes: repositorio em memoria, gateway de teste e relogio fixo. Prova a
    /// posse (404 sem diferenca), a recusa por tipo e status (409 sem chamar o provedor), a reabertura sem provedor, a repeticao
    /// de EXPIRADA, a conferencia de status, de retorno, de valor e de referencia, a corrida sob trava, a falha da gravacao e o
    /// log sem dado pessoal. O SQL e provado em PagamentoConsultasSqlTests; a gravacao real no PostgreSQL fica para o roteiro.
    /// </summary>
    public class CobrancaPixServiceTests
    {
        private const int IdClienteDoTeste = 7;
        private const int IdClienteAlheio = 99;
        private const int IdCobranca = 12;
        private const string ReferenciaInicial = "PAG-12";
        private const string NomePagador = "Maria Souza";
        private const string EmailPagador = "maria@exemplo.com";
        private const string CpfPagador = "12345678901";
        private static readonly DateTime Agora = new DateTime(2026, 11, 20, 10, 0, 0);

        private sealed class Cenario
        {
            public Cenario()
            {
                Gateway.Agora = Agora;
                Relogio = new FakeRelogio(Agora);
                Servico = new CobrancaPixService(Repositorio, Gateway, CriarMapper(), Relogio, Log);
            }

            public FakePagamentoRepository Repositorio { get; } = new FakePagamentoRepository();

            public FakePixGateway Gateway { get; } = new FakePixGateway();

            public FakeLogService Log { get; } = new FakeLogService();

            public FakeRelogio Relogio { get; }

            public CobrancaPixService Servico { get; }
        }

        private static IMapper CriarMapper()
        {
            MapperConfiguration configuracao = new MapperConfiguration(cfg => cfg.AddProfile<PagamentoProfile>(), NullLoggerFactory.Instance);

            return configuracao.CreateMapper();
        }

        /// <summary>Semeia a cobranca de uma locacao do cliente (com o cliente da origem, para o pagador).</summary>
        private static Pagamento SemearCobranca(FakePagamentoRepository repositorio, TipoPagamento tipo = TipoPagamento.DIARIA, StatusPagamento status = StatusPagamento.PENDENTE, decimal valor = 395.60m)
        {
            Cliente cliente = new Cliente { IdCliente = IdClienteDoTeste, Nome = NomePagador, Cpf = CpfPagador, Email = EmailPagador };
            Locacao locacao = new Locacao { IdLocacao = 1, IdCliente = IdClienteDoTeste, Cliente = cliente, Veiculo = new Veiculo { Marca = "Fiat", Modelo = "Toro" } };
            repositorio.Locacoes.Add(locacao);

            Pagamento cobranca = new Pagamento
            {
                IdPagamento = IdCobranca,
                IdLocacao = locacao.IdLocacao,
                Tipo = tipo,
                Valor = valor,
                DataVencimento = new DateOnly(2026, 11, 22),
                Status = status,
                Locacao = locacao
            };
            repositorio.Pagamentos.Add(cobranca);

            return cobranca;
        }

        private static PixCobrancaResultado RetornoEm(PixCobrancaSolicitacao solicitacao, PixStatusCobranca status, DateTime? expiraEm)
        {
            return new PixCobrancaResultado
            {
                IdentificadorProvedor = "ID-" + solicitacao.ReferenciaExterna,
                ReferenciaExterna = solicitacao.ReferenciaExterna,
                Valor = solicitacao.Valor,
                Status = status,
                CopiaECola = "COPIA-" + solicitacao.ReferenciaExterna,
                QrCodeBase64 = "QRBASE64-" + solicitacao.ReferenciaExterna,
                ExpiraEm = expiraEm,
                Provedor = "FAKE"
            };
        }

        [Fact]
        public async Task GerarPixNovo_ChamaOGatewayUmaVez_ComValorDoBancoEPagadorDoClienteEGrava()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Single(cenario.Gateway.Solicitacoes);
            PixCobrancaSolicitacao solicitacao = cenario.Gateway.Solicitacoes[0];
            Assert.Equal(ReferenciaInicial, solicitacao.ReferenciaExterna);
            Assert.Equal(395.60m, solicitacao.Valor);
            Assert.Equal(NomePagador, solicitacao.Pagador.Nome);
            Assert.Equal(EmailPagador, solicitacao.Pagador.Email);
            Assert.Equal(CpfPagador, solicitacao.Pagador.Documento);

            Pagamento gravado = cenario.Repositorio.Pagamentos[0];
            Assert.Equal("ID-PAG-12", gravado.IdTransacao);
            Assert.Equal(ConstantsValues.FormaPix, gravado.Forma);
            Assert.Equal("COPIA-PAG-12", gravado.PixCopiaECola);
            Assert.Equal("QRBASE64-PAG-12", gravado.QrCodeBase64);
            Assert.Equal(Agora.AddSeconds(900), gravado.PixExpiraEm);

            Assert.Equal(IdCobranca, saida.IdPagamento);
            Assert.Equal("COPIA-PAG-12", saida.CopiaECola);
            Assert.Equal(ConstantsValues.FormaPix, saida.Forma);
            Assert.Equal(900, saida.SegundosParaExpirar);
            Assert.Equal(395.60m, saida.Valor);
        }

        [Fact]
        public async Task ReabrirPixValido_NaoChamaOProvedor_DevolveOGravado()
        {
            Cenario cenario = new Cenario();
            Pagamento cobranca = SemearCobranca(cenario.Repositorio);
            cobranca.IdTransacao = "ID-EXISTENTE";
            cobranca.Forma = ConstantsValues.FormaPix;
            cobranca.PixCopiaECola = "COPIA-EXISTENTE";
            cobranca.QrCodeBase64 = "QR-EXISTENTE";
            cobranca.PixExpiraEm = Agora.AddSeconds(600);

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Empty(cenario.Gateway.Solicitacoes);
            Assert.Equal("COPIA-EXISTENTE", saida.CopiaECola);
            Assert.Equal(600, saida.SegundosParaExpirar);
            Assert.Contains(string.Format(ConstantsMessage.InfoPixReaberto, IdCobranca), cenario.Log.Entries);
        }

        [Fact]
        public async Task PixExpirado_GeraDeNovoComAReferenciaDeRecuperacao()
        {
            Cenario cenario = new Cenario();
            Pagamento cobranca = SemearCobranca(cenario.Repositorio);
            cobranca.IdTransacao = "ID-ANTIGO";
            cobranca.PixCopiaECola = "COPIA-ANTIGA";
            cobranca.QrCodeBase64 = "QR-ANTIGO";
            cobranca.PixExpiraEm = Agora.AddMinutes(-1);

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Equal("PAG-12-20261120095900", cenario.Gateway.Solicitacoes[0].ReferenciaExterna);
            Assert.Equal("ID-PAG-12-20261120095900", cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal("COPIA-PAG-12-20261120095900", saida.CopiaECola);
        }

        [Fact]
        public async Task DuasChamadasSeguidasSemExpirar_SegundaNaoChamaOProvedor()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);
            await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Equal(1, cenario.Repositorio.GravacoesDePix);
        }

        [Theory]
        [InlineData(TipoPagamento.CAUCAO, StatusPagamento.PENDENTE, ConstantsMessage.ErroPixTipoNaoPagavel)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.PAGO, ConstantsMessage.ErroPixPagamentoJaPago)]
        [InlineData(TipoPagamento.DIARIA, StatusPagamento.CANCELADO, ConstantsMessage.ErroPixPagamentoCancelado)]
        public async Task CaucaoPagoOuCancelado_Recusa409SemChamarOProvedor(TipoPagamento tipo, StatusPagamento status, string erroEsperado)
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio, tipo, status);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(erroEsperado, excecao.Message);
            Assert.Empty(cenario.Gateway.Solicitacoes);
        }

        [Fact]
        public async Task CobrancaDeOutroCliente_Responde404SemChamarOProvedor()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteAlheio, IdCobranca));

            Assert.Empty(cenario.Gateway.Solicitacoes);
        }

        [Fact]
        public async Task IdZeroOuNegativo_Responde404SemConsultar()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, 0));
            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, -3));

            Assert.Empty(cenario.Gateway.Solicitacoes);
        }

        [Fact]
        public async Task ProvedorDevolvePaga_Responde409NadaGravadoELogaPago()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => RetornoEm(solicitacao, PixStatusCobranca.PAGA, Agora.AddSeconds(900));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixGerarIndisponivel, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Contains(string.Format(ConstantsMessage.LogPixPagoNoProvedor, IdCobranca), cenario.Log.Entries);
        }

        [Fact]
        public async Task ProvedorExpiraAReferenciaOriginal_RepeteComAValidadeDevolvidaEGrava()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => solicitacao.ReferenciaExterna == ReferenciaInicial
                ? RetornoEm(solicitacao, PixStatusCobranca.EXPIRADA, Agora.AddMinutes(-1))
                : RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal(2, cenario.Gateway.Solicitacoes.Count);
            Assert.Equal("PAG-12-20261120095900", cenario.Gateway.Solicitacoes[1].ReferenciaExterna);
            Assert.Equal("COPIA-PAG-12-20261120095900", saida.CopiaECola);
            Assert.Equal(1, cenario.Repositorio.GravacoesDePix);
        }

        [Fact]
        public async Task ProvedorExpiraSemValidade_Responde409SemRepetir()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => RetornoEm(solicitacao, PixStatusCobranca.EXPIRADA, null);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixGerarIndisponivel, excecao.Message);
            Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task ProvedorDevolveValorDiferente_502NadaGravadoELogaValor()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.Valor = solicitacao.Valor + 1m;
                return retorno;
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Contains(string.Format(ConstantsMessage.LogPixRespostaDivergente, IdCobranca, ConstantsValues.MotivoDivergenciaValor), cenario.Log.Entries);
        }

        [Fact]
        public async Task ProvedorDevolveReferenciaDiferente_502NadaGravadoELogaReferencia()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.ReferenciaExterna = "OUTRA-REFERENCIA";
                return retorno;
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Contains(string.Format(ConstantsMessage.LogPixRespostaDivergente, IdCobranca, ConstantsValues.MotivoDivergenciaReferencia), cenario.Log.Entries);
        }

        [Fact]
        public async Task RetornoSemCopiaECola_502NadaGravado()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.CopiaECola = null;
                return retorno;
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task ReabrirSobTrava_CobrancaPagaNaTrava_Responde409SemDevolverOGravado()
        {
            Cenario cenario = new Cenario();
            Pagamento cobranca = SemearCobranca(cenario.Repositorio);
            cobranca.IdTransacao = "ID-EXISTENTE";
            cobranca.PixExpiraEm = Agora.AddSeconds(600);
            cenario.Repositorio.AntesDaTrava = guardada => guardada.Status = StatusPagamento.PAGO;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixPagamentoJaPago, excecao.Message);
            Assert.Empty(cenario.Gateway.Solicitacoes);
        }

        [Fact]
        public async Task CorridaNaGravacao_PixValidoJaGravado_CancelaOCriadoEDevolveOGravadoSemSobrescrever()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.AntesDaGravacao = guardada =>
            {
                guardada.IdTransacao = "ID-OUTRO";
                guardada.PixCopiaECola = "COPIA-OUTRO";
                guardada.QrCodeBase64 = "QR-OUTRO";
                guardada.PixExpiraEm = Agora.AddSeconds(900);
            };

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal("COPIA-OUTRO", saida.CopiaECola);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
            Assert.Equal("ID-OUTRO", cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(new List<string> { "ID-PAG-12" }, cenario.Gateway.Cancelamentos);
            Assert.Contains(string.Format(ConstantsMessage.InfoPixDescartadoPorConcorrencia, IdCobranca), cenario.Log.Entries);
        }

        [Fact]
        public async Task CorridaNaGravacao_MesmoPixJaGravado_NaoCancelaNoProvedor()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.AntesDaGravacao = guardada =>
            {
                guardada.IdTransacao = "ID-PAG-12";
                guardada.PixCopiaECola = "COPIA-PAG-12";
                guardada.QrCodeBase64 = "QRBASE64-PAG-12";
                guardada.PixExpiraEm = Agora.AddSeconds(900);
            };

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal("COPIA-PAG-12", saida.CopiaECola);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
            Assert.Empty(cenario.Gateway.Cancelamentos);
        }

        [Fact]
        public async Task CobrancaVirouPagaNaGravacao_Responde409NadaGravado()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.AntesDaGravacao = guardada => guardada.Status = StatusPagamento.PAGO;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixPagamentoJaPago, excecao.Message);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
        }

        [Fact]
        public async Task FalhaNaGravacao_RelancaSemGravar_E_TentativaSeguinteRecuperaAMesmaReferencia()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.FalharGravacao = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Contains(ConstantsMessage.ErroGerarPixPagamento, string.Join("\n", cenario.Log.Entries));

            cenario.Repositorio.FalharGravacao = false;
            await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal(2, cenario.Gateway.Solicitacoes.Count);
            Assert.Equal(ReferenciaInicial, cenario.Gateway.Solicitacoes[1].ReferenciaExterna);
            Assert.Equal("ID-PAG-12", cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task Log_NaoTraz_NomeEmailCpfCopiaNemQr()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            string registros = string.Join("\n", cenario.Log.Entries);
            Assert.DoesNotContain(NomePagador, registros);
            Assert.DoesNotContain(EmailPagador, registros);
            Assert.DoesNotContain(CpfPagador, registros);
            Assert.DoesNotContain("COPIA-", registros);
            Assert.DoesNotContain("QRBASE64", registros);
        }

        [Fact]
        public async Task Financeiro_DevolveOIdentificadorDoProvedor_ComoClienteNaoDevolve()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);

            ReturnCobrancaPixFuncionarioVO saida = await cenario.Servico.GerarOuReabrirDaLocadoraAsync(IdCobranca);

            Assert.Equal("ID-PAG-12", saida.IdentificadorProvedor);
            Assert.Equal("COPIA-PAG-12", saida.CopiaECola);
            Assert.Null(typeof(ReturnCobrancaPixVO).GetProperty("IdentificadorProvedor"));
        }

        [Fact]
        public async Task Financeiro_CobrancaDeOutraLocadora_Responde404()
        {
            Cenario cenario = new Cenario();

            await Assert.ThrowsAsync<NotFoundException>(() => cenario.Servico.GerarOuReabrirDaLocadoraAsync(IdCobranca));

            Assert.Empty(cenario.Gateway.Solicitacoes);
        }

        [Fact]
        public async Task RespostaDoGerar_SegundosParaExpirarVemDoRelogioDepoisDoProvedor()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                cenario.Relogio.Agora = cenario.Relogio.Agora.AddSeconds(30);

                return RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
            };

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal(870, saida.SegundosParaExpirar);
        }

        [Fact]
        public async Task GravacaoRecusada409_CancelaNoProvedorOPixCriadoEDevolve409()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.AntesDaGravacao = guardada => guardada.Status = StatusPagamento.PAGO;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixPagamentoJaPago, excecao.Message);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
            Assert.Equal(new string[] { "ID-PAG-12" }, cenario.Gateway.Cancelamentos);
        }

        [Fact]
        public async Task GravacaoRecusada409_FalhaDoCancelamentoNaoMudaORetornoELogaSoOId()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.AntesDaGravacao = guardada => guardada.Status = StatusPagamento.CANCELADO;
            cenario.Gateway.FalharCancelamento = true;

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixPagamentoCancelado, excecao.Message);
            string registros = string.Join("\n", cenario.Log.Entries);
            Assert.Contains(string.Format(ConstantsMessage.ErroCancelarPixDescartado, IdCobranca), registros);
            Assert.DoesNotContain("ID-PAG-12", registros);
        }

        [Fact]
        public async Task RetornoSemCopiaECola_CancelaNoProvedorOPixCriadoEDevolve502()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.CopiaECola = null;
                return retorno;
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
            Assert.Equal(new string[] { "ID-PAG-12" }, cenario.Gateway.Cancelamentos);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task ProvedorDevolveValorDiferente_CancelaNoProvedorOPixCriado()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.Valor = solicitacao.Valor + 1m;
                return retorno;
            };

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(new string[] { "ID-PAG-12" }, cenario.Gateway.Cancelamentos);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task FalhaNaGravacao_CancelaNoProvedorOPixCriadoERelanca()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Repositorio.FalharGravacao = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(new string[] { "ID-PAG-12" }, cenario.Gateway.Cancelamentos);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task ProvedorDevolveOMesmoPixJaGravado_NaoCancelaOPixQueACobrancaUsa()
        {
            Cenario cenario = new Cenario();
            Pagamento cobranca = SemearCobranca(cenario.Repositorio);
            cobranca.IdTransacao = "ID-PAG-12";
            cobranca.PixExpiraEm = Agora.AddSeconds(-1);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                retorno.IdentificadorProvedor = "ID-PAG-12";
                retorno.CopiaECola = null;
                return retorno;
            };

            await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Empty(cenario.Gateway.Cancelamentos);
            Assert.Equal("ID-PAG-12", cenario.Repositorio.Pagamentos[0].IdTransacao);
        }

        [Fact]
        public async Task ErroDeIntegracaoDoGateway_PassaSemLogDuplicadoENadaEGrava()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => throw new IntegrationException(ConstantsIntegration.Pagamento, ConstantsMessage.ErroPixProvedorIndisponivel);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixProvedorIndisponivel, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
            Assert.DoesNotContain(ConstantsMessage.ErroGerarPixPagamento, string.Join("\n", cenario.Log.Entries));
        }

        [Fact]
        public async Task BecoSemSaida_GravacaoFalhaDepoisDeExpiradaNaReferenciaOriginal_TentativaSeguinteRecuperaUmPixNovo()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => solicitacao.ReferenciaExterna.StartsWith(ReferenciaInicial + "-", StringComparison.Ordinal)
                ? RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900))
                : RetornoEm(solicitacao, PixStatusCobranca.EXPIRADA, Agora.AddMinutes(-1));
            cenario.Repositorio.FalharGravacao = true;

            await Assert.ThrowsAsync<InvalidOperationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);

            cenario.Repositorio.FalharGravacao = false;
            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Equal(4, cenario.Gateway.Solicitacoes.Count);
            Assert.Equal(ReferenciaInicial, cenario.Gateway.Solicitacoes[2].ReferenciaExterna);
            Assert.Equal("PAG-12-20261120095900", cenario.Gateway.Solicitacoes[3].ReferenciaExterna);
            Assert.Equal("ID-PAG-12-20261120095900", cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal("COPIA-PAG-12-20261120095900", saida.CopiaECola);
        }

        [Fact]
        public async Task RepeticaoComStatusQueNaoAguarda_Responde409ELogaOStatus()
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao => solicitacao.ReferenciaExterna == ReferenciaInicial
                ? RetornoEm(solicitacao, PixStatusCobranca.EXPIRADA, Agora.AddMinutes(-1))
                : RetornoEm(solicitacao, PixStatusCobranca.CANCELADA, Agora.AddMinutes(-1));

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixGerarIndisponivel, excecao.Message);
            Assert.Equal(2, cenario.Gateway.Solicitacoes.Count);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Contains(string.Format(ConstantsMessage.LogPixStatusNaoAguardandoNoProvedor, IdCobranca, PixStatusCobranca.CANCELADA.ToString()), cenario.Log.Entries);
        }

        [Fact]
        public Task RetornoSemQrCode_502NadaGravado()
        {
            return AfirmarRetornoInvalidoNaoGrava(retorno => retorno.QrCodeBase64 = null);
        }

        [Fact]
        public Task RetornoSemValidade_502NadaGravado()
        {
            return AfirmarRetornoInvalidoNaoGrava(retorno => retorno.ExpiraEm = null);
        }

        [Fact]
        public Task RetornoComValidadeJaPassada_502NadaGravado()
        {
            return AfirmarRetornoInvalidoNaoGrava(retorno => retorno.ExpiraEm = Agora.AddSeconds(-1));
        }

        [Fact]
        public Task RetornoComCopiaEColaAcimaDoLimite_502NadaGravado()
        {
            return AfirmarRetornoInvalidoNaoGrava(retorno => retorno.CopiaECola = new string('A', ConstantsValues.TamanhoMaximoPixCopiaECola + 1));
        }

        [Fact]
        public async Task PixQueVenceEntreALeituraEATrava_SegueParaGerarEGrava()
        {
            Cenario cenario = new Cenario();
            Pagamento cobranca = SemearCobranca(cenario.Repositorio);
            cobranca.IdTransacao = "ID-EXISTENTE";
            cobranca.Forma = ConstantsValues.FormaPix;
            cobranca.PixExpiraEm = Agora.AddSeconds(600);
            cenario.Repositorio.AntesDaTrava = guardada => guardada.PixExpiraEm = Agora.AddSeconds(-1);

            ReturnCobrancaPixVO saida = await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            Assert.Single(cenario.Gateway.Solicitacoes);
            Assert.Equal("PAG-12-20261120095959", cenario.Gateway.Solicitacoes[0].ReferenciaExterna);
            Assert.Equal("ID-PAG-12-20261120095959", cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(1, cenario.Repositorio.GravacoesDePix);
            Assert.Equal("COPIA-PAG-12-20261120095959", saida.CopiaECola);
        }

        [Fact]
        public async Task CobrancaDeVenda_UsaOClienteDaVendaComoPagador()
        {
            const int idVenda = 3;
            const string nomeDaVenda = "Cliente da Venda";
            const string cpfDaVenda = "98765432100";
            const string emailDaVenda = "venda@exemplo.com";
            Cenario cenario = new Cenario();
            Cliente clienteDaVenda = new Cliente { IdCliente = IdClienteDoTeste, Nome = nomeDaVenda, Cpf = cpfDaVenda, Email = emailDaVenda };
            Venda venda = new Venda { IdVenda = idVenda, IdCliente = IdClienteDoTeste, Cliente = clienteDaVenda };
            cenario.Repositorio.Vendas.Add(venda);
            cenario.Repositorio.Pagamentos.Add(new Pagamento
            {
                IdPagamento = IdCobranca,
                IdVenda = idVenda,
                Tipo = TipoPagamento.VENDA,
                Valor = 30000m,
                DataVencimento = new DateOnly(2026, 11, 22),
                Status = StatusPagamento.PENDENTE,
                Venda = venda
            });

            await cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca);

            PixPagador pagador = cenario.Gateway.Solicitacoes[0].Pagador;
            Assert.Equal(nomeDaVenda, pagador.Nome);
            Assert.Equal(emailDaVenda, pagador.Email);
            Assert.Equal(cpfDaVenda, pagador.Documento);
        }

        private async Task AfirmarRetornoInvalidoNaoGrava(Action<PixCobrancaResultado> estragar)
        {
            Cenario cenario = new Cenario();
            SemearCobranca(cenario.Repositorio);
            cenario.Gateway.Responder = solicitacao =>
            {
                PixCobrancaResultado retorno = RetornoEm(solicitacao, PixStatusCobranca.AGUARDANDO_PAGAMENTO, Agora.AddSeconds(900));
                estragar(retorno);

                return retorno;
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => cenario.Servico.GerarOuReabrirDoClienteAsync(IdClienteDoTeste, IdCobranca));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
            Assert.Null(cenario.Repositorio.Pagamentos[0].IdTransacao);
            Assert.Equal(0, cenario.Repositorio.GravacoesDePix);
        }
    }
}
