using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoVia.Integrations.Requests.Caucao
{
    /// <summary>Corpo do POST /v1/payments com capture=false (pre-autorizacao). DTO interno.</summary>
    public class MercadoPagoCriarCaucaoRequest
    {
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        /// <summary>Token de uso unico gerado no front. Nunca vai para o log.</summary>
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("installments")]
        public int Installments { get; set; }

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; } = string.Empty;

        [JsonPropertyName("issuer_id")]
        public long? IssuerId { get; set; }

        [JsonPropertyName("capture")]
        public bool Capture { get; set; }

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("payer")]
        public MercadoPagoPagadorCaucaoRequest Payer { get; set; } = new MercadoPagoPagadorCaucaoRequest();
    }

    /// <summary>Pagador do corpo de pre-autorizacao. E-mail e CPF nunca vao para o log.</summary>
    public class MercadoPagoPagadorCaucaoRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("identification")]
        public MercadoPagoIdentificacaoCaucaoRequest? Identification { get; set; }
    }

    /// <summary>Documento do pagador (CPF).</summary>
    public class MercadoPagoIdentificacaoCaucaoRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;
    }

    /// <summary>
    /// Corpo do PUT /v1/payments/{id} para captura: {"capture":true} (integral) ou
    /// {"capture":true,"transaction_amount":120.00} (parcial). Campo nulo nao e enviado.
    /// </summary>
    public class MercadoPagoCapturaCaucaoRequest
    {
        [JsonPropertyName("capture")]
        public bool Capture { get; set; }

        [JsonPropertyName("transaction_amount")]
        public decimal? TransactionAmount { get; set; }
    }

    /// <summary>Corpo do PUT /v1/payments/{id} para cancelamento: {"status":"cancelled"}.</summary>
    public class MercadoPagoCancelamentoCaucaoRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Resposta de pre-autorizacao, consulta, captura e cancelamento do Mercado Pago.</summary>
    public class MercadoPagoPaymentCaucaoResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("status_detail")]
        public string? StatusDetail { get; set; }

        [JsonPropertyName("transaction_amount")]
        public decimal? TransactionAmount { get; set; }

        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }

        [JsonPropertyName("date_created")]
        public string? DateCreated { get; set; }

        [JsonPropertyName("date_approved")]
        public string? DateApproved { get; set; }

        [JsonPropertyName("captured")]
        public bool? Captured { get; set; }

        [JsonPropertyName("card")]
        public MercadoPagoCartaoCaucaoResponse? Card { get; set; }
    }

    /// <summary>Corpo de erro (400/422) do Mercado Pago. So os codigos de causa sao lidos; o texto nunca sai.</summary>
    public class MercadoPagoErroCaucaoResponse
    {
        [JsonPropertyName("cause")]
        public List<MercadoPagoCausaCaucaoResponse?>? Cause { get; set; }
    }

    /// <summary>Causa do erro. O codigo pode vir como texto ou numero, por isso e lido como JsonElement.</summary>
    public class MercadoPagoCausaCaucaoResponse
    {
        [JsonPropertyName("code")]
        public JsonElement? Code { get; set; }
    }

    /// <summary>Bloco card da resposta. So os ultimos digitos sao usados (recibo).</summary>
    public class MercadoPagoCartaoCaucaoResponse
    {
        [JsonPropertyName("last_four_digits")]
        public string? LastFourDigits { get; set; }
    }
}
