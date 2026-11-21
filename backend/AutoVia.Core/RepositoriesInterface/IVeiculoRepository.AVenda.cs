using AutoVia.Core.Entities.PgSql;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Seminovo a venda pelo id (P6-BE-02 e P6-FE-02). Arquivo parcial do IVeiculoRepository: usado pelo detalhe publico
    /// (GET /api/veiculos/a-venda/{id}), pela simulacao e pela proposta de compra.
    /// </summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>
        /// Busca um veiculo do catalogo pelo id, com o mesmo recorte do catalogo publico (ativo, DISPONIVEL, com valor de venda e
        /// de locadora ativa), com a categoria, a locadora e a capa carregadas. Nulo quando o veiculo nao esta a venda.
        /// </summary>
        Task<Veiculo?> FindAVendaByIdAsync(int idVeiculo);
    }
}
