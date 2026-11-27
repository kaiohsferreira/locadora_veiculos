namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Valores da coluna proposta_compra.status (restricao ck_proposta_compra_status, desvio proposta_compra).
    /// Persistido como texto (HasConversion&lt;string&gt;): o NOME do membro e o valor gravado no banco.
    ///
    /// O primeiro membro (valor 0) e o DEFAULT da coluna. Nao reordene: o EF trata o valor 0 como
    /// "nao informado" no insert e deixa o banco aplicar o DEFAULT. Nesta entrega (P6-BE-02) toda proposta
    /// nasce ENVIADA; ACEITA, RECUSADA e CANCELADA ficam para a negociacao (P6-BE-03 e P6-FE-03).
    /// </summary>
    public enum StatusPropostaCompra
    {
        ENVIADA,
        ACEITA,
        RECUSADA,
        CANCELADA
    }
}
