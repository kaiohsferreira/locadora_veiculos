using AutoVia.Communication.ViewObjects.Vendas;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Simulacao do parcelamento e proposta de compra de seminovo (P6-BE-02, RF-27).</summary>
    public interface IPropostaCompraService
    {
        /// <summary>Simula o parcelamento de um veiculo do catalogo, sem gravar nada.</summary>
        Task<ReturnSimulacaoParcelamentoVO> SimularAsync(int idVeiculo, SimulacaoParcelamentoVO simulacao);

        /// <summary>Registra a proposta do cliente autenticado, com os valores calculados no servidor.</summary>
        Task<ReturnPropostaCompraVO> CriarAsync(int idCliente, PropostaCompraVO propostaVO);

        /// <summary>Retorna uma proposta do cliente autenticado.</summary>
        Task<ReturnPropostaCompraVO> GetByIdAsync(int idPropostaCompra, int idCliente);
    }
}
