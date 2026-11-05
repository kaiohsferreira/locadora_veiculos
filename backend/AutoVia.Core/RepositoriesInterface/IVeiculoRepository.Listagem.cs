using AutoVia.Communication.Enums;
using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Consultas da listagem de veiculos (P1-BE-03). Arquivo parcial: so enxerga veiculos
    /// ativos (DisabledAt nulo). Os filtros combinam com AND.
    /// </summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>
        /// Lista os veiculos ativos que atendem aos filtros, ordenados por placa e id, na pagina pedida, com a categoria.
        /// </summary>
        Task<List<Veiculo>> FindAllAsync(int? idCategoria, StatusVeiculo? status, string? placa, int deslocamento, int quantidade);

        /// <summary>
        /// Conta os veiculos ativos que atendem aos mesmos filtros de FindAllAsync.
        /// </summary>
        Task<int> CountAsync(int? idCategoria, StatusVeiculo? status, string? placa);
    }
}
