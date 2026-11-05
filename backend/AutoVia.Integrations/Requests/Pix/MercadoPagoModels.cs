using System.Text.Json.Serialization;

namespace AutoVia.Integrations.Requests.Pix
{
    /// <summary>Corpo do POST /v1/payments do Mercado Pago (DTO interno, nao sai da biblioteca).</summary>
    public class MercadoPagoCriarPagamentoRequest
    {
        [JsonPropertyName("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("payment_method_id")]
        public string PaymentMethodId { get; set; } = string.Empty;

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("date_of_expiration")]
        public string DateOfExpiration { get; set; } = string.Empty;

        [JsonPropertyName("payer")]
        public MercadoPagoPagadorRequest Payer { get; set; } = new MercadoPagoPagadorRequest();
    }

    /// <summary>Pagador do corpo de criacao. Nome, e-mail e CPF nunca vao para o log.</summary>
    public class MercadoPagoPagadorRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("identification")]
        public MercadoPagoIdentificacaoRequest? Identification { get; set; }
    }

    /// <summary>Documento do pagador (CPF).</summary>
    public class MercadoPagoIdentificacaoRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;
    }

    /// <summary>Corpo do PUT /v1/payments/{id} para cancelamento: {"status":"cancelled"}.</summary>
    public class MercadoPagoCancelamentoRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Resposta de criacao, consulta e cancelamento de pagamento do Mercado Pago.</summary>
    public class MercadoPagoPaymentResponse
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

        [JsonPropertyName("date_of_expiration")]
        public string? DateOfExpiration { get; set; }

        [JsonPropertyName("date_approved")]
        public string? DateApproved { get; set; }

        [JsonPropertyName("point_of_interaction")]
        public MercadoPagoPointOfInteraction? PointOfInteraction { get; set; }
    }

    /// <summary>Bloco point_of_interaction da resposta.</summary>
    public class MercadoPagoPointOfInteraction
    {
        [JsonPropertyName("transaction_data")]
        public MercadoPagoTransactionData? TransactionData { get; set; }
    }

    /// <summary>Dados do PIX (copia e cola, QR Code e link). Nunca vao para o log.</summary>
    public class MercadoPagoTransactionData
    {
        [JsonPropertyName("qr_code")]
        public string? QrCode { get; set; }

        [JsonPropertyName("qr_code_base64")]
        public string? QrCodeBase64 { get; set; }

        [JsonPropertyName("ticket_url")]
        public string? TicketUrl { get; set; }
    }
}
