namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Linha da lista de alertas de revisao, lida numa consulta so (IManutencaoRepository.ListarAlertasRevisaoAsync). Nao e
    /// entidade nem VO: e o retorno interno do repositorio. KmUltimaPreventiva e o maior km das preventivas do veiculo
    /// (nulo quando nao ha preventiva); a regra pura (RegrasRevisao) trata o nulo como zero.
    /// </summary>
    public sealed record VeiculoRevisaoRegistro(int IdVeiculo, string Placa, string Marca, string Modelo, string CategoriaNome, int KmAtual, int? KmUltimaPreventiva);
}
