using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Caucao;
using AutoVia.Tests.Fakes;
using System.Globalization;
using System.Text.Json;

namespace AutoVia.Tests.Integrations.Caucao
{
    public class MercadoPagoCaucaoProviderTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string TokenCartao = "tok_cartao_9876543210";
        private const string EmailPagador = "maria@exemplo.com";
        private const string CpfPagador = "12345678901";
        private const string Referencia = "CAUCAO-LOC-123";
        private const string Chave = "caucao-0123456789abcdef0123456789abcdef01234567";
        private const string CriadoEmTexto = "2026-11-10T10:00:00.000-03:00";
        private const string CorpoReservada = "{\"id\":123456789,\"status\":\"authorized\",\"status_detail\":\"pending_capture\",\"transaction_amount\":500,\"external_reference\":\"CAUCAO-LOC-123\",\"date_created\":\"2026-11-10T10:00:00.000-03:00\",\"captured\":false,\"card\":{\"last_four_digits\":\"4242\"}}";

        // ------------------------------------------------------------------
        // Escopo de teste: IComunicacaoService roteirizado (sem rede).
        // ------------------------------------------------------------------

        private sealed record RespostaRoteirizada(int StatusCode, string? Corpo, bool FalhaRede, bool Timeout);

        private sealed class ComunicacaoRoteirizada : IComunicacaoService
        {
            private readonly Queue<RespostaRoteirizada> _roteiro = new Queue<RespostaRoteirizada>();

            public List<RequestInfo> Requisicoes { get; } = new List<RequestInfo>();

            public Exception? ExcecaoLancada { get; set; }

            public void Responder(int statusCode, string? corpo = null)
            {
                _roteiro.Enqueue(new RespostaRoteirizada(statusCode, corpo, false, false));
            }

            public void FalharRede()
            {
                _roteiro.Enqueue(new RespostaRoteirizada(0, null, true, false));
            }

            public void Expirar()
            {
                _roteiro.Enqueue(new RespostaRoteirizada(0, null, true, true));
            }

            public Task<RequestResponse<T>> SendAsync<T>(RequestInfo request, CancellationToken cancellationToken = default)
            {
                Requisicoes.Add(request);

                if (ExcecaoLancada != null)
                {
                    throw ExcecaoLancada;
                }

                RespostaRoteirizada roteiro = _roteiro.Dequeue();

                RequestResponse<T> resposta = new RequestResponse<T>
                {
                    StatusCode = roteiro.StatusCode,
                    RawContent = roteiro.Corpo,
                    IsNetworkFailure = roteiro.FalhaRede || roteiro.Timeout,
                    IsTimeout = roteiro.Timeout,
                    Attempts = 1
                };

                bool sucessoHttp = roteiro.StatusCode >= 200 && roteiro.StatusCode <= 299;

                if (!sucessoHttp || roteiro.FalhaRede || roteiro.Timeout)
                {
                    resposta.ErrorMessage = roteiro.Timeout
                        ? ConstantsMessageIntegration.ErroComunicacaoTimeout
                        : roteiro.FalhaRede
                            ? ConstantsMessageIntegration.ErroComunicacaoFalhaRede
                            : ConstantsMessage.ErroIntegracaoExterna;

                    return Task.FromResult(resposta);
                }

                try
                {
                    resposta.Data = string.IsNullOrWhiteSpace(roteiro.Corpo)
                        ? default
                        : IntegrationSerializer.Deserialize<T>(roteiro.Corpo);
                    resposta.IsSuccess = true;
                }
                catch (JsonException)
                {
                    resposta.IsSuccess = false;
                    resposta.ErrorMessage = ConstantsMessageIntegration.ErroComunicacaoDesserializar;
                }

                return Task.FromResult(resposta);
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static ConfiguracaoIntegracao CriarConfiguracao(bool comToken = true, string? valorToken = TokenSecreto, string? publicKey = null)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { ConstantsCaucao.ChaveProvedor, new IntegrationSettingValue(ConstantsCaucao.ProvedorMercadoPago, false) }
            };

            if (comToken)
            {
                settings.Add(ConstantsCaucao.ChaveAccessToken, new IntegrationSettingValue(valorToken ?? string.Empty, true));
            }

