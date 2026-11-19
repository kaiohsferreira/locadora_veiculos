using AutoVia.Communication.ViewObjects.Frota;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>
    /// Contrato da troca manual de status do veiculo (P1-BE-04). Lanca NotFoundException (404) e BusinessException (409).
    /// </summary>
    public partial interface IVeiculoService
    {
        /// <summary>Troca o status do veiculo pelo nome pedido (MANUTENCAO, DISPONIVEL ou BAIXADO nos casos permitidos); nao devolve corpo.</summary>
        Task AlterarStatusAsync(int idVeiculo, AlterarStatusVeiculoVO alterarStatusVeiculoVO);
    }
}
