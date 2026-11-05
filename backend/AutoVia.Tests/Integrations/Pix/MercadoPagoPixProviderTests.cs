using AutoVia.Communication.CustomExceptions;
using AutoVia.Communication.Enums;
using AutoVia.Helpers;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Pix;
using AutoVia.Integrations.Interfaces;
using AutoVia.Integrations.Services.Pix;
using AutoVia.Tests.Fakes;
using System.Globalization;
using System.Text.Json;

namespace AutoVia.Tests.Integrations.Pix
{
    public class MercadoPagoPixProviderTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string Referencia = "PED-2026-0001";
        private const string CorpoCriacaoValido = "{\"id\":123456789,\"status\":\"pending\",\"status_detail\":\"pending_waiting_transfer\",\"transaction_amount\":10.5,\"external_reference\":\"PED-2026-0001\",\"date_of_expiration\":\"2026-11-05T11:00:00.000-03:00\",\"point_of_interaction\":{\"transaction_data\":{\"qr_code\":\"00020126COPIA-E-COLA\",\"qr_code_base64\":\"iVBORw0KGgo=\",\"ticket_url\":\"https://www.mercadopago.com.br/ticket\"}}}";

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

        private static ConfiguracaoIntegracao CriarConfiguracao(bool comToken = true, string? valorToken = TokenSecreto)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "provedor", new IntegrationSettingValue("MERCADOPAGO", false) }
            };

            if (comToken)
            {
                settings.Add("access_token", new IntegrationSettingValue(valorToken ?? string.Empty, true));
            }

            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://api.mercadopago.com",
                15000,
                settings);
        }

        private static PixCobrancaSolicitacao CriarSolicitacao()
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = 10.5m,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador { Nome = "Maria da Silva", Email = "maria@exemplo.com", Documento = "12345678901" }
            };
        }

        private static MercadoPagoPixProvider CriarProvider(ComunicacaoRoteirizada comunicacao, FakeLogService logService)
        {
            return new MercadoPagoPixProvider(comunicacao, logService);
        }

        private static string CorpoStatus(string status, string? statusDetail)
        {
            return JsonSerializer.Serialize(new
            {
                id = 42,
                status = status,
                status_detail = statusDetail,
                transaction_amount = 10.5,
                external_reference = Referencia
            });
        }

        // ------------------------------------------------------------------
        // Criacao
        // ------------------------------------------------------------------

        [Fact]
        public async Task CriarAsync_RespostaValida_DevolveCobrancaAguardandoPagamento()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, CorpoCriacaoValido);
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            PixCobrancaResultado resultado = await provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600);

            Assert.Equal("123456789", resultado.IdentificadorProvedor);
            Assert.Equal(Referencia, resultado.ReferenciaExterna);
            Assert.Equal(PixStatusCobranca.AGUARDANDO_PAGAMENTO, resultado.Status);
            Assert.Equal(10.5m, resultado.Valor);
            Assert.Equal("00020126COPIA-E-COLA", resultado.CopiaECola);
            Assert.Equal("iVBORw0KGgo=", resultado.QrCodeBase64);
            Assert.Equal("https://www.mercadopago.com.br/ticket", resultado.UrlCobranca);
            Assert.Equal(ConstantsPix.ProvedorMercadoPago, resultado.Provedor);
            Assert.Equal(DateTimeOffset.Parse("2026-11-05T11:00:00.000-03:00", CultureInfo.InvariantCulture).ToLocalTime().DateTime, resultado.ExpiraEm);
            Assert.Null(resultado.PagoEm);
            Assert.Single(comunicacao.Requisicoes);
            Assert.Equal(HttpMethod.Post, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task CriarAsync_SemAccessToken_LancaIntegrationExceptionComAChaveEFazNadaNaRede()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(comToken: false), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsPix.ChaveAccessToken, excecao.Chave);
            Assert.Empty(comunicacao.Requisicoes);
        }

        [Fact]
        public async Task CriarAsync_AccessTokenDefinirNoBanco_LancaIntegrationExceptionComAChave()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(valorToken: ConstantsValues.MarcadorValorNaoDefinido), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsPix.ChaveAccessToken, excecao.Chave);
            Assert.Empty(comunicacao.Requisicoes);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(422)]
        public async Task CriarAsync_RecusadaPeloProvedor_LancaBusinessExceptionComMotivoMascaradoETruncado(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            string corpoRecusa = "{\"message\":\"token " + TokenSecreto + " " + new string('x', 500) + "\"}";
            comunicacao.Responder(statusCode, corpoRecusa);
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            BusinessException excecao = await Assert.ThrowsAsync<BusinessException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            string prefixo = ConstantsMessage.ErroPixCobrancaRecusada.Replace("{0}", string.Empty);
            Assert.StartsWith(prefixo, excecao.Message);
            Assert.DoesNotContain(TokenSecreto, excecao.Message);
            Assert.Contains(ConstantsIntegration.Mascara, excecao.Message);
            Assert.True(excecao.Message.Length <= prefixo.Length + ConstantsPix.TamanhoMaximoMotivoRecusa);
        }

        [Theory]
        [InlineData(401)]
        [InlineData(403)]
        public async Task CriarAsync_CredenciaisRecusadas_LancaIntegrationException(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, "{\"message\":\"unauthorized\"}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixCredenciaisRecusadas, excecao.Message);
            Assert.DoesNotContain(TokenSecreto, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_LimiteDeRequisicoes_LancaIntegrationException()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(429, "{}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixLimiteRequisicoes, excecao.Message);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(503)]
        public async Task CriarAsync_ProvedorIndisponivel_LancaIntegrationException(int statusCode)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(statusCode, "{}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixProvedorIndisponivel, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_FalhaDeRede_LancaIntegrationExceptionDeRede()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.FalharRede();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessageIntegration.ErroComunicacaoFalhaRede, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_Timeout_LancaIntegrationExceptionDeTimeout()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Expirar();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessageIntegration.ErroComunicacaoTimeout, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_CorpoSemIdentificador_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"status\":\"pending\",\"transaction_amount\":10.5}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_CorpoSemQrCodeOuCopiaECola_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(201, "{\"id\":1,\"status\":\"pending\",\"transaction_amount\":10.5}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_CorpoNaoJson_LancaRespostaInesperada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, "isto nao e json");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixRespostaInesperada, excecao.Message);
        }

        [Fact]
        public async Task CriarAsync_ExcecaoInesperada_LogaEEnvolveEmIntegrationExceptionComInner()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.ExcecaoLancada = new InvalidOperationException("falha interna simulada");
            FakeLogService logService = new FakeLogService();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, logService);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.Equal(ConstantsMessage.ErroPixCriarCobranca, excecao.Message);
            Assert.IsType<InvalidOperationException>(excecao.InnerException);
            Assert.Contains(logService.Entries, entrada => entrada.Contains(ConstantsMessage.ErroPixCriarCobranca));
        }

        // ------------------------------------------------------------------
        // Consulta e cancelamento
        // ------------------------------------------------------------------

        [Fact]
        public async Task ConsultarAsync_NaoEncontrada_LancaNotFoundException()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(404, "{\"message\":\"not found\"}");
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            NotFoundException excecao = await Assert.ThrowsAsync<NotFoundException>(() => provider.ConsultarAsync(CriarConfiguracao(), "999"));

            Assert.Equal(ConstantsMessage.ErroPixCobrancaNaoEncontrada, excecao.Message);
        }

        [Theory]
        [InlineData("pending", null, PixStatusCobranca.AGUARDANDO_PAGAMENTO)]
        [InlineData("approved", null, PixStatusCobranca.PAGA)]
        [InlineData("cancelled", "expired", PixStatusCobranca.EXPIRADA)]
        [InlineData("cancelled", null, PixStatusCobranca.CANCELADA)]
        [InlineData("rejected", null, PixStatusCobranca.RECUSADA)]
        public async Task ConsultarAsync_MapeiaStatusDoMercadoPago(string status, string? statusDetail, PixStatusCobranca esperado)
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(200, CorpoStatus(status, statusDetail));
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            PixCobrancaResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal(esperado, resultado.Status);
            Assert.Equal("42", resultado.IdentificadorProvedor);
            Assert.Equal(Referencia, resultado.ReferenciaExterna);
            Assert.Equal(HttpMethod.Get, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task ConsultarAsync_Aprovada_InformaDataDoPagamentoEmHoraLocal()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            string corpo = "{\"id\":42,\"status\":\"approved\",\"transaction_amount\":10.5,\"date_approved\":\"2026-11-05T10:30:00.000-03:00\"}";
            comunicacao.Responder(200, corpo);
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            PixCobrancaResultado resultado = await provider.ConsultarAsync(CriarConfiguracao(), "42");

            Assert.Equal(DateTimeOffset.Parse("2026-11-05T10:30:00.000-03:00", CultureInfo.InvariantCulture).ToLocalTime().DateTime, resultado.PagoEm);
        }

        [Fact]
        public async Task CancelarAsync_EnviaPutEDevolveCancelada()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            string corpo = "{\"id\":42,\"status\":\"cancelled\",\"transaction_amount\":10.5,\"external_reference\":\"PED-2026-0001\"}";
            comunicacao.Responder(200, corpo);
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, new FakeLogService());

            PixCobrancaResultado resultado = await provider.CancelarAsync(CriarConfiguracao(), "42");

            Assert.Equal(PixStatusCobranca.CANCELADA, resultado.Status);
            Assert.Equal(HttpMethod.Put, comunicacao.Requisicoes[0].Method);
        }

        [Fact]
        public async Task CriarAsync_NaoVazaOTokenNasMensagensDeErro()
        {
            ComunicacaoRoteirizada comunicacao = new ComunicacaoRoteirizada();
            comunicacao.Responder(401, "{\"message\":\"" + TokenSecreto + "\"}");
            FakeLogService logService = new FakeLogService();
            MercadoPagoPixProvider provider = CriarProvider(comunicacao, logService);

            IntegrationException excecao = await Assert.ThrowsAsync<IntegrationException>(() => provider.CriarAsync(CriarConfiguracao(), CriarSolicitacao(), 3600));

            Assert.DoesNotContain(TokenSecreto, excecao.Message);
            Assert.DoesNotContain(logService.Entries, entrada => entrada.Contains(TokenSecreto));
        }
    }
}
