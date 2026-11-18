namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Resumo das fotos ativas de um veiculo, lido numa consulta so: Total (quantas ativas) e MaiorOrdem (a maior ordem ativa, nulo
    /// quando nao ha foto). Nao e entidade nem VO: e o retorno interno do repositorio para o envio de foto.
    /// </summary>
    public sealed record ResumoFotosAtivasVeiculo(int Total, int? MaiorOrdem);
}
