using AutoVia.Core.Entities.PgSql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Catalogo de seminovos (P6-BE-01). Arquivo parcial do IVeiculoRepository: so veiculos
    /// ativos, com status DISPONIVEL e valor de venda preenchido (RF-26).
    /// </summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>
        /// Lista os veiculos a venda que atendem aos filtros, ordenados por valor de venda e id, na pagina pedida, com a categoria.
        /// A marca chega ja normalizada (Trim e maiusculas) ou null.
        /// </summary>
        Task<List<Veiculo>> FindAllAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo, int deslocamento, int quantidade);

        /// <summary>Conta os veiculos a venda que atendem aos mesmos filtros de FindAllAVendaAsync.</summary>
        Task<int> CountAVendaAsync(string? marca, decimal? precoMinimo, decimal? precoMaximo, int? anoMinimo, int? anoMaximo, int? kmMaximo);
    }
}
