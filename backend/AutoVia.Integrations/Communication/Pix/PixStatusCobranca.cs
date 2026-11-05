namespace AutoVia.Integrations.Communication.Pix
{
    /// <summary>
    /// Status da cobranca PIX, independente do provedor. Mapeamento do Mercado Pago:
    /// pending = AGUARDANDO_PAGAMENTO, approved = PAGA, cancelled + expired = EXPIRADA,
    /// cancelled = CANCELADA, rejected = RECUSADA.
    /// </summary>
    public enum PixStatusCobranca
    {
        AGUARDANDO_PAGAMENTO,
        PAGA,
        EXPIRADA,
        CANCELADA,
        RECUSADA
    }
}
