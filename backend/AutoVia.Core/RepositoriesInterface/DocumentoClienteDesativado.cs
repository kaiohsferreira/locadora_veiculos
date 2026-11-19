namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Documento do cliente desativado por uma substituicao: id e chave do objeto (a chave nunca vai para o log).</summary>
    public sealed record DocumentoClienteDesativado(int IdClienteDocumento, string ChaveObjeto);
}
