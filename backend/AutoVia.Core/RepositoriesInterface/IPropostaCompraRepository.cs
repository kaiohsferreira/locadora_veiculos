using AutoVia.Core.Entities.PgSql;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>Repositorio da proposta de compra de seminovo (P6-BE-02, RF-27).</summary>
    public interface IPropostaCompraRepository
    {
        /// <summary>Grava a proposta nova (CreatedAt = UpdatedAt = DateTime.Now, DisabledAt nulo) e devolve com o id gerado.</summary>
        Task<PropostaCompra> InsertAsync(PropostaCompra proposta);

        /// <summary>
        /// Busca a proposta ativa pelo id, so quando ela pertence ao cliente pedido e ao cliente do contexto (posse), com o
        /// veiculo. Nula quando nao existe, esta desativada ou e de outro cliente.
        /// </summary>
        Task<PropostaCompra?> FindDoClienteByIdAsync(int idPropostaCompra, int idCliente);
    }
}
