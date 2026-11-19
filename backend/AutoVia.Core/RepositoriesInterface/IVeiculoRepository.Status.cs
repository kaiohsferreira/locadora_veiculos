using AutoVia.Communication.Enums;
using System.Threading.Tasks;

namespace AutoVia.Core.RepositoriesInterface
{
    /// <summary>
    /// Metodos da troca manual de status do veiculo (P1-BE-04). A decisao sai da regra pura (RegrasStatusVeiculo); o
    /// repositorio so trava, le e grava dentro da transacao.
    /// </summary>
    public partial interface IVeiculoRepository
    {
        /// <summary>
        /// Troca o status do veiculo em transacao, com trava. Devolve se alterou e o status anterior. Sem alteracao (mesmo
        /// status manual) nao grava. Lanca NotFoundException (veiculo nao achado na locadora) ou BusinessException (recusa).
        /// </summary>
        Task<(bool Alterou, StatusVeiculo StatusAnterior)> AlterarStatusAtomicamenteAsync(int idVeiculo, StatusVeiculo destino);
    }
}
