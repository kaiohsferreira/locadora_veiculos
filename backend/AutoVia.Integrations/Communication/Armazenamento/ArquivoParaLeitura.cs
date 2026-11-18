namespace AutoVia.Integrations.Communication.Armazenamento
{
    /// <summary>
    /// Arquivo lido pela rota publica. NaoModificado verdadeiro: Conteudo vazio, porque os bytes nao foram lidos (o ETag
    /// do cliente ja confere). ETag sem aspas; o resultado da rota o envolve.
    /// </summary>
    public sealed record ArquivoParaLeitura(byte[] Conteudo, string ContentType, string ETag, bool NaoModificado);
}
