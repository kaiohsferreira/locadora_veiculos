namespace AutoVia.Integrations.Communication.Pix
{
    /// <summary>Estado de uma cobranca PIX devolvido pelo gateway. Datas em hora local.</summary>
    public class PixCobrancaResultado
    {
        /// <summary>Identificador da cobranca no provedor (FAKE-... ou id numerico do Mercado Pago).</summary>
        public string IdentificadorProvedor { get; set; } = string.Empty;

        public string ReferenciaExterna { get; set; } = string.Empty;

        public PixStatusCobranca Status { get; set; } = PixStatusCobranca.AGUARDANDO_PAGAMENTO;

        public decimal Valor { get; set; }

        /// <summary>Copia e cola do PIX. Nunca vai para o log.</summary>
        public string? CopiaECola { get; set; }

        /// <summary>Imagem do QR Code em base64, sem prefixo data:. Nunca vai para o log.</summary>
        public string? QrCodeBase64 { get; set; }

        public string? UrlCobranca { get; set; }

        /// <summary>Data-hora de expiracao (hora local).</summary>
        public DateTime? ExpiraEm { get; set; }

        /// <summary>Data-hora do pagamento (hora local), quando houver.</summary>
        public DateTime? PagoEm { get; set; }

        public string Provedor { get; set; } = string.Empty;
    }
}
