using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Caucao;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Helpers.Caucao;
using AutoVia.Integrations.Requests.Caucao;
using System.Text.Json;

namespace AutoVia.Tests.Integrations.Caucao
{
    public class MercadoPagoCaucaoRequestTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string TokenCartao = "tok_cartao_9876543210";
        private const string EmailPagador = "maria@exemplo.com";
        private const string CpfPagador = "12345678901";
        private const string DispositivoSecreto = "device-abc-987654";
        private const string Chave = "caucao-0123456789abcdef0123456789abcdef01234567";
        private const string Referencia = "CAUCAO-LOC-123";

        private static ConfiguracaoIntegracao CriarConfiguracao(bool tokenProtegido = true)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { ConstantsCaucao.ChaveProvedor, new IntegrationSettingValue(ConstantsCaucao.ProvedorMercadoPago, false) },
                { ConstantsCaucao.ChaveAccessToken, new IntegrationSettingValue(TokenSecreto, tokenProtegido) }
            };

            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://api.mercadopago.com",
                30000,
                settings);
        }

        private static CaucaoSolicitacao CriarSolicitacao(decimal valor = 500m, string? dispositivo = null, long? emissor = 1234L)
        {
            return new CaucaoSolicitacao
            {
                ReferenciaExterna = Referencia,
                Valor = valor,
                Descricao = "Caucao da locacao",
                TokenCartao = TokenCartao,
                MetodoPagamentoId = "visa",
                EmissorId = emissor,
                Pagador = new CaucaoPagador { Email = EmailPagador, Documento = CpfPagador },
                IdentificadorDispositivo = dispositivo
            };
        }

        private static JsonElement CorpoComoJson(RequestInfo requisicao)
        {
            string json = IntegrationSerializer.Serialize(requisicao.Body);
            using JsonDocument documento = JsonDocument.Parse(json);
            return documento.RootElement.Clone();
        }

        [Fact]
        public void PreAutorizar_EhPostComCapturaFalsaParcelaUnicaEToken()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(), Chave);

            Assert.Equal(HttpMethod.Post, requisicao.Method);
            Assert.Equal("https://api.mercadopago.com", requisicao.BaseUrl);
            Assert.Equal("/v1/payments", requisicao.Resource);
            Assert.Equal(30000, requisicao.TimeoutMs);
            Assert.True(requisicao.AllowRetry);

            JsonElement corpo = CorpoComoJson(requisicao);
            Assert.False(corpo.GetProperty("capture").GetBoolean());
            Assert.Equal(1, corpo.GetProperty("installments").GetInt32());
            Assert.Equal(TokenCartao, corpo.GetProperty("token").GetString());
            Assert.Equal("visa", corpo.GetProperty("payment_method_id").GetString());
            Assert.Equal(Referencia, corpo.GetProperty("external_reference").GetString());
            Assert.Equal(1234L, corpo.GetProperty("issuer_id").GetInt64());
        }

        [Fact]
        public void PreAutorizar_CabecalhosTemAutorizacaoEChaveDeIdempotencia()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(), Chave);

            Assert.Equal("Bearer " + TokenSecreto, requisicao.Headers["Authorization"]);
            Assert.Equal(Chave, requisicao.Headers["X-Idempotency-Key"]);
            Assert.Equal(Chave, requisicao.IdempotencyKey);
        }

        [Fact]
        public void PreAutorizar_ValorComDuasCasasNoCorpo()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(valor: 500m), Chave);

            JsonElement corpo = CorpoComoJson(requisicao);
            Assert.Equal("500.00", corpo.GetProperty("transaction_amount").GetRawText());
        }

        [Fact]
        public void PreAutorizar_ComDispositivo_EnviaCabecalhoDeSessao()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(dispositivo: DispositivoSecreto), Chave);

            Assert.Equal(DispositivoSecreto, requisicao.Headers["X-meli-session-id"]);
        }

        [Fact]
        public void PreAutorizar_SemDispositivo_NaoEnviaCabecalhoDeSessao()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(dispositivo: null), Chave);

            Assert.False(requisicao.Headers.ContainsKey("X-meli-session-id"));
        }

        [Fact]
        public void PreAutorizar_SemEmissor_OmiteIssuerId()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(emissor: null), Chave);

            JsonElement corpo = CorpoComoJson(requisicao);
            Assert.False(corpo.TryGetProperty("issuer_id", out JsonElement emissor));
        }

        [Fact]
        public void PreAutorizar_SensitiveValuesIncluiACpfFormatado()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(), Chave);

            Assert.Contains("123.456.789-01", requisicao.SensitiveValues);
        }

        [Fact]
        public void PreAutorizar_SemCpf_OmiteIdentificacao()
        {
            CaucaoSolicitacao solicitacao = CriarSolicitacao();
            solicitacao.Pagador.Documento = null;

            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, solicitacao, Chave);

            JsonElement corpo = CorpoComoJson(requisicao);
            Assert.False(corpo.GetProperty("payer").TryGetProperty("identification", out JsonElement identificacao));
        }

        [Fact]
        public void PreAutorizar_PagadorTemEmailECpfNoFormatoDoContrato()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(), Chave);

            JsonElement pagador = CorpoComoJson(requisicao).GetProperty("payer");
            Assert.Equal(EmailPagador, pagador.GetProperty("email").GetString());
            Assert.Equal("CPF", pagador.GetProperty("identification").GetProperty("type").GetString());
            Assert.Equal(CpfPagador, pagador.GetProperty("identification").GetProperty("number").GetString());
        }

        [Fact]
        public void PreAutorizar_SensitiveValuesTemTokenEmailCpfEAccessToken()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(), Chave);

            Assert.Contains(TokenCartao, requisicao.SensitiveValues);
            Assert.Contains(EmailPagador, requisicao.SensitiveValues);
            Assert.Contains(CpfPagador, requisicao.SensitiveValues);
            Assert.Contains(TokenSecreto, requisicao.SensitiveValues);
        }

        [Fact]
        public void PreAutorizar_SensitiveValuesIncluiDispositivoEOAccessTokenMesmoSemProtecao()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.PreAutorizar(CriarConfiguracao(tokenProtegido: false), TokenSecreto, CriarSolicitacao(dispositivo: DispositivoSecreto), Chave);

            Assert.Contains(DispositivoSecreto, requisicao.SensitiveValues);
            Assert.Contains(TokenSecreto, requisicao.SensitiveValues);
        }

        [Fact]
        public void Capturar_Integral_EhPutSemTransactionAmount()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.Capturar(CriarConfiguracao(), TokenSecreto, "123456789", null);

            Assert.Equal(HttpMethod.Put, requisicao.Method);
            Assert.Equal("/v1/payments/123456789", requisicao.Resource);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal("{\"capture\":true}", IntegrationSerializer.Serialize(requisicao.Body));
        }

        [Fact]
        public void Capturar_Parcial_EnviaTransactionAmountComDuasCasas()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.Capturar(CriarConfiguracao(), TokenSecreto, "123456789", 120m);

            JsonElement corpo = CorpoComoJson(requisicao);
            Assert.True(corpo.GetProperty("capture").GetBoolean());
            Assert.Equal("120.00", corpo.GetProperty("transaction_amount").GetRawText());
        }

        [Fact]
        public void Cancelar_EhPutComAGrafiaDaConstante()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.Cancelar(CriarConfiguracao(), TokenSecreto, "123456789");

            Assert.Equal(HttpMethod.Put, requisicao.Method);
            Assert.Equal("/v1/payments/123456789", requisicao.Resource);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal("{\"status\":\"" + ConstantsCaucao.MercadoPagoStatusCancelado + "\"}", IntegrationSerializer.Serialize(requisicao.Body));
        }

        [Fact]
        public void Consultar_EhGetSemCorpoEComIdentificador()
        {
            RequestInfo requisicao = MercadoPagoCaucaoRequest.Consultar(CriarConfiguracao(), TokenSecreto, "123456789");

            Assert.Equal(HttpMethod.Get, requisicao.Method);
            Assert.Equal("/v1/payments/123456789", requisicao.Resource);
            Assert.Null(requisicao.Body);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal("Bearer " + TokenSecreto, requisicao.Headers["Authorization"]);
        }
    }
}
