namespace AutoVia.Integrations.Communication.Armazenamento
{
    /// <summary>URL temporaria de leitura de um objeto privado.</summary>
    public sealed record ArquivoArmazenado(string Url, DateTimeOffset ExpiraEm);
}
