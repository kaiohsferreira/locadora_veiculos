using AutoVia.Core.Entities.PgSql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    public partial interface IVeiculoRepository
    {
        Task<VeiculoLocacaoConsulta?> FindParaLocacaoAsync(int idVeiculo, DateOnly? retirada, DateOnly? devolucao, CancellationToken cancellationToken = default);
        Task<int> CountDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, string? cidade = null, string? uf = null);
        Task<List<Veiculo>> FindDisponiveisAsync(DateOnly retirada, DateOnly devolucao, int? idCategoria, decimal? valorMin, decimal? valorMax, int deslocamento, int quantidade, string? cidade = null, string? uf = null);
    }
}
