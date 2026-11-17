using AutoVia.Core.Entities.PgSql;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Persistencia das fotos do veiculo. Toda leitura filtra DisabledAt nulo e passa pelo filtro "Empresa" (o veiculo da
    /// locadora do contexto). O repositorio NAO lanca BusinessException nem NotFoundException: a regra fica no servico.
    /// Exclusao sempre logica (Disable), nunca Remove().
    /// </summary>
    public interface IVeiculoFotoRepository
    {
        /// <summary>Verdadeiro quando o veiculo existe, nao esta desativado e e da locadora do contexto.</summary>
        Task<bool> VeiculoAtivoExisteAsync(int idVeiculo, CancellationToken cancellationToken = default);

        /// <summary>Quantas fotos ativas o veiculo tem.</summary>
        Task<int> CountAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default);

        /// <summary>Fotos ativas do veiculo, por Ordem e depois por IdVeiculoFoto, sem rastreamento.</summary>
        Task<List<VeiculoFoto>> FindAtivasAsync(int idVeiculo, CancellationToken cancellationToken = default);

        /// <summary>Foto ativa do veiculo informado, sem rastreamento; nulo quando nao encontrou.</summary>
        Task<VeiculoFoto?> FindAtivaAsync(int idVeiculo, int idVeiculoFoto, CancellationToken cancellationToken = default);

        /// <summary>Grava a foto nova com CreatedAt e UpdatedAt = DateTime.Now e DisabledAt nulo.</summary>
        Task<VeiculoFoto> InsertAsync(VeiculoFoto foto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Desativa a foto ativa rastreada do veiculo (Disable) e grava quem removeu. Falso quando nao encontrou.
        /// </summary>
        Task<bool> DesativarAsync(int idVeiculo, int idVeiculoFoto, int idFuncionarioRemocao, CancellationToken cancellationToken = default);
    }
}
