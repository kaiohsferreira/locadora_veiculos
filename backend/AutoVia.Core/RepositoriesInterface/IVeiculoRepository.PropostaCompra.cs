using AutoVia.Core.Entities.PgSql;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Proposta de compra (P6-BE-02). Arquivo parcial do IVeiculoRepository: o veiculo a venda pelo id.</summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>
        /// Busca um veiculo do catalogo pelo id, com o mesmo recorte do catalogo publico (ativo, DISPONIVEL, com valor de venda e
        /// de locadora ativa). Usado pela simulacao e pela proposta de compra. Nulo quando o veiculo nao esta a venda.
        /// </summary>
        Task<Veiculo?> FindAVendaByIdAsync(int idVeiculo);
    }
}
