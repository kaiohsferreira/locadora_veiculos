namespace AutoVia.Integrations.Communication.Caucao
{
    /// <summary>
    /// Estado da caucao no cartao, independente do provedor (D7: tipo do gateway, nao persistido).
    /// Mercado Pago: authorized (pending_capture) = RESERVADA, approved = CAPTURADA,
    /// in_process/pending = EM_ANALISE, rejected = RECUSADA, cancelled/canceled = CANCELADA
    /// (EXPIRADA quando status_detail = expired), refunded/charged_back = ESTORNADA.
    /// </summary>
    public enum CaucaoStatus
    {
        RESERVADA,
        CAPTURADA,
        CANCELADA,
        EXPIRADA,
        RECUSADA,
        EM_ANALISE,
        ESTORNADA
    }
}