            if (publicKey != null)
            {
                settings.Add(ConstantsCaucao.ChavePublica, new IntegrationSettingValue(publicKey, false));
            }

            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://api.mercadopago.com",
                30000,
                settings);
        }

        private static CaucaoSolicitacao CriarSolicitacao()
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = 500m,
                Descricao = "Caucao da locacao",
                TokenCartao = TokenCartao,
                MetodoPagamentoId = "visa",
                Pagador = new CaucaoPagador { Email = EmailPagador, Documento = CpfPagador }
            };
        }

        private static MercadoPagoCaucaoProvider CriarProvider(ComunicacaoRoteirizada comunicacao, FakeLogService logService)
        {
            return new MercadoPagoCaucaoProvider(comunicacao, logService);
        }

        private static string CorpoStatus(string? status, string? statusDetail, decimal? valor = 500m)
        {
            return JsonSerializer.Serialize(new
            {
                id = 42,
                status = status,
                status_detail = statusDetail,
                transaction_amount = valor,
                external_reference = Referencia,
                date_created = CriadoEmTexto,
                date_approved = "2026-11-10T11:00:00.000-03:00",
                card = new { last_four_digits = "4242" }
            });
        }

        private static Task<CaucaoResultado> PreAutorizar(MercadoPagoCaucaoProvider provider, ConfiguracaoIntegracao? configuracao = null)
        {
            return provider.PreAutorizarAsync(configuracao ?? CriarConfiguracao(), CriarSolicitacao(), Chave, 5);
        }

        // ------------------------------------------------------------------
        // Pre-autorizacao
        // ------------------------------------------------------------------

        [Fact]
        public async Task PreAutorizar_201Authorized_DevolveReservadaComCartaoECriadoEm()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoReservada);
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await PreAutorizar(provider);

            Assert.Equal("123456789", resultado.IdentificadorProvedor);
            Assert.Equal(Referencia, resultado.ReferenciaExterna);
            Assert.Equal(CaucaoStatus.RESERVADA, resultado.Status);
            Assert.Equal(500m, resultado.Valor);
            Assert.Null(resultado.ValorCapturado);
            Assert.Equal("4242", resultado.CartaoFinal);
            Assert.Equal(DateTimeOffset.Parse(CriadoEmTexto, CultureInfo.InvariantCulture).ToLocalTime().DateTime, resultado.CriadoEm);
            Assert.Equal(ConstantsCaucao.ProvedorMercadoPago, resultado.Provedor);
            Assert.Single(comunicacao.Requisicoes);
            Assert.Equal(HttpMethod.Post, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task PreAutorizar_NaoPreencheReservaValidaAte_ComoFazOGateway()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoReservada);
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await PreAutorizar(provider);

            Assert.Null(resultado.ReservaValidaAte);
        }

        [Fact]
        public async Task PreAutorizar_SemAccessToken_LancaIntegrationExceptionComAChaveEFazNadaNaRede()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider, CriarConfiguracao(comToken: false)));

            Assert.Equal(ConstantsCaucao.ChaveAccessToken, excecao.Chave);
            Assert.Empty(comunicacao.Requisicoes);
        }

        [Fact]
        public async Task PreAutorizar_AccessTokenDefinirNoBanco_LancaIntegrationExceptionComAChave()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider, CriarConfiguracao(valorToken: ConstantsValues.MarcadorValorNaoDefinido)));

            Assert.Equal(ConstantsCaucao.ChaveAccessToken, excecao.Chave);
            Assert.Empty(comunicacao.Requisicoes);
        }

        [Theory]
        [InlineData("cc_rejected_insufficient_amount", "limite ou saldo insuficiente.")]
        [InlineData("cc_rejected_bad_filled_other_amount", "dados do cartão incorretos.")]
        [InlineData("cc_rejected_call_for_authorize", "o banco emissor exige autorização; peça ao cliente para contatar o banco.")]
        [InlineData("cc_rejected_card_disabled", "cartão desabilitado ou bloqueado; peça ao cliente para contatar o banco.")]
        [InlineData("cc_rejected_high_risk", "recusado por segurança; tente outro cartão.")]
        [InlineData("cc_rejected_other_reason", "recusado pelo emissor; tente outro cartão.")]
        public async Task PreAutorizar_Recusada201_LancaBusinessExceptionComMotivoTraduzido(string statusDetail, string motivo)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoStatus("rejected", statusDetail));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.Equal(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, motivo), excecao.Message);
            Assert.DoesNotContain("cc_rejected", excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_RecusadaComDetalheDesconhecido_UsaMotivoGenerico()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoStatus("rejected", "codigo_novo_xyz"));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.Equal(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoOutro), excecao.Message);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(422)]
        public async Task PreAutorizar_RecusadaPeloProvedor_LancaBusinessExceptionMascaradaETruncada(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            string corpoRecusa = "{\"message\":\"token " + TokenCartao + " " + TokenSecreto + " " + new string('x', 500) + "\"}";
            comunicacao.Responder(statusCode, corpoRecusa);
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            // Mensagem ao cliente fixa: nem o texto do provedor nem os segredos aparecem.
            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
            Assert.DoesNotContain(TokenSecreto, excecao.Message);
            Assert.DoesNotContain(TokenCartao, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_RecusadaQueEcoaEmailECpf_MascaraOsDoisAntesDeDevolverOMotivo()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(400, "{\"message\":\"pagador " + EmailPagador + " cpf " + CpfPagador + " recusado\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.DoesNotContain(EmailPagador, excecao.Message);
            Assert.DoesNotContain(CpfPagador, excecao.Message);
        }

        [Theory]
        [InlineData(401)]
        [InlineData(403)]
        public async Task PreAutorizar_CredenciaisRecusadas_LancaIntegrationException(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, "{\"message\":\"unauthorized\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoCredenciaisRecusadas, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_LimiteDeRequisicoes_LancaIntegrationException()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(429, "{}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoLimiteRequisicoes, excecao.Message);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(503)]
        public async Task PreAutorizar_ProvedorIndisponivel_LancaIntegrationException(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, "{}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoProvedorIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_FalhaDeRede_LancaIntegrationExceptionDeRede()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.FalharRede();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessageIntegration.ErroComunicacaoFalhaRede, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_Timeout_LancaIntegrationExceptionDeTimeout()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Expirar();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessageIntegration.ErroComunicacaoTimeout, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_CorpoSemIdentificador_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"status\":\"authorized\",\"status_detail\":\"pending_capture\",\"transaction_amount\":500,\"external_reference\":\"" + Referencia + "\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_IdZeroOuNegativo_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":0,\"status\":\"authorized\",\"status_detail\":\"pending_capture\",\"transaction_amount\":500,\"external_reference\":\"" + Referencia + "\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_CorpoNaoJson_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, "isto nao e json");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task PreAutorizar_StatusNaoMapeado_RegistraSoCamposPermitidos()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":42,\"status\":\"em_outro_estado\",\"status_detail\":\"" + TokenCartao + "\",\"transaction_amount\":500,\"external_reference\":\"" + Referencia + "\",\"card\":{\"last_four_digits\":\"4242\"}}");
            FakeLogService logService = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, logService);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);

            string registro = Assert.Single(logService.Entries);
            Assert.Contains(ConstantsMessage.ErroCaucaoRespostaInesperada, registro);
            Assert.Contains("integração PAGAMENTO", registro);
            Assert.Contains("provedor MERCADOPAGO", registro);
            Assert.Contains("HTTP 201", registro);
            Assert.Contains("status do provedor em_outro_estado", registro);
            Assert.Contains("identificador 42", registro);
            Assert.DoesNotContain(TokenCartao, registro);
            Assert.DoesNotContain(TokenSecreto, registro);
            Assert.DoesNotContain("4242", registro);
        }

        [Fact]
        public async Task PreAutorizar_ExcecaoInesperada_LogaEEnvolveEmIntegrationExceptionComInner()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada
            {
                ExcecaoLancada = new InvalidOperationException("falha interna simulada")
            };
            FakeLogService logService = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, logService);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoPreAutorizar, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);
            Assert.Contains(logService.Entries, entrada => entrada.Contains(ConstantsMessage.ErroCaucaoPreAutorizar));
        }

        [Fact]
        public async Task PreAutorizar_CancelamentoPedidoPeloChamador_PropagaOperationCanceled()
        {
            using CancellationTokenSource cancelamento = new CancellationTokenSource();
            cancelamento.Cancel();

            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada
            {
                ExcecaoLancada = new OperationCanceledException(cancelamento.Token)
            };
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.PreAutorizarAsync(CriarConfiguracao(), CriarSolicitacao(), Chave, 5, cancelamento.Token));
        }

        // ------------------------------------------------------------------
        // Consulta, captura e cancelamento: tabela de status do Mercado Pago.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("authorized", "pending_capture", CaucaoStatus.RESERVADA)]
        [InlineData("authorized", "outro_detalhe_qualquer", CaucaoStatus.RESERVADA)]
        [InlineData("authorized", null, CaucaoStatus.RESERVADA)]
        [InlineData("approved", null, CaucaoStatus.CAPTURADA)]
        [InlineData("in_process", null, CaucaoStatus.EM_ANALISE)]
        [InlineData("pending", null, CaucaoStatus.EM_ANALISE)]
        [InlineData("rejected", "cc_rejected_insufficient_amount", CaucaoStatus.RECUSADA)]
        [InlineData("cancelled", "expired", CaucaoStatus.EXPIRADA)]
        [InlineData("cancelled", null, CaucaoStatus.CANCELADA)]
        [InlineData("canceled", null, CaucaoStatus.CANCELADA)]
        [InlineData("refunded", null, CaucaoStatus.ESTORNADA)]
        [InlineData("charged_back", null, CaucaoStatus.ESTORNADA)]
        public async Task ConsultarAsync_MapeiaStatusDoMercadoPago(string status, string? statusDetail, CaucaoStatus esperado)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus(status, statusDetail));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal(esperado, resultado.Status);
            Assert.Equal("42", resultado.IdentificadorProvedor);
            Assert.Equal(HttpMethod.Get, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task ConsultarAsync_StatusDesconhecido_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("status_novo", null));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(), "42"));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task ConsultarAsync_Reservada_InformaCartaoECriadoEmEmHoraLocal()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("authorized", "pending_capture"));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal("4242", resultado.CartaoFinal);
            Assert.Equal(DateTimeOffset.Parse(CriadoEmTexto, CultureInfo.InvariantCulture).ToLocalTime().DateTime, resultado.CriadoEm);
        }

        [Fact]
        public async Task ConsultarAsync_Aprovada_PreencheValorCapturadoECapturadoEm()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("approved", null, 300m));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal(300m, resultado.ValorCapturado);
            Assert.Equal(DateTimeOffset.Parse("2026-11-10T11:00:00.000-03:00", CultureInfo.InvariantCulture).ToLocalTime().DateTime, resultado.CapturadoEm);
        }

        [Fact]
        public async Task ConsultarAsync_RecusadaNaConsulta_DevolveStatusSemLancarExcecao()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("rejected", "cc_rejected_high_risk"));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal(CaucaoStatus.RECUSADA, resultado.Status);
        }

        [Fact]
        public async Task ConsultarAsync_NaoEncontrada_LancaNotFoundException()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(404, "{\"message\":\"not found\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => provider.ConsultarAsync(CriarConfiguracao(), "999"));

            Assert.Equal(ConstantsMessage.ErroCaucaoNaoEncontrada, excecao.Message);
        }

        [Fact]
        public async Task CapturarAsync_Parcial_EnviaPutEDevolveCapturadaComValorParcial()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("approved", null, 300m));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.CapturarAsync(CriarConfiguracao(), "42", 300m);

            Assert.Equal(CaucaoStatus.CAPTURADA, resultado.Status);
            Assert.Equal(300m, resultado.ValorCapturado);
            Assert.Equal(HttpMethod.Put, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task CancelarAsync_EnviaPutEDevolveCancelada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus("cancelled", null));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.CancelarAsync(CriarConfiguracao(), "42");

            Assert.Equal(CaucaoStatus.CANCELADA, resultado.Status);
            Assert.Equal(HttpMethod.Put, comunicacao.Requisicoes[0].Method);
        }

        // ------------------------------------------------------------------
        // Segredos: a resposta ecoa token, access_token, e-mail e CPF; nada disso sai em mensagem ou log.
        // ------------------------------------------------------------------

        // ------------------------------------------------------------------
        // Rodada de correcao: mensagem fixa em 400/422, confrontos do pre-autorizado, campos obrigatorios,
        // sanitizacao do log e formato do identificador.
        // ------------------------------------------------------------------

        private static string CorpoPreAutorizacao(string referencia, decimal valor, string? card = "4242", string status = "authorized", string? detalhe = "pending_capture")
        {
            return JsonSerializer.Serialize(new
            {
                id = 123456789,
                status = status,
                status_detail = detalhe,
                transaction_amount = valor,
                external_reference = referencia,
                date_created = CriadoEmTexto,
                card = card == null ? null : new { last_four_digits = card }
            });
        }

        [Theory]
        [InlineData(400)]
        [InlineData(422)]
        public async Task Recusa400_CorpoEcoaEmailEmMaiusculoECpfFormatado_NaoVazaNemNaMensagemNemNoLog(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, "{\"message\":\"MARIA@EXEMPLO.COM 123.456.789-01 recusado\",\"cause\":[{\"code\":\"2067\",\"description\":\"MARIA@EXEMPLO.COM 123.456.789-01\"}]}");
            FakeLogService log = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
            Assert.DoesNotContain("MARIA@EXEMPLO.COM", excecao.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("123.456.789-01", excecao.Message);
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("MARIA@EXEMPLO.COM", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("123.456.789-01"));
            Assert.Contains(log.Entries, entrada => entrada.Contains("2067"));
        }

        [Theory]
        [InlineData("CAUCAO-LOC-123", 1)]
        [InlineData("OUTRA-REFERENCIA", 500)]
        public async Task PreAutorizar_RespostaNaoBateComOPedido_LancaRespostaInesperadaComIdentificadorNoLog(string referencia, decimal valor)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoPreAutorizacao(referencia, valor));
            FakeLogService log = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, log);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
            Assert.Contains(log.Entries, entrada => entrada.Contains("identificador 123456789"));
        }

        [Theory]
        [InlineData("consultar")]
        [InlineData("capturar")]
        [InlineData("cancelar")]
        public async Task Operacoes_CorpoSemTransactionAmount_LancaRespostaInesperada(string operacao)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, "{\"id\":42,\"status\":\"approved\",\"external_reference\":\"" + Referencia + "\"}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());
            ConfiguracaoIntegracao configuracao = CriarConfiguracao();

            Func<Task> chamada = operacao switch
            {
                "consultar" => () => provider.ConsultarAsync(configuracao, "42"),
                "capturar" => () => provider.CapturarAsync(configuracao, "42", 300m),
                _ => () => provider.CancelarAsync(configuracao, "42")
            };

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(chamada);

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Theory]
        [InlineData("{\"id\":0,\"status\":\"approved\",\"transaction_amount\":500}")]
        [InlineData("{\"status\":\"approved\",\"transaction_amount\":500}")]
        public async Task Consultar_IdZeroOuAusente_LancaRespostaInesperada(string corpo)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, corpo);
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(), "42"));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task Consultar_TransactionAmountZeroOuNegativo_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoPreAutorizacao(Referencia, 0m));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.ConsultarAsync(CriarConfiguracao(), "42"));

            Assert.Equal(ConstantsMessage.ErroCaucaoRespostaInesperada, excecao.Message);
        }

        [Theory]
        [InlineData("ab12x")]
        [InlineData("42424")]
        public async Task Consultar_CartaoFinalForaDoFormato_DevolveNulo(string card)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoPreAutorizacao(Referencia, 500m, card, "authorized", "pending_capture"));
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            CaucaoResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Null(resultado.CartaoFinal);
        }

        [Fact]
        public async Task Log_StatusComControleEDetalhe_SaiSemQuebraDeLinhaEComDetalhe()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":42,\"status\":\"em\\r\\nout\",\"status_detail\":\"det_codigo\",\"transaction_amount\":500,\"external_reference\":\"" + Referencia + "\"}");
            FakeLogService log = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, log);

            await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            string registro = Assert.Single(log.Entries);
            Assert.Contains("status do provedor emout", registro);
            Assert.Contains("detalhe do provedor det_codigo", registro);
            Assert.DoesNotContain('\r', registro);
            Assert.DoesNotContain('\n', registro);
        }

        [Theory]
        [InlineData("..")]
        [InlineData("123/../x")]
        [InlineData("FAKE-CAU-000000000000")]
        [InlineData("")]
        public void IdentificadorValido_ForaDoFormatoDoMercadoPago_DevolveFalso(string identificador)
        {
            MercadoPagoCaucaoProvider provider = CriarProvider(new ComunicacaoRoteirizada(), new FakeLogService());

            Assert.False(provider.IdentificadorValido(identificador));
        }

        [Fact]
        public void IdentificadorValido_Numerico_DevolveVerdadeiro()
        {
            MercadoPagoCaucaoProvider provider = CriarProvider(new ComunicacaoRoteirizada(), new FakeLogService());

            Assert.True(provider.IdentificadorValido("123456789"));
        }

        // ------------------------------------------------------------------
        // Rodada 2: item nulo em cause, codigos numericos curtos no log, rejected antes do confronto,
        // sanitizacao de controles Unicode.
        // ------------------------------------------------------------------

        [Fact]
        public async Task Recusa400_CauseComItemNulo_LancaBusinessExceptionFixaNaoIntegrationException()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(400, "{\"message\":\"recusado\",\"cause\":[null]}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
        }

        [Fact]
        public async Task Captura_CauseComCodigoLongo_NaoVaiParaOLog()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(400, "{\"cause\":[{\"code\":\"12345678901\"}]}");
            FakeLogService log = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, log);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CapturarAsync(CriarConfiguracao(), "42", 300m));

            Assert.Equal(ConstantsMessage.ErroCaucaoSolicitacaoRecusada, excecao.Message);
            Assert.DoesNotContain(log.Entries, entrada => entrada.Contains("12345678901"));
            Assert.Contains(log.Entries, entrada => entrada.Contains("códigos de causa -"));
        }

        [Fact]
        public async Task PreAutorizar_Rejected201SemExternalReference_LancaCartaoRecusadoNaoRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":42,\"status\":\"rejected\",\"status_detail\":\"cc_rejected_insufficient_amount\",\"transaction_amount\":500}");
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.Equal(string.Format(ConstantsMessage.ErroCaucaoCartaoRecusado, ConstantsMessage.MotivoCaucaoSaldoInsuficiente), excecao.Message);
        }

        [Theory]
        [InlineData(0x2028, "ab")]
        [InlineData(0x0085, "ab")]
        [InlineData(0x007F, "ab")]
        public async Task Log_ControlesEUnicodeDeSeparacaoSaoRemovidos(int codigoCaractere, string esperado)
        {
            string status = "a" + (char)codigoCaractere + "b";
            string corpo = JsonSerializer.Serialize(new
            {
                id = 42,
                status = status,
                transaction_amount = 500,
                external_reference = Referencia
            });
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, corpo);
            FakeLogService log = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, log);

            await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            string registro = Assert.Single(log.Entries);
            Assert.Contains("status do provedor " + esperado, registro);
            // Comparacao por caractere (ordinal): a versao por string usa cultura e ignora DEL/controles.
            Assert.DoesNotContain((char)codigoCaractere, registro);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(404)]
        [InlineData(500)]
        public async Task Segredos_RespostaQueEcoaSegredos_NaoVazaEmMensagemNemLog(int statusCode)
        {
            string corpoEcoado = "{\"message\":\"" + TokenSecreto + " " + TokenCartao + " " + EmailPagador + " " + CpfPagador + "\"}";
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, corpoEcoado);
            FakeLogService logService = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, logService);

            Exception excecao = await Assert.ThrowsAnyAsync<Exception>(() => PreAutorizar(provider));

            string[] segredos = { TokenSecreto, TokenCartao, EmailPagador, CpfPagador };

            foreach (string segredo in segredos)
            {
                Assert.DoesNotContain(segredo, excecao.Message);
                Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains(segredo));
            }
        }

        [Fact]
        public async Task Segredos_RecusaComSegredosNoCorpo_NaoVazaNoMotivoDevolvido()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":42,\"status\":\"rejected\",\"status_detail\":\"cc_rejected_other_reason\",\"external_reference\":\"" + Referencia + "\",\"transaction_amount\":500,\"message\":\"" + TokenCartao + "\"}");
            FakeLogService logService = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, logService);

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => PreAutorizar(provider));

            Assert.DoesNotContain(TokenCartao, excecao.Message);
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains(TokenCartao));
        }

        [Fact]
        public async Task Segredos_RespostaInesperadaComSegredosNoStatusDetail_NaoVazaNoLog()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":42,\"status\":\"" + TokenSecreto + "\",\"status_detail\":\"" + EmailPagador + "\",\"transaction_amount\":500}");
            FakeLogService logService = new FakeLogService();
            MercadoPagoCaucaoProvider provider = CriarProvider(comunicacao, logService);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => PreAutorizar(provider));

            Assert.DoesNotContain(TokenSecreto, excecao.Message);
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains(TokenSecreto));
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains(EmailPagador));
        }
    }
}
