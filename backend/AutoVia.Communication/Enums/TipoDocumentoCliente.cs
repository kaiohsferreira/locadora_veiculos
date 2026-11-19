namespace AutoVia.Communication.Enums
{
    /// <summary>
    /// Tipo do documento enviado pelo cliente (RF-08). Gravado como texto na coluna tipo (CHECK ck_cliente_documento_tipo).
    /// A ordem dos membros define a ordem da listagem: CNH antes do comprovante de residencia.
    /// </summary>
    public enum TipoDocumentoCliente
    {
        CNH,
        COMPROVANTE_RESIDENCIA
    }
}
