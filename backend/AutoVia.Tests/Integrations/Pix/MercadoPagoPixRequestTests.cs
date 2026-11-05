using AutoVia.Communication.Enums;
using AutoVia.Integrations.Communication.Base;
using AutoVia.Integrations.Communication.Pix;
using AutoVia.Integrations.Helpers;
using AutoVia.Integrations.Requests.Pix;
using System.Globalization;
using System.Text.Json;

namespace AutoVia.Tests.Integrations.Pix
{
    public class MercadoPagoPixRequestTests
    {
        private const string TokenSecreto = "APP_USR-token-secreto-123";
        private const string ReferenciaExterna = "PED-2026-0001";
        private static readonly DateTime Agora = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Local);

        private static ConfiguracaoIntegracao CriarConfiguracao(bool tokenProtegido = true)
        {
            Dictionary<string, IntegrationSettingValue> settings = new Dictionary<string, IntegrationSettingValue>(StringComparer.OrdinalIgnoreCase)
            {
                { "provedor", new IntegrationSettingValue("MERCADOPAGO", false) },
                { "access_token", new IntegrationSettingValue(TokenSecreto, tokenProtegido) }
            };

            return new ConfiguracaoIntegracao(
                ConstantsIntegration.Pagamento,
                "Pagamento",
                AmbienteIntegracao.SANDBOX,
                "https://api.mercadopago.com",
                15000,
                settings);
        }

        private static PixCobrancaSolicitacao CriarSolicitacao(decimal valor, string? documento = "12345678901")
        {
            return new PixCobrancaSolicitacao
            {
                ReferenciaExterna = ReferenciaExterna,
                Valor = valor,
                Descricao = "Diaria de locacao",
                Pagador = new PixPagador
                {
                    Nome = "Maria da Silva",
                    Email = "maria@exemplo.com",
                    Documento = documento
                }
            };
        }

        [Fact]
        public void Criar_MontaPostComAutorizacaoIdempotenciaERetry()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(10.5m), 3600, Agora);

            Assert.Equal(HttpMethod.Post, requisicao.Method);
            Assert.Equal("https://api.mercadopago.com", requisicao.BaseUrl);
            Assert.Equal("/v1/payments", requisicao.Resource);
            Assert.Equal(15000, requisicao.TimeoutMs);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal(ReferenciaExterna, requisicao.IdempotencyKey);
            Assert.Equal("Bearer " + TokenSecreto, requisicao.Headers["Authorization"]);
            Assert.Equal(ReferenciaExterna, requisicao.Headers["X-Idempotency-Key"]);
        }

        [Fact]
        public void Criar_SensitiveValuesIncluiOTokenMesmoSeNaoProtegidoNoBanco()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(tokenProtegido: false), TokenSecreto, CriarSolicitacao(10.5m), 3600, Agora);

            Assert.Contains(TokenSecreto, requisicao.SensitiveValues);
        }

        [Fact]
        public void Criar_SensitiveValuesIncluiEmailECpfDoPagador()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(10.5m), 3600, Agora);

            Assert.Contains("maria@exemplo.com", requisicao.SensitiveValues);
            Assert.Contains("12345678901", requisicao.SensitiveValues);
        }

        [Fact]
        public void Criar_CorpoTemValorComDuasCasasEFormatoDoContrato()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(10.5m), 3600, Agora);

            MercadoPagoCriarPagamentoRequest corpo = Assert.IsType<MercadoPagoCriarPagamentoRequest>(requisicao.Body);
            Assert.Equal(10.50m, corpo.TransactionAmount);

            string json = IntegrationSerializer.Serialize(corpo);
            using JsonDocument documento = JsonDocument.Parse(json);
            JsonElement raiz = documento.RootElement;

            Assert.Equal(JsonValueKind.Number, raiz.GetProperty("transaction_amount").ValueKind);
            Assert.Equal("10.50", raiz.GetProperty("transaction_amount").GetRawText());
            Assert.Equal("pix", raiz.GetProperty("payment_method_id").GetString());
            Assert.Equal(ReferenciaExterna, raiz.GetProperty("external_reference").GetString());
            Assert.Equal("Diaria de locacao", raiz.GetProperty("description").GetString());

            JsonElement pagador = raiz.GetProperty("payer");
            Assert.Equal("maria@exemplo.com", pagador.GetProperty("email").GetString());
            Assert.Equal("Maria", pagador.GetProperty("first_name").GetString());
            Assert.Equal("da Silva", pagador.GetProperty("last_name").GetString());
            Assert.Equal("CPF", pagador.GetProperty("identification").GetProperty("type").GetString());
            Assert.Equal("12345678901", pagador.GetProperty("identification").GetProperty("number").GetString());
        }

        [Fact]
        public void Criar_DataDeExpiracaoTemOffsetEApontaParaAgoraMaisOsSegundos()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(10.5m), 3600, Agora);

            MercadoPagoCriarPagamentoRequest corpo = Assert.IsType<MercadoPagoCriarPagamentoRequest>(requisicao.Body);
            DateTimeOffset esperado = new DateTimeOffset(Agora).AddSeconds(3600);
            DateTimeOffset recebido = DateTimeOffset.Parse(corpo.DateOfExpiration, CultureInfo.InvariantCulture);

            Assert.Matches(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2}$", corpo.DateOfExpiration);
            Assert.Equal(esperado.UtcDateTime, recebido.UtcDateTime);
        }

        [Fact]
        public void Criar_SemDocumento_OmiteIdentificacao()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Criar(CriarConfiguracao(), TokenSecreto, CriarSolicitacao(10.5m, documento: null), 3600, Agora);

            string json = IntegrationSerializer.Serialize(requisicao.Body);

            Assert.DoesNotContain("identification", json);
        }

        [Fact]
        public void Consultar_EhGetSemCorpoEComIdentificador()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Consultar(CriarConfiguracao(), TokenSecreto, "123456789");

            Assert.Equal(HttpMethod.Get, requisicao.Method);
            Assert.Equal("/v1/payments/123456789", requisicao.Resource);
            Assert.Null(requisicao.Body);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal("Bearer " + TokenSecreto, requisicao.Headers["Authorization"]);
        }

        [Fact]
        public void Cancelar_EhPutComStatusCancelled()
        {
            RequestInfo requisicao = MercadoPagoPixRequest.Cancelar(CriarConfiguracao(), TokenSecreto, "123456789");

            Assert.Equal(HttpMethod.Put, requisicao.Method);
            Assert.Equal("/v1/payments/123456789", requisicao.Resource);
            Assert.True(requisicao.AllowRetry);
            Assert.Equal("{\"status\":\"cancelled\"}", IntegrationSerializer.Serialize(requisicao.Body));
        }
    }
}
