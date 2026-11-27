using AutoVia.Communication.ViewObjects.Vendas;
using System.Threading.Tasks;

namespace AutoVia.Core.ServicesInterface
{
    /// <summary>Detalhe publico do seminovo a venda (P6-FE-02). Arquivo parcial do IVeiculoService.</summary>
    public partial interface IVeiculoService
    {
        /// <summary>
        /// Retorna um veiculo do catalogo de seminovos pelo id, no mesmo VO publico do catalogo (sem placa, chassi nem renavam),
        /// com a capa preenchida. 404 quando o veiculo nao esta a venda.
        /// </summary>
        Task<ReturnVeiculoCatalogoVO> GetAVendaByIdAsync(int idVeiculo);
    }
}
